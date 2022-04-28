using System.Linq;
using System.Collections.Generic;
using NotReaper.Targets;
using NotReaper.Models;
using NotReaper.Timing;
using NotReaper.Notifications;

namespace NotReaper.Tools
{
    public class UndoRedoManager : Singleton<UndoRedoManager>
    {

        /// <summary>
        /// Contains the complete list of actions the user has done recently.
        /// </summary>
        private static List<NRAction> actions = new List<NRAction>();

        /// <summary>
        /// Contains the actions the user has "undone" for future use.
        /// </summary>
        private static List<NRAction> redoActions = new List<NRAction>();

        private const int MaxSavedActions = 20;

        private static Timeline timeline;

        private void Start()
        {
            timeline = NRDependencyInjector.Get<Timeline>();
            EditorState.OnEditorReset += ClearActions;
        }

        /// <summary>
        /// Undo the last action performed by the user.
        /// </summary>
        public static void Undo()
        {
            if (actions.Count <= 0) return;

            NRAction action = actions.Last();

            action.UndoAction(timeline);

            redoActions.Add(action);
            actions.RemoveAt(actions.Count - 1);

            //EditorScale.ReapplyScale();
        }
        /// <summary>
        /// Redo the last action the user has undone.
        /// </summary>
        public static void Redo()
        {

            if (redoActions.Count <= 0) return;

            NRAction action = redoActions.Last();

            action.DoAction(timeline);

            actions.Add(action);
            redoActions.RemoveAt(redoActions.Count - 1);
            //EditorScale.ReapplyScale();
        }
        /// <summary>
        /// Add an action that can be un- and redone.
        /// </summary>
        /// <param name="action">The actino to add.</param>
        public static void AddAction(NRAction action)
        {
            if (actions.Count <= MaxSavedActions)
            {
                actions.Add(action);
            }
            else
            {
                while (MaxSavedActions > actions.Count)
                {
                    actions.RemoveAt(0);
                }
                actions.Add(action);
            }
            action.DoAction(timeline);
            redoActions = new List<NRAction>();
        }
        /// <summary>
        /// Removes an action from the undo/redo history.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        public static void RemoveAction(NRAction action)
        {
            if(actions.Contains(action))
                actions.Remove(action);

            if (redoActions.Contains(action))
                redoActions.Remove(action);
        }

        public static void ClearActions()
        {
            actions = new List<NRAction>();
            redoActions = new List<NRAction>();
        }
    }

    public abstract class NRAction
    {
        protected List<Target> chainStarts = new();
        /// <summary>
        /// Is true if we checked for stacked targets and found a stack, false otherwise.
        /// </summary>
        internal bool hasStackedTargets { get; private set; } = false;
        public abstract void DoAction(Timeline timeline);
        public abstract void UndoAction(Timeline timeline);

        /// <summary>
        /// Finds the chain start of a target.
        /// </summary>
        /// <param name="data">The target to find the chain start for.</param>
        protected void FindChainStart(TargetData data)
        {
            var start = TargetFinder.FindChainStart(data);
            if (start != null && !chainStarts.Contains(start))
                chainStarts.Add(start);
        }
        /// <summary>
        /// Updates chain connectors of all affected chains.
        /// </summary>
        protected void UpdateChainConnectors()
        {
            foreach (var start in chainStarts)
                EditorTargets.UpdateChainConnector(start);

            chainStarts.Clear();
        }

        /// <summary>
        /// Checks if any targets with the same handtype are stacked, undos the action and removes it from the undo/redo history if any stacked target is found.
        /// </summary>
        /// <param name="timeline">Reference to timeline</param>
        /// <param name="actionName">The name of the action for error notifications. Notifications have the following format: Can't {actionName}: reason for stack</param>
        /// <param name="manipulatedTargets">The targets that have been manipulated by the action.</param>
        internal void CheckForStackedTargets(Timeline timeline, string actionName, params TargetData[] manipulatedTargets)
            => CheckForStackedTargets(timeline, actionName, manipulatedTargets.ToList());

