using NotReaper.Grid;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools.ChainBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.Repeaters
{
    [Serializable]
    public class RepeaterSection
    {
        public string ID;
        [NonSerialized]
        public bool isParent;
        public bool flipTargetColors;
        public bool mirrorHorizontally;
        public bool mirrorVertically;
        public QNT_Timestamp startTime;
        public QNT_Timestamp endTime;

        public QNT_Timestamp activeStartTime;
        public QNT_Timestamp activeEndTime;
        public List<TargetData> targets;
        public List<ulong> targetTimes = new List<ulong>();
        public List<Cue> targetDTOs = new List<Cue>();
        [NonSerialized] public RepeaterIndicator indicator;
        [NonSerialized] private Timeline timeline;

        private long targetIndexID = 0;

        public RepeaterSection(string ID, bool isParent, QNT_Timestamp startTime, QNT_Timestamp endTime, QNT_Timestamp activeEndTime, RepeaterIndicator indicator, List<TargetData> targets, Timeline timeline)
        {
            this.ID = ID;
            this.startTime = activeStartTime = startTime;
            this.endTime = endTime;
            this.activeEndTime = activeEndTime;
            this.timeline = timeline;
            this.indicator = indicator;
            this.targets = targets;
            this.isParent = isParent;
            foreach(var target in targets)
            {
                target.repeaterData.Section = this;
                target.repeaterData.targetID = targetIndexID;
                targetIndexID++;
            }
        }

        public RepeaterSection(string ID, bool isParent, bool flipTargetColors, bool mirrorHorizontally, bool mirrorVertically, QNT_Timestamp startTime, QNT_Timestamp activeStartTime, QNT_Timestamp endTime, QNT_Timestamp activeEndTime, RepeaterIndicator indicator, List<TargetData> targets, Timeline timeline, List<Cue> targetDTOs = null, bool isLoading = false)
        {
            this.ID = ID;
            this.startTime = startTime;
            this.activeStartTime = activeStartTime;
            this.endTime = endTime;
            this.activeEndTime = activeEndTime;
            this.timeline = timeline;
            this.indicator = indicator;
            this.targets = targets;
            this.isParent = isParent;
            this.flipTargetColors = flipTargetColors;
            this.mirrorHorizontally = mirrorHorizontally;
            this.mirrorVertically = mirrorVertically;
            this.targetDTOs = targetDTOs;
            LoadHiddenTargets();
            foreach (var target in targets)
            {
                target.repeaterData.Section = this;

                if(isParent || isLoading)
                    target.repeaterData.targetID = targetIndexID;

                targetIndexID++;
            }
        }

        public void Copy(RepeaterSection section)
        {
            ID = section.ID;
            startTime = section.startTime;
            endTime = section.endTime;
            activeStartTime = section.activeStartTime;
            activeEndTime = section.activeEndTime;
        }

        public RepeaterSection() { }

        public bool Contains(QNT_Timestamp time)
        {
            return time >= activeStartTime && time <= activeEndTime;
        }

        public void SetActiveStartTime(QNT_Timestamp time)
        {
            activeStartTime = time;
            UpdateActiveNotes();
        }

        public void SetActiveEndTime(QNT_Timestamp time)
        {
            activeEndTime = time;
            UpdateActiveNotes();
        }

        public void SetIsParent(bool isParent)
        {
            this.isParent = isParent;
            indicator.SetIsParent(isParent);
        }

        public long GetCurrentTargetIndexID()
        {
            return targetIndexID;
        }
        /// <summary>
        /// Creates a parent target in this section.
        /// </summary>
        /// <param name="data">The parent's data.</param>
        public Target CreateRepeaterParentTarget(TargetData data)
        {
            var repeaterData = new RepeaterData();
            repeaterData.RelativeTime = data.time - startTime;
            repeaterData.Section = this;
            if(data.repeaterData.targetID == -1)
            {
                repeaterData.targetID = targetIndexID;
                targetIndexID++;
            }
            else
            {
                repeaterData.targetID = data.repeaterData.targetID;
            }
            data.repeaterData = repeaterData;
            targets.Add(data);
            return EditorTargets.AddTargetFromAction(data);
        }
        /// <summary>
        /// Creates a child target in this repeater section.
        /// </summary>
        /// <param name="parentData">The parent section's target data.</param>
        public void CreateRepeaterChildTarget(TargetData parentData)
        {
            TargetData repeaterTarget = new TargetData();
            repeaterTarget.Copy(parentData);
            repeaterTarget.repeaterData = new RepeaterData();
            repeaterTarget.repeaterData.RelativeTime = parentData.repeaterData.RelativeTime;
            repeaterTarget.SetTimeFromAction(startTime + repeaterTarget.repeaterData.RelativeTime);
            repeaterTarget.repeaterData.Section = this;
            repeaterTarget.repeaterData.targetID = parentData.repeaterData.targetID;
            targetIndexID++;
            if (repeaterTarget.legacyPathbuilderData != null)
            {
                repeaterTarget.legacyPathbuilderData = new();
                repeaterTarget.legacyPathbuilderData.Copy(parentData.legacyPathbuilderData, false);
            }
            if (repeaterTarget.isPathbuilderTarget)
            {
                repeaterTarget.pathbuilderData = new();
                repeaterTarget.pathbuilderData.Copy(parentData.pathbuilderData);
            }
            
            if (flipTargetColors)
            {
                if(parentData.handType == TargetHandType.Left)
                {
                    repeaterTarget.handType = TargetHandType.Right;
                }
                else if(parentData.handType == TargetHandType.Right)
                {
                    repeaterTarget.handType = TargetHandType.Left;
                }
            }
            if (mirrorHorizontally)
            {
                var pos = repeaterTarget.position;
                pos.x *= -1f;
                repeaterTarget.position = pos;

                if (repeaterTarget.isPathbuilderTarget)
                {
                    repeaterTarget.pathbuilderData.Flip(new(-1, 1));
                }
            }
            if (mirrorVertically)
            {
                var pos = repeaterTarget.position;
                pos.y *= -1f;
                repeaterTarget.position = pos;
                if (repeaterTarget.isPathbuilderTarget)
                {
                    repeaterTarget.pathbuilderData.Flip(new(1, -1));
                }
            }

            targets.Add(repeaterTarget);
            if(repeaterTarget.time >= activeStartTime && repeaterTarget.time <= activeEndTime)
            {
                EditorTargets.AddTargetFromAction(repeaterTarget);
                if (repeaterTarget.isPathbuilderTarget)
                {                   
                    timeline.pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(repeaterTarget, repeaterTarget.pathbuilderData);   
                }
                /*if(repeaterTarget.legacyPathbuilderData != null)
                {
                    ChainBuilder.GenerateChainNotes(repeaterTarget, true);
                }*/
            }
        }
        /// <summary>
        /// Adds an already existing target to a repeater. Mainly used for baking targets from pathbuilder/chainbuilder.
        /// </summary>
        /// <param name="data">The target to add to this repeater section.</param>
        public void AddExistingTargetToRepeater(TargetData data)
        {
            data.repeaterData = new();
            data.repeaterData.RelativeTime = data.time - startTime;
            data.repeaterData.Section = this;
            data.repeaterData.targetID = targetIndexID;
            targetIndexID++;
            targets.Add(data);
        }

        public void RemoveRepeaterTarget(TargetData data)
        {
            var target = targets.First(t => t.repeaterData.targetID == data.repeaterData.targetID);
            targets.Remove(target);
            EditorTargets.DeleteTargetFromAction(target);
        }

        public void RemoveInactiveNotes()
        {
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];
                
                if (target.transient) continue;
                
                if(target.time >= activeStartTime && target.time <= activeEndTime)
                {
                    if (target.isPathbuilderTarget)
                    {
                        if(target.time + target.pathbuilderData.TotalSegmentLength > activeEndTime)
                        {
                            timeline.pathbuilder.RemoveAllNodes(target.pathbuilderData);
                            EditorTargets.DeleteTargetFromAction(target);
                            targets.RemoveAt(i);
                            return;
                        }

                    }
                    else if(target.behavior == TargetBehavior.Sustain)
                    {
                        if(target.time + target.beatLength > activeEndTime)
                        {
                            EditorTargets.DeleteTargetFromAction(target);
                            targets.RemoveAt(i);
                            return;
                        }
                    }
                }
                else
                {
                    EditorTargets.DeleteTargetFromAction(target);
                    targets.RemoveAt(i);
                }
            }
        }

        public void UpdateActiveNotes()
        {
            foreach(var target in targets)
            {
                if (target.transient)
                    continue;

                if(target.time >= activeStartTime && target.time <= activeEndTime)
                {
                    if (target.isPathbuilderTarget)
                    {
                        if(target.time + target.pathbuilderData.TotalSegmentLength > activeEndTime)
                        {
                            timeline.pathbuilder.RemoveAllNodes(target.pathbuilderData);
                            EditorTargets.DeleteTargetFromAction(target);
                            continue;
                        }

                    }
                    else if(target.behavior == TargetBehavior.Sustain)
                    {
                        if(target.time + target.beatLength > activeEndTime)
                        {
                            EditorTargets.DeleteTargetFromAction(target);
                            continue;
                        }
                    }
                    if(TargetFinder.FindNoteByID(target) == null)
                    {
                        EditorTargets.AddTargetFromAction(target);

                        if (target.isPathbuilderTarget)
                            timeline.pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(target, target.pathbuilderData);
                    }
                }
                else
                {
                    EditorTargets.DeleteTargetFromAction(target);
                }
            }
        }

        public void FixScaling() => indicator.FixScaling();

        public void RenameID(string newID)
        {
            ID = newID;
            if(indicator != null)
            {
                indicator.SetText(newID);
            }
        }

        public void SaveTargetTimes()
        {
            if (targetDTOs == null)
                targetDTOs = new();

            targetTimes.Clear();
            targetDTOs.Clear();
            var offset = Timeline.offset;
            foreach(var target in targets)
            {
                targetTimes.Add(target.time.tick);
                if(target.time < activeStartTime || target.time > activeEndTime)
                    targetDTOs.Add(NotePosCalc.ToCue(target, offset));
            }
        }

        private void LoadHiddenTargets()
        {
            if (targetDTOs == null)
                targetDTOs = new();

            foreach(var cue in targetDTOs)
            {
                var data = EditorTargets.ConvertCueToTargetData(cue);
                data.repeaterData = new RepeaterData();
                data.repeaterData.RelativeTime = data.time - startTime;
                data.repeaterData.Section = this;
                targets.Add(data);
            }
            targets = targets.OrderBy(t => t.time.tick).ThenBy(t => (int)t.behavior).ThenBy(t => (int)t.handType).ToList();
            //targets.Sort((t1, t2) => t1.time.CompareTo(t2.time));
        }
    }
}
