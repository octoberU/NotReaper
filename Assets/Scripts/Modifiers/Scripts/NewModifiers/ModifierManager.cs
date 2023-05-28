using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.HtmlControls;
using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Timing;
using NotReaper.UI.Particles;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ModifierManager : TimelineManager<Data>
    {
        [NRInject] private ModifierInputManager inputManager;
        [NRInject] private ModifierTrackManager trackManager;
        protected override TimelineType TimelineType => TimelineType.Modifier;
        protected override GridTimeline.WidthType TimelineWidthType => GridTimeline.WidthType.Reduced;

        [NRListener]
        private void OnIsInUIChanged(bool isInUi)
        {
            if(isInUi && IsActive)
                ToggleTimeline();
        }

        [NRListener]
        private void OnToolChanged(EditorTool tool)
        {
            if(IsActive && tool is not EditorTool.ModifierCreator && tool is not EditorTool.None)
                ToggleTimeline();
        }

        public void UpdateVisibleTracks() => trackManager.UpdateVisibleTracks();

        public override void ToggleTimeline()
        {
            base.ToggleTimeline();
            EditorState.SelectTool(EditorTool.ModifierCreator);
        }
        
        protected override void Show(bool show)
        {
            if(show) inputManager.Activate();
            else inputManager.Deactivate();
            
            base.Show(show);
        }

        protected override bool CheckSpecialPlaceRequirements(QNT_Timestamp startTime, TrackContent content)
        {
            var track = content.tracks[GridTimeline.Type];
            var type = (ModifierType)track.Type;
            if (type.IsUpdateModifier(out var baseModifier))
            {
                if (!tracks.ContainsContentAtTimeInType(baseModifier, startTime))
                {
                    NotificationCenter.SendNotification($"{type.ToDisplayName()} modifiers can only be placed during active {baseModifier.ToDisplayName()} modifiers.");
                    return false;
                }
            }
            
            if (tracks.ContainsContentAtTimeInType(content.tracks[GridTimeline.Type].Type, new Timeframe(startTime, startTime + Constants.SixteenthNoteDuration)))
            {
                NotificationCenter.SendNotification("Not enough space to place modifier.", NotificationType.Warning);
                return false;
            }

            return true;
        }

        protected override bool CanSwitchTrack(Content content, TrackManager.TrackID currentTrack, TrackManager.TrackID nextTrack) => false;

        public Modifier LoadModifier(Data data)
        {
            var modifier = LoadContent((int)data.type, data.typeIndex) as Modifier;
            if (modifier == null)
                return null;

            modifier.LoadData(data);
            modifier.SetupSprites();
            SortTrackContent(modifier.Track);
            return modifier;
        }

        public void LoadTrackArrangement(List<TrackManager.TrackOrder> trackArrangement)
        {
            trackManager.LoadTrackArrangement(trackArrangement);
        }

        protected override void AddContentAction(QNT_Timestamp startTime, TrackContent trackContent)
            => ModifierUndoRedo.AddAction(new AddModifierAction(startTime, trackContent.tracks[GridTimeline.Type]));
        
        protected override void MultiAddContentAction(List<Data> content)
            => ModifierUndoRedo.AddAction(new MultiAddModifierAction(content));

        protected override void MoveContentAction(List<MoveData> moveData)
            => ModifierUndoRedo.AddAction(new TimelineMoveAction<Data>(moveData));

        protected override void RemoveContentAction(Content content)
            => ModifierUndoRedo.AddAction(new RemoveModifierAction(content));
        
        protected override void MultiRemoveContentAction(List<Content> content)
            => ModifierUndoRedo.AddAction(new MultiRemoveModifierAction(content));

        protected override bool CheckAlwaysMoveRequirements(Content content)
        {
            var modifier = content as Modifier;
            return !modifier.SupportsEndTime;
        }

        

        public List<Content> GetZOffsetModifiers()
        {
            List<Content> zOffsets = new();
            foreach (var modifier in Content)
            {
                if ((ModifierType)modifier.Type != ModifierType.zOffset) continue;
                zOffsets.Add(modifier);
            }

            return zOffsets;
        }
    }
}
