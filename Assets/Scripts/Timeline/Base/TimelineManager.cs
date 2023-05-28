using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using NAudio.Wave;
using NotReaper.Audio;
using NotReaper.Audio.Noise;
using NotReaper.Modifiers;
using NotReaper.Notifications;
using NotReaper.Timing;
using NotReaper.Tools;
using NotReaper.UI;
using NotReaper.UI.Particles;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Profiling;

namespace NotReaper
{
    public abstract class TimelineManager<TData> : MonoBehaviour where TData : ContentData
    {
        [SerializeField] protected TrackManager tracks;
        [SerializeField] protected ContentPool contentPool;
        
        public bool IsActive { get; private set; }
        public Content CurrentContent { get; private set; }
        public List<Content> Content { get; private set; } = new();
        public List<Content> SelectedContent { get; private set; } = new();

        protected List<TData> copiedContent = new();

        protected bool isMovingContent = false;
        protected List<MoveData> moveData = new();
        
        protected abstract TimelineType TimelineType { get; }
        protected abstract GridTimeline.WidthType TimelineWidthType { get; }

        public bool IsLoadingContent { get; protected set; } = false;

        public delegate void ContentEvent(Content content);
        public static event ContentEvent onContentSelected;

        public delegate void GenericContentEvent();

        public static event GenericContentEvent onSelectedContentRemoved;
        public static event GenericContentEvent onMultiSelect;
        public static event GenericContentEvent onContentChanged;
        public static event GenericContentEvent onAfterTrackSwitch;

        protected GridTimeline timeline;
        private AudioPeer visualizer;

        protected virtual void Start()
        {
            timeline = NRDependencyInjector.Get<GridTimeline>();
            visualizer = NRDependencyInjector.Get<AudioPeer>();
            EditorState.OnEditorReset += OnReset;
        }
        
        protected virtual void OnReset()
        {
            SelectedContent.Clear();
            CurrentContent = null;
            
            for (int i = Content.Count - 1; i >= 0; i--)
            {
                tracks.RemoveContent(Content[i]);
                Destroy(Content[i].gameObject);
            }
            
            Content.Clear();
        }

        public virtual void ToggleTimeline()
        {
            IsActive = !IsActive;
            Show(IsActive);
        }

        protected virtual void Show(bool show)
        {
            if (show)
            {
                timeline.SetTimelineType(TimelineType, tracks.TrackCount, TimelineWidthType);
                visualizer.StopVisualization();
            }
            else
            {
                visualizer.StartVisualization();
            }
            
            timeline.ShowTimeline(show);
            tracks.Show(IsActive);

            CameraProvider.grid.enabled = !show;
            EditorTargets.ShowVisuals(!show);
            GridParticles.AllowEmission(!show);
            TransformTool.ShowTransformTool = !show;
        }
        
        public void MultiSelectContent(Content content)
        {
            if (!SelectedContent.Contains(content))
            {
                SelectedContent.Add(content);
                content.SetSelected(true);
            }
        }

        public void ReselectContentFromDrag(Content content, Timeframe oldTimeframe, Vector2 startMousePosition)
        {
            MultiSelectContent(content);
            AddReselectContentToMove(content, oldTimeframe, startMousePosition);
        }

        public void DeselectMultiselectContent(Content content)
        {
            if (SelectedContent.Contains(content))
            {
                SelectedContent.Remove(content);
                content.SetSelected(false);
            }
        }

        private Content SelectContent(QNT_Timestamp time, TrackContent trackContent, bool multiSelect)
        {
            if (tracks.TryGetContent(trackContent.tracks[GridTimeline.Type].ID, time, out var content))
            {
                SelectContent(content, multiSelect);
                return content;
            }

            return null;
        }

        /// <summary>
        /// Returns true if the content has been selected and false if it has been deselected.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="multiSelect"></param>
        /// <returns></returns>
        public bool SelectContent(Content content, bool multiSelect)
        {
            bool hasSelected = true;
            if (multiSelect) //&& CurrentContent != null)
            {
                if (content.Selected)
                {
                    content.SetSelected(false);
                    onContentSelected?.Invoke(content);
                    if (SelectedContent.Contains(content))
                        SelectedContent.Remove(content);

                    hasSelected = false;
                }
                else if(!SelectedContent.Contains(content))
                {
                    if (CurrentContent == null)
                    {
                        UpdateCurrentContent(content, true);
                    }
                    else
                    {
                        content.SetSelected(true);
                        SelectedContent.Add(content);
                        onMultiSelect?.Invoke();
                        onContentSelected?.Invoke(content);
                    }
                }
            }
            else
            {
                for (int i = SelectedContent.Count - 1; i >= 0; i--)
                {
                    SelectedContent[i].SetSelected(false);
                }
                    
                SelectedContent.Clear();
                    
                UpdateCurrentContent(content, true);
            }

            return hasSelected;
        }

