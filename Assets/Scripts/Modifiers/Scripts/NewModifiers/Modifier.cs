using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifier;
using NotReaper.Timing;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Modifiers
{
    public class Modifier : MonoBehaviour
    {
        [SerializeField] private GameObject selectionOutline;
        [SerializeField] private RectTransform rect;
        [SerializeField] private BoxCollider2D boxCollider;
        [SerializeField] private List<Image> indicators;
        
        public Data Data { get; private set; }
        public QNT_Timestamp startTime { get; private set; } = new QNT_Timestamp(0);
        public QNT_Timestamp endTime { get; private set; } = new QNT_Timestamp(0);
        public QNT_Duration duration => new QNT_Duration(endTime.tick - startTime.tick);
        public Timeframe timeframe => GetTimeframe();
        public ModifierType Type => Data.type;
        public bool SupportsEndTime => ModifierUtility.SupportsEndTime(Type, Data.option1, Data.option2);
        public Track Track { get; private set; }

        public float[] LeftHandColor => Data.leftHandColor ?? new float[] { 1, 1, 1};
        public float[] RightHandColor => Data.rightHandColor ?? new float[] { 1, 1, 1 };

        public bool Selected => selectionOutline.activeInHierarchy;

        public delegate void OnTimeChanged(Modifier modifier, Timeframe oldTime, Timeframe newTime);

        public event OnTimeChanged onTimeChanged;

        private bool isShowing = true;

        private Bounds bounds => new (new(transform.position.x + rect.sizeDelta.x * .5f, transform.position.y), rect.sizeDelta);
        

        private void Awake() =>  selectionOutline.SetActive(false);

        public void Show(bool show)
        {
            if (show == isShowing) return;

            foreach (var indicator in indicators)
                indicator.enabled = show;

            boxCollider.enabled = show;
            isShowing = show;
        }

        public void SetSelected(bool selected) => selectionOutline.SetActive(selected);

        public void Initialize(Track track)
        {
            Data = new();
            Data.type = track.Type;
            Track = track;
        }

        public void LoadData(Data data)
        {
            Data = data;
            SetTime(new(data.startTick, data.endTick), false);
           /* SetStartTime(new ((ulong)data.startTick), false);
            
            if (data.startTick != data.endTick && data.endTick != 0)
            {
                SetEndTime(new((ulong)data.endTick), false);
            }*/
        }

        public void SetStartTime(QNT_Timestamp startTime, bool notify = true)
        {
            var previousTimeframe = GetTimeframe();
            this.startTime = startTime;
            /*var position = transform.localPosition;
            position.x = startTime.ToBeatTime();
            transform.localPosition = position;*/
            if (endTime.tick == 0)
                endTime = startTime;
            
            UpdateSize();
            UpdateTimeData();
            
            if (notify)
            {
                onTimeChanged?.Invoke(this, previousTimeframe, GetTimeframe());
            }
        }

        public void SetEndTime(QNT_Timestamp endTime, bool notify = true)
        {
            var previousTimeframe = GetTimeframe();
            if (endTime < startTime)
            {
                endTime = startTime;
            }
            
            this.endTime = endTime;
            
            /*if(endTime > startTime)
            {
                var size = rect.sizeDelta;
                size.x = endTime.ToBeatTime() - startTime.ToBeatTime();
                rect.sizeDelta = size;
            }*/
            
            UpdateSize();
            UpdateTimeData();
            
            if (notify)
            {
                onTimeChanged?.Invoke(this, previousTimeframe, GetTimeframe());
            }
        }

        public void SetTime(Timeframe timeframe, bool notify = true)
        {
            var previousTimeframe = GetTimeframe();
            var startTime = new QNT_Timestamp(timeframe.Start);
            var endTime = new QNT_Timestamp(timeframe.End);


            if (endTime < startTime)
                endTime = startTime;

            this.startTime = startTime;
            this.endTime = endTime;

            UpdateSize();
            
            UpdateTimeData();
            
            if (notify)
            {
                onTimeChanged?.Invoke(this, previousTimeframe, GetTimeframe());
            }
            
        }

        private void UpdateTimeData()
        {
            Data.startTick = (int)startTime.tick;
            Data.endTick = (int)endTime.tick;
        }

        public void ResetDuration()
        {
            if (SupportsEndTime) return;

            endTime = startTime;
            UpdateTimeData();

            var size = rect.sizeDelta;
            size.x = .1f;
            rect.sizeDelta = size;
            
            boxCollider.size = rect.sizeDelta;
            boxCollider.offset = new Vector2(boxCollider.size.x * .5f, 0f);
        }

        private void UpdateSize()
        {
            var position = transform.localPosition;
            position.x = startTime.ToBeatTime();
            transform.localPosition = position;

            if (endTime > startTime)
            {
                var size = rect.sizeDelta;
                size.x = endTime.ToBeatTime() - startTime.ToBeatTime();
                rect.sizeDelta = size;
            }

            boxCollider.size = rect.sizeDelta;
            boxCollider.offset = new Vector2(boxCollider.size.x * .5f, 0f);
        }

        private Timeframe GetTimeframe()
            => new (startTime, endTime);

        public bool IsInsideBounds(Bounds other) =>  bounds.Intersects(other);
    }
}
