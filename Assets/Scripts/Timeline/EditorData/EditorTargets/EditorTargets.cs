using NotReaper.TargetEditor;
using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools;
using NotReaper.Tools.ChainBuilder;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NotReaper.MapEditor.Notes;
namespace NotReaper
{
    public class EditorTargets : MonoBehaviour
    {
        private static TargetAddRemove addRemove = new();
        private static TargetCopyPaste copyPaste = new();
        private static EditorNotesUI visuals;

        private void Start()
        {
            visuals = NRDependencyInjector.Get<EditorNotesUI>();
            EditorTime.onTimeChanged += _ => UpdateDualines();
            EditorTime.onTimeChanged += UpdateCueDarts;
            EditorTime.onTimeChanged += CheckTargetHit;
            EditorState.OnEditorReset += DeleteAllTargets;
        }

        /// <summary>
        /// Adds a singular target to the map through user input.
        /// </summary>
        /// <param name="x">The target's x position on the grid.</param>
        /// <param name="y">The target's y position on the grid.</param>
        public static void AddTarget(float x, float y)
            => addRemove.AddTarget(new(x, y));

        /// <summary>
        /// Adds a singular target to the map through user input.
        /// </summary>
        /// <param name="position">The target's grid position.</param>
        public static void AddTarget(Vector2 position)
            => addRemove.AddTarget(position);

        /// <summary>
        /// Adds a target to the map through an action.
        /// </summary>
        /// <param name="data">The data to add.</param>
        /// <param name="transient">True if the target should be non-selectable (e.g. for pathbuilder nodes).</param>
        /// <remarks>TargetData is kept as a reference NOT copied</remarks>
        public static Target AddTargetFromAction(TargetData data, bool transient = false)
            => addRemove.AddTargetFromAction(data, transient);
        /// <summary>
        /// Adds a target to the map through an action.
        /// </summary>
        /// <param name="cue">The cue to add.</param>
        /// <param name="transient">True if the target should be non-selectable (e.g. for pathbuilder nodes).</param>
        /// <remarks>TargetData is kept as a reference NOT copied</remarks>
        public static Target AddTargetFromAction(Cue cue, bool transient = false)
            => AddTargetFromAction(ConvertCueToTargetData(cue), transient);

        /// <summary>
        /// Deletes a target from the map through an action.
        /// </summary>
        /// <param name="data">The target to delete.</param>
        public static void DeleteTargetFromAction(TargetData data)
            => addRemove.DeleteTargetFromAction(data);
        /// <summary>
        /// Deletes the targets from the map through an action.
        /// </summary>
        /// <param name="data">The targets to delete.</param>
        public static void DeleteTargetsFromAction(List<TargetData> data)
            => data.ForEach(target => DeleteTargetFromAction(target));

        /// <summary>
        /// Deletes a target.
        /// </summary>
        /// <param name="data">The target to delete.</param>
        public static void DeleteTarget(TargetData data)
            => addRemove.DeleteTarget(data);

        /// <summary>
        /// Deletes a target.
        /// </summary>
        /// <param name="target">The target to delete.</param>
        public static void DeleteTarget(Target target)
            => DeleteTarget(target.data);
        /// <summary>
        /// Deletes the supplied targets.
        /// </summary>
        /// <param name="targets">The targets to delete.</param>
        public static void DeleteTargets(List<TargetData> targets)
            => addRemove.DeleteTargets(targets);
        /// <summary>
        /// Deletes the supplied targets.
        /// </summary>
        /// <param name="targets">The targets to delete.</param>
        public static void DeleteTargets(List<Target> targets)
            => DeleteTargets(targets.Select(target => target.data).ToList());

        /// <summary>
        /// Deletes all targets.
        /// </summary>
        public static void DeleteAllTargets()
            => addRemove.DeleteAllTargets();

        /// <summary>
        /// Deletes the currently selected targets.
        /// </summary>
        public static void DeleteSelectedTargets()
            => DeleteTargets(EditorNotes.SelectedNotes);

        /// <summary>
        /// Copies the list of targets.
        /// </summary>
        /// <param name="targets">The targets to copy.</param>
        public static void CopyTargets(List<TargetData> targets)
            => copyPaste.CopyTargets(targets);