        public void SelectCurrentContent()
        {
            if (CurrentContent != null)
            {
                for (int i = SelectedContent.Count - 1; i >= 0; i--)
                {
                    SelectedContent[i].SetSelected(false);
                }

                SelectedContent.Clear();
                
                CurrentContent.SetSelected(true);
                
                if(!SelectedContent.Contains(CurrentContent))
                    SelectedContent.Add(CurrentContent);
                
                onContentSelected?.Invoke(CurrentContent);
            }
        }

        public bool TrySelectContent(QNT_Timestamp time, TrackContent trackContent, bool multiSelect, out Content selectedContent)
        {
            selectedContent = null;
            
            if (tracks.ContainsContentAtTime(trackContent.tracks[GridTimeline.Type].ID, time))
            {
                selectedContent = SelectContent(time, trackContent, multiSelect);
                return true;
            }

            return false;
        }
        public bool TryPlaceContent(QNT_Timestamp startTime, TrackContent content)
        {
            if (tracks.ContainsContentAtTime(content.tracks[GridTimeline.Type].ID, startTime))
            {
                SelectContent(startTime, content, false);
                return false;
            }

            if (!CheckSpecialPlaceRequirements(startTime, content)) 
                return false;
            
            AddContentAction(startTime, content);
            return true;
        }

        protected abstract void AddContentAction(QNT_Timestamp startTime, TrackContent trackContent);

        private void OnContentChanged()
        {
            if (!IsActive) return;
            onContentChanged?.Invoke();
        }
        
        public Content PlaceContentFromAction(QNT_Timestamp startTime, Track track)
        {
            var content = contentPool.Spawn();
            content.Initialize(track, startTime);
            timeline.PlaceContent(content);
            UpdateCurrentContent(content, true);
            tracks.AddContent(CurrentContent);
            SortTrackContent(track);
            Content.Add(CurrentContent);
            SelectCurrentContent();
            OnContentChanged();
            return content;
        }

        protected void SortTrackContent(Track track)
        {
            if (!IsLoadingContent)
            {
                track.SortContent();
            }
        }

        private void SortContent()
        {
            Content.Sort((c1, c2) => c1.startTime.tick.CompareTo(c2.startTime.tick));
            tracks.SortAllTrackContent();
        }

        public Content PlaceContentFromAction(Timeframe timeframe, Track track, bool notify = true)
        {
            var content = contentPool.Spawn();
            content.Initialize(track, timeframe.StartTime);
            timeline.PlaceContent(content);
            content.SetTime(timeframe);
            UpdateCurrentContent(content, true);
            tracks.AddContent(CurrentContent);
            SortTrackContent(track);
            Content.Add(CurrentContent);
            SelectCurrentContent();
            if (notify)
            {
                OnContentChanged();
            }
            return content;
        }

        private void UpdateCurrentContent(Content newCurrent, bool select)
        {
            if (CurrentContent != null)
            {
                CurrentContent.SetSelected(false);
                if (SelectedContent.Contains(CurrentContent))
                    SelectedContent.Remove(CurrentContent);
            }
            CurrentContent = newCurrent;
            
            if (select)
            {
                SelectCurrentContent();
            }
        }

        public void SetIsLoading(bool isLoading) => IsLoadingContent = isLoading;

        public Content LoadContent(int type, int typeIndex)
        {
            var content = contentPool.Spawn();
            content.Initialize(tracks.GetTrack(type, typeIndex));
            timeline.PlaceContent(content);
            tracks.AddContent(content);
            Content.Add(content);
            return content;
        }

