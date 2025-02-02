using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NotReaper.MapEditor.Notes;

namespace NotReaper
{
    /// <summary>
    /// Responsible for the map's targets.
    /// </summary>
    public static class EditorNotes
    {
        /// <summary>
        /// The loaded map's notes.
        /// </summary>
        public static List<Target> Notes { get; private set; } = new();

        /// <summary>
        /// The notes in range of current time.
        /// </summary>
        public static List<Target> LoadedNotes { get; private set; } = new();

        /// <summary>
        /// The loaded map's notes, sorted by time.
        /// </summary>
        public static List<Target> OrderedNotes { get; private set; } = new();

        /// <summary>
        /// The currently selected notes.
        /// </summary>
        public static List<Target> SelectedNotes { get; private set; } = new();
        /// <summary>
        /// The currently selected notes <see cref="TargetData"/>.
        /// </summary>
        public static List<TargetData> SelectedNotesData 
            => SelectedNotes.Select(target => target.data).ToList();

        /// <summary>
        /// Indicates if any targets are selected.
        /// </summary>
        public static bool HasSelectedNotes => SelectedNotes.Count > 0;

        /// <summary>
        /// Raised when count of <see cref="SelectedNotes"/> changed.
        /// </summary>
        public static OnNoteCountChangedHandler onSelectedNoteCountChanged;
        public delegate void OnNoteCountChangedHandler(int noteCount);

        static EditorNotes()
        {
            EditorTime.onTimeChanged += _ => UpdateLoadedNotes();
            //EditorFile.onAudicaFileLoaded += _ => UpdateNotes();
            EditorFile.onLoaded += () =>
            {
                UpdateNotes();
                EditorTargets.UpdateChainConnectors();
            };
        }

        /// <summary>
        /// Adds a note to the loaded map.
        /// </summary>
        /// <param name="target">The target to add.</param>
        public static void AddNote(Target target) => AddNotes(new List<Target>() { target });

        /// <summary>
        /// Adds notes to the loaded map.
        /// </summary>
        /// <param name="target">The targets to add.</param>
        public static void AddNotes(params Target[] targets) => AddNotes(targets.ToList());

        /// <summary>
        /// Adds notes to the loaded map.
        /// </summary>
        /// <param name="target">The targets to add.</param>
        public static void AddNotes(List<Target> targets)
        {
            bool addedAny = false;
            foreach (var target in targets)
            {
                if (!Notes.Contains(target))
                {
                    Notes.Add(target);
                    addedAny = true;
                }
            }
            if (addedAny)
                UpdateNotes();
        }
        /// <summary>
        /// Removes note from the loaded map.
        /// </summary>
        /// <param name="target">The target to remove.</param>
        public static void RemoveNote(Target target) => RemoveNotes(new List<Target>() { target });

        /// <summary>
        /// Removes notes from the loaded map.
        /// </summary>
        /// <param name="target">The targets to remove.</param>
        public static void RemoveNotes(params Target[] targets) => RemoveNotes(targets.ToList());

        /// <summary>
        /// Removes notes from the loaded map.
        /// </summary>
        /// <param name="target">The targets to remove.</param>
        public static void RemoveNotes(List<Target> targets)
        {
            bool removedAny = false;
            foreach (var target in targets)
            {
                if (Notes.Contains(target))
                {
                    Notes.Remove(target);
                    DeselectTarget(target);
                    removedAny = true;
                }
            }

            if (removedAny)
                UpdateNotes();
        }
        /// <summary>
        /// Sorts <see cref="OrderedNotes"/> by time, then handType, then pitch.
        /// </summary>
        public static void SortOrderedNotes()
        {
            OrderedNotes.Sort((t1, t2) =>
            {
                var timeComparison = t1.data.time.CompareTo(t2.data.time);
                if (timeComparison != 0)
                {
                    return timeComparison;
                }
                
                var behaviorComparison = t1.data.behavior.CompareTo(t2.data.behavior);
                if (behaviorComparison != 0)
                {
                    return behaviorComparison;
                }
                
                var handTypeComparison = t1.data.handType.CompareTo(t2.data.handType);
                if (handTypeComparison != 0)
                {
                    return handTypeComparison;
                }

                var xPositionComparison =  t1.data.x.CompareTo(t2.data.x);
                if (xPositionComparison != 0)
                {
                    return xPositionComparison;
                }
                
                return t1.data.y.CompareTo(t2.data.y);
            });
        }

