using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifiers;
using NotReaper.Timing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper
{
    public abstract class Content : MonoBehaviour
    {
        [SerializeField] private GameObject selectionOutline;

        [SerializeField] private bool useCollisionRadius = false;
        [SerializeField, ShowIf("@useCollisionRadius")] private float collisionRadius = .75f;
        //[SerializeField] private RectTransform rect;
        //[SerializeField] protected BoxCollider2D boxCollider;
        [SerializeField] protected List<SpriteRenderer> indicators;
        
        public QNT_Timestamp startTime { get; protected set; } = new (0);
        public QNT_Timestamp endTime { get; protected set; } = new (0);
        public QNT_Duration duration => new (endTime.tick - startTime.tick);
        public Timeframe timeframe => new (startTime, endTime);
        public Track Track { get; private set; }
        public abstract int Type { get; }
        
        public abstract TimelineType TimelineType { get; }
        
        public abstract ContentData GetData();
        public bool Selected => selectionOutline.activeInHierarchy;

        private bool isShowing = true;

        //private Bounds bounds => new (new(transform.position.x + rect.sizeDelta.x * .5f, transform.position.y), rect.sizeDelta);
        

        private void Awake() =>  selectionOutline.SetActive(false);
        

        public void Show(bool show)
        {
            if (show == isShowing) return;

            foreach (var indicator in indicators)
                indicator.enabled = show;

            
            //boxCollider.enabled = show;
            
            
            isShowing = show;
        }

        public virtual void SetSelected(bool selected) => selectionOutline.SetActive(selected);

        public virtual void Initialize(Track track, QNT_Timestamp? start = null)
        {
            Track = track;
            
            if(start.HasValue)
                SetStartTime(start.Value);
        }
        
        public virtual void SetStartTime(QNT_Timestamp start)
        {
            this.startTime = start;
            if (endTime.tick == 0)
                endTime = start;
            
            UpdatePosition();
            UpdateSize();
            UpdateTimeData();
        }

        public virtual void SetEndTime(QNT_Timestamp endTime)
        {
            if (endTime < startTime)
            {
                endTime = startTime;
            }
            
            this.endTime = endTime;

            UpdatePosition();
            UpdateSize();
            UpdateTimeData();
        }

        public virtual void SetTime(Timeframe timeframe)
        {
            var startTime = new QNT_Timestamp(timeframe.Start);
            var endTime = new QNT_Timestamp(timeframe.End);


            if (endTime < startTime)
                endTime = startTime;

            this.startTime = startTime;
            this.endTime = endTime;

            UpdatePosition();
            UpdateSize();
            UpdateTimeData();
        }

        protected abstract void UpdateTimeData();

        public virtual void ResetDuration()
        {
            endTime = startTime;
            UpdateTimeData();
            ResetSize();
        }

        protected virtual void UpdatePosition()
        {
            var position = transform.localPosition;
            position.x = startTime.ToBeatTime();
            transform.localPosition = position;
        }
        protected abstract void UpdateSize();
        protected abstract void ResetSize();

        public virtual bool SwitchTrack(Track newTrack)
        {
            Track.Remove(this);
            Track = newTrack;
            Track.Add(this);
            return true;
        }

        public virtual void ResetState()
        {
            selectionOutline.SetActive(false);
            Track = null;
            ResetData();
        }

        protected abstract void ResetData();

        public abstract void OnScaleChanged(float scaleAmount);

        public virtual bool IsNearTime(QNT_Timestamp time)
        {
            QNT_Duration loadedDuration = Constants.EighthNoteDuration; // + Constants.QuarterNoteDuration
            return (time >= startTime && time <= endTime) || Math.Abs((time - startTime).tick) <= (long)loadedDuration.tick || Mathf.Abs((time - endTime).tick) <= (long)loadedDuration.tick;
        }

        public bool IsNearPoint(Vector2 point)
        {
            if (!useCollisionRadius)
                return true;
            
            Vector2 center = transform.position;//transform.TransformPoint(0, 0, 0);
            float collisionRad = transform.TransformVector(collisionRadius, 0, 0).x;
            return (point - center).sqrMagnitude < collisionRad * collisionRad;
        }
    }
}
