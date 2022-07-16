using System.Collections;
using System.Collections.Generic;
using System.Linq;
using I18N.Common;
using NotReaper.Models;
using NotReaper.Statistics;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools;
using UnityEngine;
using UnityEngine.Profiling;

namespace NotReaper.HitsoundTimeline
{
    public class HitsoundManager : TimelineManager<HitsoundData>
    {
        protected override TimelineType TimelineType => TimelineType.Hitsound;
        protected override GridTimeline.WidthType TimelineWidthType => GridTimeline.WidthType.Full;

        [NRInject] private HitsoundInputManager inputManager;
        [NRInject] private GridTimeline timeline;

        private Dictionary<Target, HitsoundMarker> markerMap = new();
        private List<HitsoundMarker> selectedDualContent = new();

        protected override void Start()
        {
            base.Start();
            EditorTargets.onTargetAdded += OnTargetAdded;
            onAfterTrackSwitch += UpdateSelectedTargetDuality;
        }

        private void UpdateSelectedTargetDuality()
        {
            foreach (var content in selectedDualContent)
            {
                content.SetToDual(true);
            }
        }

        protected override void OnReset()
        {
            base.OnReset();
            markerMap.Clear();
        }

        public override void StartMove(Vector3 mousePosition)
        {
            if (isMovingContent) return;
            if (KeybindManager.Global.Modifier.IsCtrlDown())
            {
                for (int i = SelectedContent.Count - 1; i >= 0; i--)
                {
                    var content = SelectedContent[i];
                    if (ShouldBeDual(content as HitsoundMarker, out var foundMarker))
                    {
                        foundMarker.SetSelected(true);
                        SelectedContent.Add(foundMarker);
                        selectedDualContent.Add(foundMarker);
                        selectedDualContent.Add(content as HitsoundMarker);
                    }
                }
            }
            
            base.StartMove(mousePosition);
        }

        public override void EndMove()
        {
            base.EndMove();
            selectedDualContent.Clear();
        }

        protected override void AddReselectContentToMove(Content content, Timeframe oldTimeframe, Vector3 mousePosition)
        {
            if (!isMovingContent) return;

            if (KeybindManager.Global.Modifier.IsCtrlDown())
            {
                var marker = content as HitsoundMarker;
                if (ShouldBeDual(marker, out var foundMarker))
                {
                    foundMarker.SetSelected(true);
                    SelectedContent.Add(foundMarker);
                    moveData.Add(new MoveData
                    {
                        content = foundMarker,
                        oldTimeframe = oldTimeframe,
                        oldTrack = content.Track.Type,
                        distanceToMouse = mousePosition.y - content.transform.position.y
                    });
                    
                    if (!selectedDualContent.Contains(marker))
                    {
                        selectedDualContent.Add(marker);
                        selectedDualContent.Add(foundMarker);
                    }
                }
            }
            
            base.AddReselectContentToMove(content, oldTimeframe, mousePosition);
        }

        public override void TrySwitchTrack(Vector2 mousePosition)
        {
            base.TrySwitchTrack(mousePosition);
            foreach (var move in moveData)
            {
                UpdateDuality(move);
            }
        }

        private void OnTargetAdded(Target target) => CreateMarker(target);

        private void OnTargetRemoved(HitsoundMarker marker)
        {
            
            marker.onBehaviorChanged -= OnTargetBehaviorChanged;
            marker.onHitsoundChanged -= OnTargetHitsoundChanged;
            marker.onTimeChanged -= OnTargetTimeChanged;
            marker.onTrackSwitched -= UpdateDuality;
            marker.onDestroy -= OnTargetRemoved;

            if (ShouldBeDual(marker.Data.targetData.time, marker.Data.targetData.behavior is TargetBehavior.Melee, marker.Data.type, marker, out var foundMarker))
            {
                foundMarker.SetToSingle();
            }

            if (markerMap.ContainsKey(marker.Data.target))
            {
                markerMap.Remove(marker.Data.target);
            }
            RemoveContentFromAction(marker);
        }

        private void OnTargetSelected(HitsoundMarker marker, bool selected)
        {
            if (selected)
            {
                SelectContent(marker, true);
            }
            else
            {
                DeselectMultiselectContent(marker);
            }
        }