        /// <summary>
        /// Updates Loaded Notes.
        /// </summary>
        private static void UpdateLoadedNotes()
        {
            List<Target> newLoadedNotes = new List<Target>();
            QNT_Timestamp loadStart = EditorTime.Time - Relative_QNT.FromBeatTime(10.0f);
            QNT_Timestamp loadEnd = EditorTime.Time + Relative_QNT.FromBeatTime(10.0f);

            foreach (Target t in new NoteEnumerator(loadStart, loadEnd))
            {
                newLoadedNotes.Add(t);
                if (LoadedNotes.Contains(t)) continue;
                t.gridTargetIcon.IconEnterLoadedNotes();
            }
            LoadedNotes = newLoadedNotes;
        }

        /// <summary>
        /// Selects a target.
        /// </summary>
        /// <param name="target">The target to select.</param>
        public static void SelectTarget(Target target) => SelectTargets(new List<Target>() { target });
        /// <summary>
        /// Selects multiple targets.
        /// </summary>
        /// <param name="targets">Targets to select.</param>
        public static void SelectTargets(params Target[] targets) => SelectTargets(targets.ToList());
        /// <summary>
        /// Selects all targets.
        /// </summary>
        public static void SelectAllTargets() => SelectTargets(OrderedNotes);
        /// <summary>
        /// Selects multiple targets.
        /// </summary>
        /// <param name="targets">Targets to select.</param>
        public static void SelectTargets(List<Target> targets)
        {
            bool hasSelectedAny = false;
            foreach (Target target in targets)
            {
                if (target.transient) continue;
                
                if (!SelectedNotes.Contains(target))
                {
                    target.VisualSelect();
                    SelectedNotes.Add(target);
                    hasSelectedAny = true;
                }
            }

            if (hasSelectedAny)
            {
                SelectedNotes.Sort((n1, n2) => n1.data.time.CompareTo(n2.data.time));
                onSelectedNoteCountChanged?.Invoke(SelectedNotes.Count);
            }
        }
        /// <summary>
        /// Deselcts a target.
        /// </summary>
        /// <param name="target">The target to remove.</param>
        public static void DeselectTarget(Target target) => DeselectTargets(new List<Target>() { target });
        /// <summary>
        /// Deselects targets.
        /// </summary>
        /// <param name="targets">The targets to deselect.</param>
        public static void DeselectTargets(params Target[] targets) => DeselectTargets(targets.ToList());
        /// <summary>
        /// Deselects targets.
        /// </summary>
        /// <param name="targets">The targets to deselect.</param>
        public static void DeselectTargets(List<Target> targets)
        {
            bool hasDeselectedAny = false;
            foreach (var target in targets)
            {
                if (SelectedNotes.Contains(target))
                {
                    target.VisualDeselect();
                    SelectedNotes.Remove(target);
                    hasDeselectedAny = true;
                }
            }

            if (hasDeselectedAny)
                onSelectedNoteCountChanged?.Invoke(SelectedNotes.Count);
        }
        /// <summary>
        /// Removes all notes from <see cref="SelectedNotes"/>.
        /// </summary>
        public static void DeselectAllTargets()
        {
            if (SelectedNotes.Count == 0)
                return;

            foreach (var target in SelectedNotes)
                target.VisualDeselect();

            SelectedNotes.Clear();
            onSelectedNoteCountChanged?.Invoke(0);
        }

        /// <summary>
        /// Clears all targets in <see cref="Notes"/>, <see cref="LoadedNotes"/>, <see cref="OrderedNotes"/> and <see cref="SelectedNotes"/>
        /// </summary>
        public static void ClearAllNotes()
        {
            Notes.Clear();
            LoadedNotes.Clear();
            OrderedNotes.Clear();
            SelectedNotes.Clear();
        }
        /// <summary>
        /// Updates <see cref="OrderedNotes"/> to state of <see cref="Notes"/> and sorts by time.
        /// </summary>
        private static void UpdateNotes(bool force = false)
        {
            if (EditorFile.IsLoading && !force) return;
            OrderedNotes = Notes;
            OrderedNotes.Sort((t1, t2) => t1.data.time.CompareTo(t2.data.time));
            UpdateLoadedNotes();
            EditorTargets.UpdateDualines();
        }

        public static void ForceUpdateNotes() => UpdateNotes(true);

    }
}