        public void SetStartTime(bool increase, bool move)
        {
            if (SelectedContent.Count == 0) return;
            
            Relative_QNT beatSnap = new((long)EditorBeatSnap.Duration.tick * (increase ? 1 : -1));
            Dictionary<Content, Timeframe> newTimes = new();
            foreach (var content in SelectedContent)
            {
                if (CheckAlwaysMoveRequirements(content)) 
                    move = true;
                
                var currentDuration = content.duration;
                if ((long)currentDuration.tick - beatSnap.tick <= 0 && !move)
                    return; //don't allow setting time if we get 0 or less duration
                
                
                var newStartTime = content.startTime + beatSnap;
                var end = (move ? newStartTime : content.startTime) + currentDuration;
                
                if (!move && content.duration.tick < (ulong)beatSnap.tick)
                {
                    var remainder = newStartTime.tick % (ulong)beatSnap.tick;
                    if(remainder != 0 && newStartTime.tick + remainder < end.tick)
                        newStartTime = new QNT_Timestamp(newStartTime.tick + remainder);
                }
                
                Timeframe newTimeframe = new(newStartTime, end);
                
                var bufferTimeframe = new Timeframe((int)newTimeframe.Start + 1, (int)newTimeframe.End);
                if(!move && tracks.ContainsContentAtTime(content, bufferTimeframe))
                    return;
                
                


                if (newTimeframe == content.timeframe)
                {
                    continue;
                }

                if (move && newTimeframe.Duration != currentDuration)
                {
                    return;
                }

                newTimes.Add(content, newTimeframe);
            }

            foreach (var kvp in newTimes)
            {
                kvp.Key.SetTime(kvp.Value);
            }
        }

        
        
        public virtual void TrySwitchTrack(Vector2 mousePosition)
        {
            if (moveData.Count == 0) return;

            foreach (var move in moveData)
            {
                var pos = mousePosition;
                pos.y -= move.distanceToMouse;
                timeline.TrySwitchTrack(TimelineType, move.content, pos);
            }
            SortContent();
            onAfterTrackSwitch?.Invoke();
        }

        /// <summary>
        /// Add any logic here that determines whether a content marker should always be moved, even when trying to set start time through dragging.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        protected abstract bool CheckAlwaysMoveRequirements(Content content);
        

        public virtual void StartMove(Vector3 mousePosition)
        {
            if (isMovingContent) return;
            foreach (var content in SelectedContent)
            {
                moveData.Add(new MoveData
                {
                    content = content,
                    oldTimeframe = content.timeframe,
                    oldTrack = content.Track.ID,
                    distanceToMouse = mousePosition.y - content.transform.position.y,
                    newTimeframe =  content.timeframe,
                    newTrack = content.Track.ID
                });
            }
            isMovingContent = true;
        }

        protected virtual void AddReselectContentToMove(Content content, Timeframe oldTimeframe, Vector3 mousePosition)
        {
            if (!isMovingContent) return;
            moveData.Add(new MoveData
            {
                content = content,
                oldTimeframe = oldTimeframe,
                oldTrack = content.Track.ID,
                distanceToMouse = mousePosition.y - content.transform.position.y
            });
        }

        public void SetEndTime(bool increase, bool move)
        {
            if (SelectedContent.Count == 0) return;

            Relative_QNT beatSnap = new((long)EditorBeatSnap.Duration.tick * (increase ? 1 : -1));
            Dictionary<Content, Timeframe> newTimes = new();
            var songEnd = EditorAudio.SongEndTime;
            foreach (var content in SelectedContent)
            {
                if (CheckAlwaysMoveRequirements(content)) move = true;
                var currentDuration = content.duration;
                if ((long)currentDuration.tick + beatSnap.tick <= 0 && !move) return; //don't allow setting time if we get 0 or less duration
                var newEndTime = content.endTime + beatSnap;

                if (!move && content.duration.tick < (ulong)beatSnap.tick)
                {
                    var remainder = newEndTime.tick % (ulong)beatSnap.tick;
                    if(remainder != 0 && newEndTime.tick - remainder > 0)
                        newEndTime = new QNT_Timestamp(newEndTime.tick - remainder);
                }

                if (newEndTime > songEnd)
                    newEndTime = songEnd;
                
                var start = (move ? newEndTime : content.endTime) - currentDuration;
                Timeframe newTimeframe = new(start, newEndTime);
                if (newTimeframe == content.timeframe) continue;
                if (move && newTimeframe.Duration != currentDuration) return;

                var bufferTimeframe = new Timeframe((int)newTimeframe.Start, (int)newTimeframe.End - 1);
                if(!move && tracks.ContainsContentAtTime(content, bufferTimeframe))
                    return;

                newTimes.Add(content, newTimeframe);
            }

            foreach (var kvp in newTimes)
                kvp.Key.SetTime(kvp.Value);
        }

        public virtual void EndMove()
        {
            if (!isMovingContent)
            {
                return;
            }
            isMovingContent = false;

            bool hasMoved = false;
            foreach (var data in moveData)
            {
                data.newTimeframe = data.content.timeframe;
                data.newTrack = data.content.Track.ID;
                if (!hasMoved)
                {
                    if (data.oldTimeframe != data.content.timeframe || data.newTrack != data.oldTrack)
                        hasMoved = true;
                }
            }

            if (hasMoved)
            {
                moveData.Sort((c1, c2) => c1.oldTimeframe.StartTime.CompareTo(c2.oldTimeframe.StartTime));
                MoveContentAction(moveData);
                SortContent();
                OnContentChanged();
            }
            moveData.Clear();
        }