        /// <summary>
        /// Copies the list of targets.
        /// </summary>
        /// <param name="targets">The targets to copy.</param>
        public static void CopyTargets(params TargetData[] targets)
            => CopyTargets(targets.ToList());

        /// <summary>
        /// Copies the currently selected targets.
        /// </summary>
        public static void CopySelectedTargets()
            => copyPaste.CopySelectedTargets();

        /// <summary>
        /// Pastes the currently copied targets.
        /// </summary>
        public static void PasteCopiedTargets()
            => copyPaste.PasteCopiedTargets();

        /// <summary>
        /// Pastes the supplied cues at the specified time.
        /// </summary>
        /// <param name="cues">The cues to paste.</param>
        /// <param name="pasteBeatTime">The time to paste them at.</param>
        public static void PasteTargets(List<TargetData> targets, QNT_Timestamp time)
            => copyPaste.PasteCues(targets, time);

        /// <summary>
        /// Pastes the supplied cues at the specified time.
        /// </summary>
        /// <param name="cues">The cues to paste.</param>
        /// <param name="pasteBeatTime">The time to paste them at.</param>
        public static void PasteTargets(List<Target> targets, QNT_Timestamp time)
            => PasteTargets(targets.Select(target => target.data).ToList(), time);

        /// <summary>
        /// Cuts the selected targets.
        /// </summary>
        public static void CutSelectedTargets()
            => copyPaste.CutSelectedTargets();

        /// <summary>
        /// Checks if the supplied time is inside of the intro zone.
        /// </summary>
        /// <param name="time">The time to check for.</param>
        /// <returns>True if the time is inside of the intro zone.</returns>
        public static bool IsTimeInIntroZone(QNT_Timestamp time)
            => addRemove.IsTimeInIntroZone(time);

        /// <summary>
        /// Checks if doubled targets (e.g. 2 left hand targets on the same tick) would occur.
        /// </summary>
        /// <param name="targets">The targets to check for.</param>
        /// <returns>True if any of the targets in the list would lead to doubled targets when added.</returns>
        public static bool WouldHaveDoubledTargets(List<TargetData> targets, out string reason)
            => copyPaste.WouldHaveDoubledTargets(targets, out reason);

        /// <summary>
        /// Checks if doubled targets (e.g. 2 left hand targets on the same tick) would occur.
        /// </summary>
        /// <param name="targets">The target to check for.</param>
        /// <returns>True if the target would lead to doubled targets when added.</returns>
        public static bool WouldHaveDoubledTargets(TargetData data, out string reason)
            => copyPaste.WouldHaveDoubledTargets(new List<TargetData> { data }, out reason);

        /// <summary>
        /// Moves grid targets.
        /// </summary>
        /// <param name="intents">The targets you want to move.</param>
        public static void MoveGridTargets(List<TargetGridMoveIntent> intents)
            => UndoRedoManager.AddAction(new NRActionGridMoveNotes(intents));

        /// <summary>
        /// Moves timeline targets.
        /// </summary>
        /// <param name="intents">The targets you want to move.</param>
        public static void MoveTimelineTargets(List<TargetTimelineMoveIntent> intents)
            => UndoRedoManager.AddAction(new NRActionTimelineMoveNotes(intents));

        /// <summary>
        /// Swaps the color of the supplied targets.
        /// </summary>
        /// <param name="targets">The targets to swap colors for.</param>
        public static void SwapTargetColors(List<TargetData> targets)
             => UndoRedoManager.AddAction(new NRActionSwapNoteColors(targets));

        /// <summary>
        /// Swaps the color of the supplied targets.
        /// </summary>
        /// <param name="targets">The targets to swap colors for.</param>
        public static void SwapTargetColors(params TargetData[] targets)
            => SwapTargetColors(targets.ToList());

        /// <summary>
        /// Swaps the color of the supplied targets.
        /// </summary>
        /// <param name="targets">The targets to swap colors for.</param>
        public static void SwapTargetColors(params Target[] targets)
            => SwapTargetColors(targets.ToList());

        /// <summary>
        /// Swaps the color of the supplied targets.
        /// </summary>
        /// <param name="targets">The targets to swap colors for.</param>
        public static void SwapTargetColors(List<Target> targets)
            => SwapTargetColors(targets.Select(target => target.data).ToList());

