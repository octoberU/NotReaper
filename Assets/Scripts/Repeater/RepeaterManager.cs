using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools;
using NotReaper.Tools.ChainBuilder;
using NotReaper.UserInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NotReaper.Repeaters
{
    public class RepeaterManager : NRInput<RepeaterKeybinds>
    {
        [SerializeField] private RepeaterIndicator repeaterIndicatorPrefab;
        [SerializeField] private Transform timelineParent;
        [SerializeField] private Transform miniTimelineParent;
        [SerializeField] private List<Color> colors;
        [NRInject] private Timeline timeline;
        private RepeaterMenu overlay;
        private Dictionary<string, List<RepeaterSection>> repeaters = new Dictionary<string, List<RepeaterSection>>();

        public static RepeaterManager Instance { get; private set; }

        protected override void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError("Trying to create second RepeaterManager instance.");
                return;
            }

            base.Awake();
            EditorScale.onScaleChanged += OnScaleChanged;

            
        }

        private void Start()
        {
            overlay = NRDependencyInjector.Get<RepeaterMenu>();
        }

        public bool AddRepeaterFromAction(RepeaterSection section)
        {
            return AddRepeaterFromAction(section.ID, section.startTime, section.endTime, section.activeStartTime, section.activeEndTime, section.flipTargetColors, section.mirrorHorizontally, section.mirrorVertically);
        }

        public bool AddRepeaterFromAction(string id, QNT_Timestamp startTime, QNT_Timestamp endTime, QNT_Timestamp activeStartTime, QNT_Timestamp activeEndTime, bool flipColors, bool mirrorHorizontally, bool mirrorVertically)
        {
            List<TargetData> targets = new List<TargetData>();
            //add repeater section based on the first repeater section with the same id
            if (repeaters.ContainsKey(id))
            {
                var firstRepeater = repeaters[id].First(s => s.isParent);
                endTime = activeEndTime = new QNT_Timestamp(startTime.tick + (firstRepeater.activeEndTime.tick - firstRepeater.startTime.tick));
                firstRepeater.targets.Sort((x, y) => x.time.CompareTo(y.time));
                //check if we'd run into any other targets if we were to insert a full repeater. we set the end time to whatever the last target's time is.
                NoteEnumerator notes = new NoteEnumerator(startTime, endTime);
                QNT_Timestamp blockingTargetTime = new QNT_Timestamp(0);
                bool isBlocked = notes.Count() > 0;
                if (isBlocked)
                {
                    blockingTargetTime = notes.First().data.time;
                }
                int count = 0;
                foreach (var target in firstRepeater.targets)
                {
                    var t = TargetFinder.FindNote(target);
                    if (t != null && t.transient) continue;
                    if (isBlocked)
                    {
                        if (startTime + target.repeaterData.RelativeTime >= blockingTargetTime)
                        {
                            //check if we can place at least 2 targets. We won't allow making a repeater otherwise.
                            if (count < 2)
                            {
                                NotificationCenter.SendNotification("Need to be able to place at least 2 targets in order to insert a repeater.", NotificationType.Warning);
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    TargetData repeaterTarget = new TargetData();
                    repeaterTarget.Copy(target);
                    repeaterTarget.repeaterData = new RepeaterData();
                    repeaterTarget.repeaterData.Copy(target.repeaterData);
                    if (repeaterTarget.isPathbuilderTarget)
                    {
                        repeaterTarget.pathbuilderData = new();
                        repeaterTarget.pathbuilderData.Copy(target.pathbuilderData);
                    }
                    if (repeaterTarget.legacyPathbuilderData != null)
                    {
                        repeaterTarget.legacyPathbuilderData = new();
                        repeaterTarget.legacyPathbuilderData.Copy(target.legacyPathbuilderData);
                    }
                    repeaterTarget.SetTimeFromAction(startTime + repeaterTarget.repeaterData.RelativeTime);
                    targets.Add(repeaterTarget);
                    //activeEndTime = startTime + target.repeaterData.RelativeTime;
                    count++;
                }
                foreach (var target in targets)
                {
                    EditorTargets.AddTargetFromAction(target);
                    if (target.isPathbuilderTarget) timeline.pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(target, target.pathbuilderData);
                }
            }
            //add repeater section with new id
            else
            {
                NoteEnumerator notes = new NoteEnumerator(startTime, endTime);

                //check for targets that have longer beat lengths and extend end time accordingly
                var lastNote = notes.Last().data;
                if (lastNote.behavior == TargetBehavior.Sustain)
                    endTime = lastNote.time + lastNote.beatLength;
                else if (lastNote.behavior == TargetBehavior.Legacy_Pathbuilder)
                    endTime = lastNote.legacyPathbuilderData.generatedNotes.Last().time;
                else if (lastNote.isPathbuilderTarget)
                    endTime = lastNote.time + lastNote.pathbuilderData.TotalSegmentLength;

                if(activeEndTime != endTime)
                {
                    activeEndTime = endTime;
                    notes = new(startTime, endTime);
                }
                
                if (notes.Any(note => note.data.isRepeaterTarget))
                {
                    NotificationCenter.SendNotification("Can't create a repeater section in a repeater section. What are we, Inception?", NotificationType.Warning);
                    return false;
                }
                
                foreach (var note in notes)
                {
                    if (note.transient) continue;
                    EditorNotes.SelectTarget(note);
                    var repeaterData = new RepeaterData();
                    repeaterData.RelativeTime = note.data.time - startTime;
                    note.data.repeaterData = repeaterData;
                    targets.Add(note.data);
                }
            }
            var indicator = Instantiate(repeaterIndicatorPrefab, timelineParent);
            indicator.transform.localScale = new Vector3(1f * EditorScale.ScaleAmount, 1f, 1f);
            indicator.transform.localPosition = new Vector3(startTime.ToBeatTime(), 0f, 10f);
            bool isParent = !RepeaterExists(id);
            var section = new RepeaterSection(id, isParent, flipColors, mirrorHorizontally, mirrorVertically, startTime, activeStartTime, endTime, activeEndTime, indicator, targets, timeline);
            indicator.Initialize(miniTimelineParent, isParent);
            indicator.SetSection(section);
            indicator.SetWidth((activeEndTime - startTime).ToBeatTime());
            indicator.SetInteractable(overlay.isActive);
            indicator.SetText(id);
            section.UpdateActiveNotes();
            if (repeaters.ContainsKey(id))
            {
                repeaters[id].Add(section);
            }
            else
            {
                repeaters.Add(id, new List<RepeaterSection>() { section });
            }
            repeaters[id] = repeaters[id].OrderBy(s => s.startTime.tick).ToList();

            if (flipColors)
            {
                FlipRepeaterTargetColorsFromAction(section, section.flipTargetColors);
            }
            if (mirrorHorizontally)
            {
                MirrorRepeaterHorizontallyFromAction(section, section.mirrorHorizontally);
            }
            if (mirrorVertically)
            {
                MirrorRepeaterVerticallyFromAction(section, section.mirrorVertically);
            }

            section.indicator.SetColor(GetColorForRepeater(id));

            return true;
        }

        public bool AddRepeater(string id, QNT_Timestamp startTime)
        {
            return AddRepeater(id, startTime, startTime);
        }

        public bool AddRepeater(string id, QNT_Timestamp startTime, QNT_Timestamp endTime)
        {
            if (startTime > endTime)
            {
                var temp = startTime;
                startTime = endTime;
                endTime = temp;
            }
            AddRepeaterAction action = new AddRepeaterAction(this, id, startTime, endTime, startTime, endTime, false, false, false);
            //timeline.Tools.undoRedoManager.AddAction(action);
            UndoRedoManager.AddAction(action);
            return action.success;
        }

        private int GetRepeaterIndex(string id) => repeaters.Keys.ToList().IndexOf(id);
        public Color GetColorForRepeater(string id) => colors[GetRepeaterIndex(id) % colors.Count];

        public void CreateParentRepeater(RepeaterSection section)
        {
            var indicator = Instantiate(repeaterIndicatorPrefab, timelineParent);
            indicator.transform.localScale = new Vector3(1f * EditorScale.ScaleAmount, 1f, 1f);
            indicator.transform.localPosition = new Vector3(section.activeStartTime.ToBeatTime(), 0f, 10f);
            indicator.Initialize(miniTimelineParent, section.isParent);
            indicator.SetSection(section);
            indicator.SetWidth((section.activeEndTime - section.activeStartTime).ToBeatTime());
            indicator.SetInteractable(overlay.isActive);
            indicator.SetText(section.ID);
            foreach (var target in section.targets)
            {
                target.repeaterData = new();
                target.repeaterData.RelativeTime = target.time - section.startTime;
            }
            var loadedSection = new RepeaterSection(section.ID, section.isParent, section.flipTargetColors, section.mirrorHorizontally, section.mirrorVertically, section.startTime, section.activeStartTime, section.endTime, section.activeEndTime, indicator, section.targets, timeline);
            repeaters.Add(loadedSection.ID, new List<RepeaterSection>() { loadedSection });
            overlay.SpawnRepeaterEntry(loadedSection.ID);
            section.indicator.SetColor(GetColorForRepeater(loadedSection.ID));
        }

        public void CreateChildRepeater(RepeaterSection section)
        {
            var indicator = Instantiate(repeaterIndicatorPrefab, timelineParent);
            indicator.transform.localScale = new Vector3(1f * EditorScale.ScaleAmount, 1f, 1f);
            indicator.transform.localPosition = new Vector3(section.activeStartTime.ToBeatTime(), 0f, 10f);
            indicator.Initialize(miniTimelineParent, section.isParent);
            indicator.SetSection(section);
            indicator.SetWidth((section.activeEndTime - section.activeStartTime).ToBeatTime());
            indicator.SetInteractable(overlay.isActive);
            indicator.SetText(section.ID);
            foreach (var target in section.targets)
            {
                target.repeaterData = new();
                target.repeaterData.RelativeTime = target.time - section.startTime;
            }
            var loadedSection = new RepeaterSection(section.ID, section.isParent, section.flipTargetColors, section.mirrorHorizontally, section.mirrorVertically, section.startTime, section.activeStartTime, section.endTime, section.activeEndTime, indicator, section.targets, timeline);
            repeaters[loadedSection.ID].Add(loadedSection);
            overlay.SpawnRepeaterEntry(loadedSection.ID);

            if (section.flipTargetColors)
            {
                FlipRepeaterTargetColorsFromAction(section, section.flipTargetColors);
            }
            if (section.mirrorHorizontally)
            {
                MirrorRepeaterHorizontallyFromAction(section, section.mirrorHorizontally);
            }
            if (section.mirrorVertically)
            {
                MirrorRepeaterVerticallyFromAction(section, section.mirrorVertically);
            }
            section.indicator.SetColor(GetColorForRepeater(loadedSection.ID));
        }

        public void LoadRepeater(RepeaterSection section)
        {
            var indicator = Instantiate(repeaterIndicatorPrefab, timelineParent);
            indicator.transform.localScale = new Vector3(1f * EditorScale.ScaleAmount, 1f, 1f);
            indicator.transform.localPosition = new Vector3(section.activeStartTime.ToBeatTime(), 0f, 10f);
            List<TargetData> foundTargets = new();
            QNT_Duration buffer = new(1);
            var noteEnum = new NoteEnumerator(section.activeStartTime - buffer, section.activeEndTime + buffer);
            foreach(var note in noteEnum)
            {
                if (note.data.time < section.activeStartTime || note.data.time > section.activeEndTime)
                    continue;

                var target = note.data;
                target.repeaterData = new RepeaterData();
                target.repeaterData.RelativeTime = target.time - section.startTime;
                target.repeaterData.Section = section;
                foundTargets.Add(target);
            }

            FlipColors(foundTargets);
            foundTargets = foundTargets.OrderBy(t1 => t1.time.tick).ThenBy(t1 => (int)t1.behavior).ThenBy(t1 => (int)t1.handType).ToList();

            var loadedSection = new RepeaterSection(section.ID, !RepeaterExists(section.ID), section.flipTargetColors, section.mirrorHorizontally, section.mirrorVertically, section.startTime, section.activeStartTime, section.endTime, section.activeEndTime, indicator, foundTargets, timeline, section.targetDTOs, true);
            indicator.Initialize(miniTimelineParent, loadedSection.isParent);
            indicator.SetSection(loadedSection);
            indicator.SetWidth((loadedSection.activeEndTime - loadedSection.activeStartTime).ToBeatTime());
            indicator.SetText(section.ID);
            indicator.SetInteractable(false);
            if (repeaters.ContainsKey(loadedSection.ID))
            {
                repeaters[loadedSection.ID].Add(loadedSection);
            }
            else
            {
                repeaters.Add(loadedSection.ID, new List<RepeaterSection>() { loadedSection });
                overlay.SpawnRepeaterEntry(loadedSection.ID);
            }
            repeaters[loadedSection.ID] = repeaters[loadedSection.ID].OrderBy(s => s.startTime.tick).ToList();
            loadedSection.indicator.SetColor(GetColorForRepeater(loadedSection.ID));
            FlipColors(foundTargets);
            
            void FlipColors(IEnumerable<TargetData> targets)
            {
                foreach (var t in targets.Where(t => !t.behavior.IsMeleeOrMine()).Where(t => section.flipTargetColors))
                {
                    t.handType = t.handType == TargetHandType.Left ? TargetHandType.Right : TargetHandType.Left;
                }
            }
        }

        private void OnScaleChanged(int _)
        {
            foreach (var repeater in repeaters)
            {
                foreach (var segment in repeater.Value)
                {
                    segment.FixScaling();
                }
            }
        }

        public bool RepeaterExists(string id)
        {
            return repeaters.ContainsKey(id);
        }

        public void RenameRepeaterFromAction(string id, string newId)
        {
            if (repeaters.ContainsKey(id))
            {
                var sections = repeaters[id];
                repeaters.Remove(id);

                foreach (var section in sections)
                {
                    section.RenameID(newId);
                }
                repeaters.Add(newId, sections);
            }
            overlay.UpdateRepeaterID(id, newId);
        }

        public void UpdateMiniIndicatorPositions()
        {
            foreach (var repeater in repeaters)
            {
                foreach (var section in repeater.Value)
                {
                    section.indicator.UpdateMiniIndicatorPosition();
                }
            }
        }

        public void RenameRepeater(string id, string newId)
        {
            RenameRepeaterAction action = new(this, id, newId);
            //timeline.Tools.undoRedoManager.AddAction(action);
            UndoRedoManager.AddAction(action);
        }

        public void SetRepeatersInteractable(bool interactable)
        {
            foreach (var repeater in repeaters)
            {
                foreach (var section in repeater.Value)
                {
                    section.indicator.SetInteractable(interactable);
                }
            }
        }

        public bool MakeSectionUniqueFromAction(RepeaterSection section, out string newID)
        {
            newID = section.ID;
            if (!repeaters.ContainsKey(section.ID))
            {
                NotificationCenter.SendNotification("Couldn't find original repeater. Oops. It's not you, it's me.", NotificationType.Error);
                return false;
            }

            newID += " 2";
            if (repeaters.ContainsKey(newID))
            {
                NotificationCenter.SendNotification($"The unique ID we want to use already exists. Who the hell voluntarily names their repeater {newID}? Anyhow, we'll give it an even longer name, shall we?", NotificationType.Warning);
                newID += ": Electric Boogaloo";

                if (repeaters.ContainsKey(newID))
                {
                    NotificationCenter.SendNotification($"Seriously? ..alright, no unique repeater for you. I hope you step on a lego.", NotificationType.Warning);
                    newID = section.ID;
                    return false;
                }
            }

            section.flipTargetColors = false;
            section.mirrorHorizontally = false;
            section.mirrorVertically = false;

            var temp = repeaters[section.ID].Where(s => s.activeStartTime == section.activeStartTime).First();
            repeaters[section.ID].Remove(temp);
            temp.RenameID(newID);
            temp.isParent = true;
            temp.indicator.SetIsParent(true);
            repeaters.Add(newID, new List<RepeaterSection>() { temp });
            overlay.SpawnRepeaterEntry(newID);
            section.indicator.UpdateSettingsIcons();
            section.indicator.SetColor(GetColorForRepeater(section.ID));
            return true;
        }

        public void UndoMakeSectionUniqueFromAction(RepeaterSection section, string newID, bool flipColors, bool mirrorHorizontally, bool mirrorVertically)
        {
            var oldID = section.ID;
            if (!repeaters.ContainsKey(oldID)) return;

            repeaters.Remove(oldID);
            section.RenameID(newID);
            section.isParent = false;
            section.indicator.SetIsParent(false);
            repeaters[newID].Add(section);

            overlay.RemoveEntry(oldID);

            section.flipTargetColors = flipColors;
            section.mirrorHorizontally = mirrorHorizontally;
            section.mirrorVertically = mirrorVertically;
            section.indicator.UpdateSettingsIcons();
            section.indicator.SetColor(GetColorForRepeater(section.ID));
        }

        public bool MakeSectionUnique(RepeaterSection section, out string newID)
        {
            MakeUniqueAction action = new(this, section, section.ID);
            UndoRedoManager.AddAction(action);
            newID = action.newID;

            return action.success;

        }

        public void RemoveRepeater(RepeaterSection section)
        {
            RemoveRepeaterAction action = new RemoveRepeaterAction(this, section);
            UndoRedoManager.AddAction(action);
        }

        public void RemoveRepeaterFromAction(string id, QNT_Timestamp startTime)
        {
            if (!repeaters.ContainsKey(id)) return;

            var section = repeaters[id].First(section => section.startTime == startTime);

            foreach (var target in section.targets)
            {
                target.repeaterData = null;
                //target.repeaterData = null;
                if (repeaters[id].Count > 1)
                {
                    //only delete targets if we still have repeaters with the same ID
                    if (target.isPathbuilderTarget)
                    {
                        timeline.pathbuilder.RemoveAllNodes(target.pathbuilderData);
                    }
                    if (target.legacyPathbuilderData != null)
                    {
                        target.legacyPathbuilderData.DeleteCreatedNotes();
                    }
                    EditorTargets.DeleteTargetFromAction(target);
                }
            }
            section.indicator.Destroy();

            repeaters[id].Remove(section);
            if (repeaters[id].Count == 0)
            {
                overlay.RemoveEntry(id);
                repeaters.Remove(id);
            }
            else
            {
                repeaters[id] = repeaters[id].OrderBy(s => s.startTime.tick).ToList();
            }

        }

        public void RemoveAllChildRepeaters(string id)
        {
            if (!repeaters.ContainsKey(id)) return;
            List<RepeaterSection> sections = repeaters[id].Where(s => !s.isParent).ToList();
            MultiRemoveRepeaterAction action = new MultiRemoveRepeaterAction(this, sections, id);
            UndoRedoManager.AddAction(action);
        }

        /// <summary>
        /// Deletes all repeaters with the given ID.
        /// </summary>
        /// <param name="id">The ID of the repeaters you want to delete.</param>
        public void RemoveAllRepeatersWithID(string id)
        {
            if (!repeaters.ContainsKey(id)) return;
            var sections = repeaters[id].Where(s => !s.isParent).ToList();
            var parent = repeaters[id].First(s => s.isParent);
            MultiRemoveRepeaterAction action = new MultiRemoveRepeaterAction(this, sections, id, parent);
            UndoRedoManager.AddAction(action);
        }

        public List<TargetData> GetMatchingRepeaterTargets(TargetData data)
        {
            List<TargetData> matchingTargets = new List<TargetData>();
            foreach (var section in repeaters[data.repeaterData.Section.ID])
            {
                matchingTargets.AddRange(section.targets.Where(target => target.repeaterData.targetID == data.repeaterData.targetID && target.time != data.time));
            }
            return matchingTargets;
        }

        public TargetData GetParentTarget(TargetData data)
        {
            foreach (var section in repeaters[data.repeaterData.Section.ID])
            {
                if (section.isParent)
                {
                    return section.targets.FirstOrDefault(target => target.repeaterData.targetID == data.repeaterData.targetID);
                }
            }
            return null;
        }

        public RepeaterSection GetParentRepeater(RepeaterSection section)
        {
            if (repeaters.ContainsKey(section.ID))
            {
                return repeaters[section.ID].First(s => s.isParent);
            }
            return null;
        }

        public List<RepeaterSection> GetMatchingRepeaterSections(RepeaterData data)
        {
            if (!repeaters.ContainsKey(data.Section.ID)) return new List<RepeaterSection>();
            else
            {
                return repeaters[data.Section.ID];
            }
        }

        public bool IsTargetInRepeaterZone(TargetData data, out RepeaterData repeaterData)
        {
            repeaterData = null;

            foreach (var repeater in repeaters)
            {
                foreach (RepeaterSection section in repeater.Value)
                {
                    if (section.Contains(data.time))
                    {
                        repeaterData = new RepeaterData();
                        repeaterData.RelativeTime = data.time - section.startTime;
                        repeaterData.Section = section;
                        if (data.repeaterData != null)
                        {
                            repeaterData.targetID = data.repeaterData.targetID;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsTargetInRepeaterZone(QNT_Timestamp time)
        {
            foreach (var repeater in repeaters)
            {
                foreach (RepeaterSection section in repeater.Value)
                {
                    if (section.Contains(time))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsTargetInRepeaterZone(QNT_Timestamp time, QNT_Timestamp excludeSectionStartTime)
        {
            foreach (var repeater in repeaters)
            {
                foreach (RepeaterSection section in repeater.Value)
                {
                    if (section.startTime == excludeSectionStartTime) continue;

                    if (section.Contains(time))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Adds a new target to all repeaters with the same ID.
        /// </summary>
        /// <param name="data">The target to add to a repeater.</param>
        public Target CreateRepeaterTarget(TargetData data)
        {
            Target parentTarget = null;
            if (IsTargetInRepeaterZone(data, out RepeaterData repeaterData))
            {
                if (!repeaterData.Section.isParent)
                {
                    if (repeaterData.Section.flipTargetColors)
                    {
                        if (data.handType == TargetHandType.Left)
                        {
                            data.handType = TargetHandType.Right;
                        }
                        else if (data.handType == TargetHandType.Right)
                        {
                            data.handType = TargetHandType.Left;
                        }
                    }
                    if (repeaterData.Section.mirrorHorizontally)
                    {
                        var pos = data.position;
                        pos.x *= -1f;
                        data.position = pos;
                    }
                    if (repeaterData.Section.mirrorVertically)
                    {
                        var pos = data.position;
                        pos.y *= -1f;
                        data.position = pos;
                    }
                }
                data.repeaterData = repeaterData;
                parentTarget = repeaterData.Section.CreateRepeaterParentTarget(data);
                foreach (var section in GetMatchingRepeaterSections(repeaterData))
                {
                    if (section.startTime == repeaterData.Section.startTime) continue;
                    section.CreateRepeaterChildTarget(data);
                }
            }

            return parentTarget;
        }
        /// <summary>
        /// Deletes a repeater target in all sections.
        /// </summary>
        /// <param name="data">The target to delete.</param>
        public void DeleteRepeaterTarget(TargetData data)
        {
            foreach (var section in GetMatchingRepeaterSections(data.repeaterData))
            {
                section.RemoveRepeaterTarget(data);
            }
        }

        public List<RepeaterSection> GetSections()
        {
            List<RepeaterSection> sections = new();
            foreach (var repeater in repeaters)
            {
                foreach (var section in repeater.Value)
                {
                    section.SaveTargetTimes();
                    sections.Add(section);
                }
            }
            return sections;
        }

        public RepeaterSection GetSectionAtTime(string id, QNT_Timestamp time) 
            => repeaters[id].Where(r => r.activeStartTime <= time && r.activeEndTime >= time).FirstOrDefault();
        /// <summary>
        /// Clears all repeater data. Used for resetting.
        /// </summary>
        public void RemoveAllRepeaters()
        {
            foreach (var repeater in repeaters)
            {
                for (int i = repeater.Value.Count - 1; i >= 0; i--)
                {
                    var section = repeater.Value[i];
                    section.indicator.Destroy();
                }
            }
            overlay.RemoveAllEntries();
            repeaters.Clear();
        }

        public void MirrorRepeaterHorizontally(string id, QNT_Timestamp startTime, bool mirror)
        {
            if (!repeaters.ContainsKey(id)) return;

            FlipRepeaterAction action = new(this, repeaters[id].First(s => s.startTime == startTime), mirror, FlipRepeaterAction.Mode.Horizontal);
            UndoRedoManager.AddAction(action);
        }

        //public void MirrorRepeaterHorizontallyFromAction(string id, QNT_Timestamp startTime, bool mirror)
        public void MirrorRepeaterHorizontallyFromAction(RepeaterSection section, bool mirror)
        {
            //var section = repeaters[id].First(s => s.startTime == startTime);
            section.mirrorHorizontally = mirror;
            foreach (var target in section.targets)
            {
                var pos = target.position;
                pos.x *= -1f;
                target.position = pos;

                if (target.isPathbuilderTarget)
                {
                    target.pathbuilderData.Flip(new Vector2(-1f, 1f));
                }
            }

            UpdateSectionChainConnectors(section);
            UpdateOverlayToggles(section);
        }
        public void MirrorRepeaterVertically(string id, QNT_Timestamp startTime, bool mirror)
        {
            if (!repeaters.ContainsKey(id)) return;

            FlipRepeaterAction action = new(this, repeaters[id].First(s => s.startTime == startTime), mirror, FlipRepeaterAction.Mode.Vertical);
            UndoRedoManager.AddAction(action);
        }

        //public void MirrorRepeaterVerticallyFromAction(string id, QNT_Timestamp startTime, bool mirror)
        public void MirrorRepeaterVerticallyFromAction(RepeaterSection section, bool mirror)
        {
            section.mirrorVertically = mirror;
            foreach (var target in section.targets)
            {
                if (target.behavior == TargetBehavior.Melee) continue;

                var pos = target.position;
                pos.y *= -1f;
                target.position = pos;

                if (target.isPathbuilderTarget)
                {
                    target.pathbuilderData.Flip(new Vector2(1f, -1f));
                }
            }

            UpdateSectionChainConnectors(section);
            UpdateOverlayToggles(section);
        }

        public void FlipRepeaterTargetColors(string id, QNT_Timestamp startTime, bool flip)
        {
            if (!repeaters.ContainsKey(id)) return;

            FlipRepeaterAction action = new(this, repeaters[id].First(s => s.startTime == startTime), flip, FlipRepeaterAction.Mode.Colors);
            UndoRedoManager.AddAction(action);
        }

        public void FlipRepeaterTargetColorsFromAction(RepeaterSection section, bool flip)
        {
            section.flipTargetColors = flip;
            foreach (var target in section.targets)
            {
                target.repeaterData.Section.flipTargetColors = flip;
                if (target.behavior == TargetBehavior.Melee || target.behavior == TargetBehavior.Mine) continue;
                TargetHandType newHand = target.handType;
                if (target.handType == TargetHandType.Left)
                {
                    newHand = TargetHandType.Right;
                }
                else if (target.handType == TargetHandType.Right)
                {
                    newHand = TargetHandType.Left;
                }
                target.handType = newHand;
            }

            UpdateSectionChainConnectors(section);
            UpdateOverlayToggles(section);
        }

        private void UpdateSectionChainConnectors(RepeaterSection section)
        {
            foreach (var target in section.targets)
            {
                if (target.behavior == TargetBehavior.ChainStart || target.behavior == TargetBehavior.Legacy_Pathbuilder)
                    EditorTargets.UpdateChainConnector(target);
            }
        }

        private void UpdateOverlayToggles(RepeaterSection section)
        {
            if (overlay.isActive)
                overlay.UpdateToggles();
            
            section.indicator.UpdateSettingsIcons();
        }

        public void BakeRepeaterSection(RepeaterSection section)
        {
            if (!repeaters.ContainsKey(section.ID)) return;
            BakeRepeaterAction action = new BakeRepeaterAction(this, section);
            //timeline.Tools.undoRedoManager.AddAction(action);
            UndoRedoManager.AddAction(action);

        }

        public void BakeRepeaterSectionFromAction(RepeaterSection section)
        {
            if (!repeaters.ContainsKey(section.ID)) return;

            repeaters[section.ID].Remove(section);
            foreach (var target in section.targets)
            {
                target.repeaterData = null;
            }
            section.indicator.Destroy();
        }

        internal void Activate()
        {
            base.OnActivated();
            TimelineCameraMouseHandler.allowMiniTimelineClick = true;
        }
        internal void Deactivate()
        {
            TimelineCameraMouseHandler.allowMiniTimelineClick = false;
            base.OnDeactivated();
        }

        protected override void RegisterCallbacks()
        {
            actions.Repeater.DeleteRepeaterSection.started += _ => overlay.OnDeleteClicked();
            actions.Repeater.MakeRepeaterUnique.started += _ => overlay.OnMakeUniqueClicked();
            actions.Repeater.FlipTargetColorsOfRepeater.started += _ => overlay.OnFlipTargetColorsToggled();
            actions.Repeater.MirrorTargetsHorizontally.started += _ => overlay.OnMirrorHorizontallyToggled();
            actions.Repeater.MirrorTargetsVertically.started += _ => overlay.OnMirrorVerticallyToggled();
            actions.Repeater.BakeSection.started += _ => overlay.OnBakeClicked();
        }

        public void OnInputFocused(bool isFocused)
        {
            if (isFocused)
            {
                KeybindManager.DisableKeybind("TogglePlay");
            }
            else
            {
                KeybindManager.EnableKeybind("TogglePlay");
            }
        }

        public void EnableScrubbing(bool enable)
        {
            if (enable)
            {
                KeybindManager.EnableKeybind("Scrub");
                KeybindManager.EnableKeybind("ScrubByTick");
            }
            else
            {
                KeybindManager.DisableKeybind("Scrub");
                KeybindManager.DisableKeybind("ScrubByTick");
            }
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            overlay.Hide();
        }

        protected override void SetRebindConfiguration(ref RebindConfiguration options, RepeaterKeybinds myKeybinds)
        {

        }
    }

    [Serializable]
    public class ColorConfig
    {
        public Color normalColor;
        public Color selectedColor;
    }
}