        public void MoveContentFromAction(Content content, Timeframe timeframe, TrackManager.TrackID track)
        {
            content.SetTime(timeframe);
            timeline.SwitchTrack(TimelineType, content, track);
        }

        public bool TryGetContent(Timeframe timeframe, int track, int trackIndex, out Content content)
            =>TryGetContent(timeframe, new(track, trackIndex), out content);

        public bool TryGetContent(Timeframe timeframe, TrackManager.TrackID trackID, out Content content)
            => tracks.TryGetContent(trackID, timeframe, out content);


        public void TryRemoveContent(QNT_Timestamp time, TrackContent trackContent)
        {
            if(tracks.TryGetContent(trackContent.tracks[GridTimeline.Type].ID, time, out var content))
            {
                TryRemoveContent(content);
            }
        }

        public void TryRemoveContent(Content content) => RemoveContentAction(content);

        protected abstract void RemoveContentAction(Content content);
        
        public void RemoveContentFromAction(Content content, bool notify = true)
        {
            tracks.RemoveContent(content);
            Content.Remove(content);
            if(CurrentContent == content)
                UpdateCurrentContent(null, false);

            if (SelectedContent.Contains(content))
                SelectedContent.Remove(content);

            if(SelectedContent.Count == 0)
                onSelectedContentRemoved?.Invoke();
            
            if (notify)
            {
                OnContentChanged();
            }
            
            contentPool.Return(content);
        }

        public void RemoveSelectedContent()
        {
            if (CurrentContent == null) return;
            if (CurrentContent.Selected)
            {
                TryRemoveContent(CurrentContent);
            }
        }
        
        public void CopySelectedContent()
        {
            if (SelectedContent.Count == 0) return;
            copiedContent.Clear();

            foreach (var content in SelectedContent)
            {
                copiedContent.Add(content.GetData().Clone() as TData);
            }
            copiedContent.Sort((m1, m2) => m1.startTick.CompareTo(m2.startTick));
        }

        public void CutSelectedContent()
        {
            if (SelectedContent.Count == 0) return;
            CopySelectedContent();
            
            MultiRemoveContentAction(SelectedContent);
            OnContentChanged();
            SelectedContent.Clear();
        }

        protected abstract void MultiRemoveContentAction(List<Content> content);

        public void PasteContent(QNT_Timestamp currentTime)
        {
            if (copiedContent.Count == 0) return;
            
            var delta = (int)currentTime.tick - copiedContent[0].startTick;
            foreach (var content in copiedContent)
            {
                content.startTick += delta;
                content.endTick += delta;
            }
            
            MultiAddContentAction(copiedContent);
        }

        protected abstract void MultiAddContentAction(List<TData> content);

        protected abstract void MoveContentAction(List<MoveData> moveData);

        public void SelectAll()
        {
            SelectedContent.Clear();

            for (int i = Content.Count - 1; i >= 0; i--)
            {
                Content[i].SetSelected(true);
                SelectedContent.Add(Content[i]);
            }

            onMultiSelect?.Invoke();
        }

        public void DeselectAll()
        {
            for (int i = SelectedContent.Count - 1; i >= 0; i--)
            {
                SelectedContent[i].SetSelected(false);
            }

            SelectedContent.Clear();
            onMultiSelect?.Invoke();
        }


        /// <summary>
        /// If you want to check for specific requirements to allow content to be placed, add that logic here.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        protected abstract bool CheckSpecialPlaceRequirements(QNT_Timestamp startTime, TrackContent content);

        public void MoveSelectedContentUp( Vector3 mousePosition) => MoveContent(true, mousePosition);
        public void MoveSelectedContentDown( Vector3 mousePosition) => MoveContent(false, mousePosition);

        private void MoveContent(bool up, Vector3 mousePosition)
        {
            if (!isMovingContent)
            {
                StartMove(mousePosition);
            }

            foreach (var move in moveData)
            {
                var desiredTrack = up ? tracks.GetTrackAbove(move.oldTrack) : tracks.GetTrackBelow(move.oldTrack);
                if (!desiredTrack.HasValue)
                    continue;

                var nextTrack = desiredTrack.Value;
                if (!CanSwitchTrack(move.content, move.oldTrack, nextTrack)) continue;
                timeline.SwitchTrack(TimelineType, move.content, nextTrack);
            }
            EndMove();
        }

        protected abstract bool CanSwitchTrack(Content content, TrackManager.TrackID currentTrack, TrackManager.TrackID nextTrack);

    }
}
