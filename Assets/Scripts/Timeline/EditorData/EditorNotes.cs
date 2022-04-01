using NotReaper.Targets;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        /// Indicates if any targets are selected.
        /// </summary>
        public static bool HasSelectedNotes => SelectedNotes.Count > 0;

        /// <summary>
        /// Raised when count of <see cref="SelectedNotes"/> changed.
        /// </summary>
        public static OnNoteCountChangedHandler onSelectedNoteCountChanged;
        public delegate void OnNoteCountChangedHandler(int noteCount);

        private static EditorNotesUI noteVisuals = new();

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
        /// Sorts <see cref="OrderedNotes"/> by time. 
        /// </summary>
        public static void SortOrderedNotes()
        {
            OrderedNotes.Sort((t1, t2) => t1.data.time.CompareTo(t2.data.time));
        }

        /// <summary>
        /// Updates Loaded Notes.
        /// </summary>
        private static void UpdateLoadedNotes(QNT_Timestamp _)
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
                if (!SelectedNotes.Contains(target))
                {
                    target.Select();
                    SelectedNotes.Add(target);
                    hasSelectedAny = true;
                }
            }

            if (hasSelectedAny)
                onSelectedNoteCountChanged?.Invoke(SelectedNotes.Count);
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
                    target.Deselect();
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
                target.Deselect();

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
        private static void UpdateNotes()
        {
            OrderedNotes = Notes;
            OrderedNotes.Sort((t1, t2) => t1.data.time.CompareTo(t2.data.time));
            UpdateLoadedNotes(new());
        }
        /// <summary>
        /// Updates the color of all targets 
        /// </summary>
        public static void UpdateTargetColors() => noteVisuals.UpdateTargetColors();
        /// <summary>
        /// Updates a sustain length from the buttons next to sustains.
        /// </summary>
        /// <param name="target">The target to affect</param>
        /// <param name="increase">If true, increase by one beat snap, if false, the opposite.</param>
        public static void UpdateSustainLength(Target target, bool increase) => noteVisuals.UpdateSustainLength(target, increase);
        /// <summary>
        /// Updates chain connector lines for a target.
        /// </summary>
        /// <param name="data">The target do update the connector line for.</param>
        public static void UpdateChainConnector(TargetData data) => noteVisuals.UpdateChainConnector(data);
        /// <summary>
        /// Updates chain connector lines for a target.
        /// </summary>
        /// <param name="target">The target do update the connector line for.</param>
        public static void UpdateChainConnector(Target target) => noteVisuals.UpdateChainConnector(target.data);
        /// <summary>
        /// Enables or disables sustain length buttons depending on their musical distance.
        /// </summary>
        public static void EnableNearSustainButtons() => noteVisuals.EnableNearSustainButtons();
    }
}