        /// <summary>
        /// Swaps the color of the currently selected targets.
        /// </summary>
        public static void SwapSelecedTargetsColor()
            => SwapTargetColors(EditorNotes.SelectedNotesData);

        /// <summary>
        /// Flips the supplied targets on their X-axis.
        /// </summary>
        /// <param name="targets">The targets to flip.</param>
        public static void FlipTargetsHorizontal(List<TargetData> targets)
            => UndoRedoManager.AddAction(new NRActionHFlipNotes(targets));

        /// <summary>
        /// Flips the supplied targets on their X-axis.
        /// </summary>
        /// <param name="targets">The targets to flip.</param>
        public static void FlipTargetsHorizontal(List<Target> targets)
            => FlipTargetsHorizontal(targets.Select(target => target.data).ToList());

        /// <summary>
        /// Flips the currently selected targets on their X-axis.
        /// </summary>
        /// <param name="targets">The targets to flip.</param>
        public static void FlipSelectedTargetsHorizontal()
            => FlipTargetsHorizontal(EditorNotes.SelectedNotesData);

        /// <summary>
        /// Flips the supplied targets on their Y-axis.
        /// </summary>
        /// <param name="targets">The targets to flip.</param>
        public static void FlipTargetsVertical(List<TargetData> targets)
            => UndoRedoManager.AddAction(new NRActionVFlipNotes(targets));

        /// <summary>
        /// Flips the supplied targets on their Y-axis.
        /// </summary>
        /// <param name="targets">The targets to flip.</param>
        public static void FlipTargetsVertical(List<Target> targets)
            => FlipTargetsVertical(targets.Select(target => target.data).ToList());

        /// <summary>
        /// Flips the currently selected targets on their Y-axis.
        /// </summary>
        /// <param name="targets">The targets to flip.</param>
        public static void FlipSelectedTargetsVertical()
            => FlipTargetsVertical(EditorNotes.SelectedNotesData);

        /// <summary>
        /// Scales the supplied targets by scale.
        /// </summary>
        /// <param name="targets">The tarets to scale.</param>
        /// <param name="scale">The amount to scale the targets by.</param>
        public static void ScaleTargets(List<TargetData> targets, Vector2 scale)
            => UndoRedoManager.AddAction(new NRActionScale(targets, scale));

        /// <summary>
        /// Scales the supplied targets by scale.
        /// </summary>
        /// <param name="targets">The tarets to scale.</param>
        /// <param name="scale">The amount to scale the targets by.</param>
        public static void ScaleTargets(List<Target> targets, Vector2 scale)
            => ScaleTargets(targets.Select(target => target.data).ToList(), scale);

        /// <summary>
        /// Scales the currently selected targets by scale.
        /// </summary>
        /// <param name="scale">The amount to scale the targets by.</param>
        public static void ScaleSelectedTargets(Vector2 scale)
            => ScaleTargets(EditorNotes.SelectedNotesData, scale);

        /// <summary>
        /// Rotates the supplied targets around center by angle.
        /// </summary>
        /// <param name="targets">The targets to rotate.</param>
        /// <param name="angle">The angle to rotate the targets by.</param>
        /// <param name="center">The center to rotate the targets around. Set to Vector2.zero if value is null.</param>
        public static void RotateTargets(List<TargetData> targets, float angle, Vector2? center = null)
            => UndoRedoManager.AddAction(new NRActionRotate(targets, angle, center));

        /// <summary>
        /// Rotates the supplied targets around center by angle.
        /// </summary>
        /// <param name="targets">The targets to rotate.</param>
        /// <param name="angle">The angle to rotate the targets by.</param>
        /// <param name="center">The center to rotate the targets around. Set to Vector2.zero if value is null.</param>
        public static void RotateTargets(List<Target> targets, float angle, Vector2? center = null)
            => RotateTargets(targets.Select(target => target.data).ToList(), angle, center);

        /// <summary>
        /// Rotates the currently selected targets around center by angle.
        /// </summary>
        /// <param name="targets">The targets to rotate.</param>
        /// <param name="angle">The angle to rotate the targets by.</param>
        /// <param name="center">The center to rotate the targets around. Set to Vector2.zero if value is null.</param>
        public static void RotateSelectedTargets(float angle, Vector2? center = null)
            => RotateTargets(EditorNotes.SelectedNotesData, angle, center);