        [NRListener]
        private void OnIsInUIChanged(bool isInUi)
        {
            if(isInUi && IsActive)
                ToggleTimeline();
        }

        [NRListener]
        private void OnToolChanged(EditorTool tool)
        {
            if(IsActive && tool != EditorTool.None)
                ToggleTimeline();
        }

        protected override void Show(bool show)
        {
            if (show) inputManager.Activate();
            else inputManager.Deactivate();
            
            base.Show(show);
        }

        protected override bool CheckSpecialPlaceRequirements(QNT_Timestamp startTime, TrackContent content)
            => false;

        protected override bool CanSwitchTrack(Content content, int currentTrack, int nextTrack)
            => ((TimelineHitsound)currentTrack).IsMelee() == ((TimelineHitsound)nextTrack).IsMelee();

        protected override bool CheckAlwaysMoveRequirements(Content content)
            => true;

        public HitsoundMarker LoadHitsoundMarker(HitsoundData data)
        {
            var hitsound = base.LoadContent((int)data.type) as HitsoundMarker;
            hitsound.LoadData(data);
            return hitsound;
        }

        protected override void AddContentAction(QNT_Timestamp startTime, TrackContent trackContent)
            => PlaceContentFromAction(new Timeframe(startTime, startTime + EditorBeatSnap.Duration), trackContent.tracks[GridTimeline.Type]);

        protected override void MultiAddContentAction(List<HitsoundData> content)
        {
            List<TargetSetHitsoundIntent> intents = new();
            List<Target> processedDualNotes = new();
            QNT_Timestamp lastTime = new(0);
            foreach (var data in content)
            {
                var currentTime = new QNT_Timestamp((ulong)data.startTick);

                if (currentTime != lastTime)
                {
                    lastTime = currentTime;
                    processedDualNotes.Clear();
                }
                
                var targets = TargetFinder.FindNotes(currentTime);
                bool isMelee = data.type.IsMelee();
                foreach (var target in targets)
                {
                    if (target.data.behavior.IsMelee() != isMelee) continue;
                    if (data.isDual)
                    {
                        if (isMelee)
                        {
                            if (processedDualNotes.Contains(target)) continue;
                            processedDualNotes.Add(target);
                            target.data.velocity = data.targetData.velocity;
                            intents.Add(GenerateIntent(target, data));
                            break;
                        }
                        else
                        {
                            if (target.data.handType != data.targetData.handType) continue;
                            intents.Add(GenerateIntent(target, data));
                        }
                    }
                    else
                    {
                        intents.Add(GenerateIntent(target, data));
                        break;
                    }
                }
            }
            
            UndoRedoManager.AddAction(new NRActionSetTargetHitsound(this, intents));
        }

        private TargetSetHitsoundIntent GenerateIntent(Target target, HitsoundData hitsoundData)
            => new(target, target.data.velocity, hitsoundData.targetData.velocity);

        private TargetSetHitsoundIntent GenerateIntentFromMove(Target target, int newHitsound)
            => new(target, target.data.velocity, ((TimelineHitsound)newHitsound).ToInternalVelocity());

        protected override void MoveContentAction(List<MoveData> moveData)
        {
            List<TargetSetHitsoundIntent> intents = new();
            foreach (var move in moveData)
            {
                var marker = move.content as HitsoundMarker;
                intents.Add(GenerateIntentFromMove(marker.Data.target, move.newTrack));
            }
            UndoRedoManager.AddAction(new NRActionSetTargetHitsound(this, intents));
        }

        protected override void RemoveContentAction(Content content)
        {
            
        }

        protected override void MultiRemoveContentAction(List<Content> content)
        {
        }