        /// <summary>
        /// Checks if any targets with the same handtype are stacked, undos the action and removes it from the undo/redo history if any stacked target is found.
        /// </summary>
        /// <param name="timeline">Reference to timeline</param>
        /// <param name="actionName">The name of the action for error notifications. Notifications have the following format: Can't {actionName}: reason for stack</param>
        /// <param name="manipulatedTargets">The targets that have been manipulated by the action.</param>
        internal void CheckForStackedTargets(Timeline timeline, string actionName, List<TargetData> manipulatedTargets)
        {
            if (hasStackedTargets)
                return;

            List<TargetData> temp = new();
            //pathbuilder and legacy pathbuilder targets are moved as one.
            //Meaning, if only the start is moved, all children will be moved, too.
            //that's why we have to add all nodes to the list before we perform any checks.
            foreach(var t in manipulatedTargets)
            {
                if (t.behavior == TargetBehavior.Mine)  // skip all mines
                {
                    continue;
                }
                else if (t.behavior == TargetBehavior.Legacy_Pathbuilder)   // add legacy pathbuilder targets to the list
                {
                    temp.AddRange(t.legacyPathbuilderData.generatedNotes);
                    continue;
                }

                temp.Add(t);    // add the actual moved target to the list

                if (t.isPathbuilderTarget)
                {
                    foreach (var segment in t.pathbuilderData.Segments) // add pathbuilder children to the list
                        temp.AddRange(segment.generatedNodes);
                }
            }
            manipulatedTargets = temp;  // update the list with all nodes added

            // when we perform a check, we look for targets at the same time.
            // Since binary search doesn't guarantee that the target we find
            // is actually the first at the given time, we start the search
            // one tick earlier to guarantee we find all targets at the given
            // time.
            QNT_Duration buffer = new(1);   
            foreach(var target in manipulatedTargets)
            {
                int meleeCount = 0;
                int lhMeleeCount = 0;
                int rhMeleeCount = 0;
                if (target.behavior == TargetBehavior.Sustain)  // check for targets during a sustain
                {
                    foreach (var note in new NoteEnumerator(target.time - buffer, target.time + target.beatLength))
                    {
                        if (note.data.time < target.time) // compensate for the buffer
                            continue;

                        // ignore targets at the same time - that check will be performed later.
                        // also ignore all melees,
                        // and all legacy PB targets, since those are just ghost notes.
                        if (note.data.time == target.time || note.data.behavior.IsMeleeOrMine() || note.data.behavior.IsLegacyPathbuilder())
                            continue;

                        if (note.data.handType == target.handType)
                        {
                            NotificationCenter.SendNotification($"Can't {actionName}: " +
                                $"Targets of the same color would occur during sustain at {target.time}", NotificationType.Warning);
                            goto Found;
                        }
                    }
                }
                else if(target.behavior == TargetBehavior.Melee)    // check for stacked melees
                {
                    foreach(var note in new NoteEnumerator(target.time - buffer, target.time))
                    {
                        // we only want to check for melees
                        if (note.data.behavior != TargetBehavior.Melee)
                            continue;

                        // compensate for the buffer and skip legacy PB targets
                        if (note.data.time < target.time || note.data.behavior.IsLegacyPathbuilder())
                            continue;

                        // skip our own target so we don't check against ourselves
                        if (note.data == target)
                            continue;

                        // keep track of how many melees we have
                        if (note.data.handType == TargetHandType.Left)
                            lhMeleeCount++;
                        else if (note.data.handType == TargetHandType.Right)
                            rhMeleeCount++;

                        meleeCount++;

                        if (meleeCount == 2)
                        {
                            NotificationCenter.SendNotification($"Can't {actionName}: Can't place more than 2 melees at once at {target.time}");
                            goto Found;
                        }
                        else if(lhMeleeCount == 1 && target.handType == TargetHandType.Left)
                        {
                            NotificationCenter.SendNotification($"Can't {actionName}: Can't place 2 left hand melees at the same time at {target.time}");
                            goto Found;
                        }
                        else if(rhMeleeCount == 1 && target.handType == TargetHandType.Right)
                        {
                            NotificationCenter.SendNotification($"Can't {actionName}: Can't place 2 right hand melees at the same time at {target.time}");
                            goto Found;
                        }

                        // find the target for our melee, so we can convert it
                        // to a cue and compare pitches. That way, we don't have to
                        // worry about any potential offset.
                        var myMelee = TargetFinder.FindNote(target); 

                        if(myMelee != null)
                        {
                            if(note.data.time == target.time && note.ToCue().pitch == myMelee.ToCue().pitch)
                            {
                                NotificationCenter.SendNotification($"Can't {actionName}: Melees at {target.time} would be stacked.", NotificationType.Warning);
                                goto Found;
                            }
                        }
                    }
                }

                // only continue if the target actually has a color
                if (target.handType != TargetHandType.Left && target.handType != TargetHandType.Right)
                    continue;

                var notes = new NoteEnumerator(target.time - buffer, target.time).ToList();
                foreach(var note in notes)
                {
                    var data = note.data;

                    if (data.time != target.time)   // compensate for buffer
                        continue;

                    // we don't want to check against ourselves, and skip any legacy PB targets
                    if (data == target || data.behavior.IsLegacyPathbuilder())
                        continue;

                    if(data.time == target.time && data.handType == target.handType)
                    {
                        NotificationCenter.SendNotification($"Can't {actionName}: Targets at {target.time} would be stacked.", NotificationType.Warning);
                        goto Found;
                    }
                }
            }

            return;
        
        //if we find anything, we set the hasStackedTargets flag to true
        //so we don't perform checks again when performing the undo.
        //additionally, we remove the action from undo/redo history, since it wasn't successful.
        Found:
            hasStackedTargets = true;
            UndoAction(timeline);
            UndoRedoManager.RemoveAction(this);
        }
    }
}