        /// <summary>
        /// Reverses the supplied targets.
        /// </summary>
        /// <param name="targets">The targets to reverse.</param>
        public static void ReverseTargets(List<TargetData> targets)
            => UndoRedoManager.AddAction(new NRActionReverse(targets));

        /// <summary>
        /// Reverses the supplied targets.
        /// </summary>
        /// <param name="targets">The targets to reverse.</param>
        public static void ReverseTargets(List<Target> targets)
            => ReverseTargets(targets.Select(target => target.data).ToList());

        /// <summary>
        /// Reverses the currently selected notes.
        /// </summary>
        public static void ReverseSelectedTargets()
            => ReverseTargets(EditorNotes.SelectedNotesData);

        /// <summary>
        /// Sets hitsound of targets.
        /// </summary>
        /// <param name="intents">The hitsound intents.</param>
        public static void SetTargetHitsounds(List<TargetSetHitsoundIntent> intents)
            => UndoRedoManager.AddAction(new NRActionSetTargetHitsound(intents));

        /// <summary>
        /// Sets behavior of targets.
        /// </summary>
        /// <param name="action">The behavior action.</param>
        public static void SetTargetBehaviors(NRActionSetTargetBehavior action)
            => UndoRedoManager.AddAction(action);

        /// <summary>
        /// Deselects the behavior from the currently selected targets.
        /// </summary>
        /// <param name="behavior">The behavior to deselect.</param>
        public static void DeselectBehavior(TargetBehavior behavior)
            => UndoRedoManager.AddAction(new NRActionDeselectBehavior(behavior));

        /// <summary>
        /// Deselects the hand type from the currently selected targets.
        /// </summary>
        /// <param name="handType">The hand type to deselect.</param>
        public static void DeselectHand(TargetHandType handType)
            => UndoRedoManager.AddAction(new NRActionDeselectHand(handType));

        /// <summary>
        /// Updates the color of all targets 
        /// </summary>
        public static void UpdateTargetColors() => visuals.UpdateTargetColors();
        /// <summary>
        /// Updates a sustain length from the buttons next to sustains.
        /// </summary>
        /// <param name="target">The target to affect</param>
        /// <param name="increase">If true, increase by one beat snap, if false, the opposite.</param>
        public static void UpdateSustainLength(Target target, bool increase) => visuals.UpdateSustainLength(target, increase);
        /// <summary>
        /// Updates chain connector lines for a target.
        /// </summary>
        /// <param name="data">The target do update the connector line for.</param>
        public static void UpdateChainConnector(TargetData data) => visuals.UpdateChainConnector(data);
        /// <summary>
        /// Updates chain connector lines for a target.
        /// </summary>
        /// <param name="target">The target do update the connector line for.</param>
        public static void UpdateChainConnector(Target target) => visuals.UpdateChainConnector(target.data);
        /// <summary>
        /// Enables or disables sustain length buttons depending on their musical distance.
        /// </summary>
        public static void EnableNearSustainButtons() => visuals.EnableNearSustainButtons();
        /// <summary>
        /// Shows or hides timeline targets.
        /// </summary>
        /// <param name="show">True to show, false to hide.</param>
        public static void ShowTimelineTargets(bool show) => visuals.ShowTimelineTargets(show);
        /// <summary>
        /// Updates connector lines between doubles.
        /// </summary>
        public static void UpdateDualines() => visuals.UpdateDualines();
        /// <summary>
        /// Updates cue darts.
        /// </summary>
        private static void UpdateCueDarts(QNT_Timestamp time) => visuals.UpdateCueDarts(time);
        /// <summary>
        /// Plays on-hit effects on all targets we passed since the last tick update.
        /// </summary>
        /// <param name="currentTime">The current time in the song.</param>
        private static void CheckTargetHit(QNT_Timestamp time) => visuals.OnTargetHit(time);
        /// <summary>
        /// Converts a <see cref="Cue"/> to <see cref="TargetData"/>
        /// </summary>
        /// <param name="cue">The <see cref="Cue"/> to convert.</param>
        /// <returns>The converted <see cref="TargetData"/></returns>
        public static TargetData ConvertCueToTargetData(Cue cue)
        {
            TargetData data = new TargetData(cue);
            if (data.time.tick == 0) data.SetTimeFromAction(new QNT_Timestamp(120));
            return data;
        }
    }
}