        public void CreateMarker(Target target)
        {
            var data = target.data;
            var behavior = data.behavior;
            var time = (int)data.time.tick;

            if (behavior.IsMine()) return;

            bool shouldBeDual = ShouldBeDual(data.time, behavior is TargetBehavior.Melee, data.velocity.ToTimelineHitsound(data.behavior.IsMelee()), null, out var foundMarker);
            if (shouldBeDual)
            {
                foundMarker.SetToDual(true);
            }

            var hitsound = data.velocity.ToTimelineHitsound(behavior is TargetBehavior.Melee);
            var marker = LoadContent((int)hitsound) as HitsoundMarker;
                
            marker.LoadData(new HitsoundData
            {
                startTick = time,
                endTick = time,
                target = target
            });


            marker.onHitsoundChanged += OnTargetHitsoundChanged;
            marker.onTimeChanged += OnTargetTimeChanged;
            marker.onBehaviorChanged += OnTargetBehaviorChanged;
            marker.onTrackSwitched += UpdateDuality;
            marker.onDestroy += OnTargetRemoved;

            if(shouldBeDual) marker.SetToDual(false);

            if (!markerMap.ContainsKey(target))
            {
                markerMap.Add(target, marker);
            }
        }

        private void OnTargetHitsoundChanged(HitsoundMarker marker) => timeline.SwitchTrack(TimelineType, marker, marker.Type);

        private void OnTargetBehaviorChanged(HitsoundMarker marker, TargetBehavior oldBehavior)
        {
            bool show = ShouldBeDual(marker.startTime, marker.Data.targetData.behavior.IsMelee(), marker.Data.type, marker, out var foundMarker);
            if (show)
            {
                foundMarker.SetToSingle();
            }
            marker.gameObject.SetActive(show);
        }


        private void OnTargetTimeChanged(HitsoundMarker marker, QNT_Timestamp newTime, QNT_Timestamp oldTime) =>  marker.SetStartTime(newTime);

        private HitsoundTrackManager trackManager => tracks as HitsoundTrackManager;

        /// <summary>
        /// Updates duality of icons based on the assigned hitsound
        /// </summary>
        /// <param name="marker"></param>
        /// <param name="oldTrack"></param>
        private void UpdateDuality(HitsoundMarker marker, HitsoundTrack oldTrack)
        {
            if (ShouldBeDual(marker.startTime, marker.Data.targetData.behavior is TargetBehavior.Melee, (TimelineHitsound)marker.Track.Type, marker, out var newTrackMarker))
            {
                newTrackMarker.SetToDual(false);
                marker.SetToDual(true);
            }
            else if (oldTrack.TryGetContent(marker.startTime, marker, out var content))
            {
                var foundMarker = content as HitsoundMarker;
                foundMarker.SetToSingle();
                marker.SetToSingle();
            }
        }
        
        public void UpdateDuality(MoveData moveData)
        {
            var marker = moveData.content as HitsoundMarker;
            var oldTrack = trackManager.GetTrack((TimelineHitsound)moveData.oldTrack) as HitsoundTrack;
            UpdateDuality(marker, oldTrack);
        }

        private bool ShouldBeDual(HitsoundMarker marker, out HitsoundMarker foundMarker)
            => ShouldBeDual(marker.startTime, marker.Data.targetData.behavior is TargetBehavior.Melee, marker.Data.type, marker, out foundMarker);
        
        private bool ShouldBeDual(QNT_Timestamp time, bool isMelee, TimelineHitsound hitsound, HitsoundMarker excludeMarker, out HitsoundMarker marker)
        {
            var buffer = new QNT_Duration(1);
            var start = time - buffer;
            var end = time + buffer;
            NoteEnumerator notes = new(start, end);
            foreach (var target in notes)
            {
                if (target.data.time != time) continue;
                bool isTargetMelee = target.data.behavior is TargetBehavior.Melee;
                if (isTargetMelee == isMelee)
                {
                    if (trackManager.TryGetContent(time, isMelee, excludeMarker, out marker))
                    {
                        return hitsound == marker.Data.type;
                    }

                    return false;
                }
            }

            marker = null;
            return false;
        }

        public void TrySelectContentFromTarget(Target target)
        {
            if (markerMap.ContainsKey(target))
            {
                var marker = markerMap[target];
                if (!marker.Selected)
                {
                    SelectContent(markerMap[target], true);
                }
            }
            /*var targetTime = target.data.time;
            foreach (var content in Content)
            {
                if (content.startTime < targetTime) continue;
                
                var marker = content as HitsoundMarker;
                if (marker.Data.targetData == target.data)
                {
                    SelectContent(marker, true);
                    return;
                }
            }*/
        }
    }
}
