using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TargetPreview.Targets;

namespace NotReaper.MapPreview
{
    public class LineConnector : MonoBehaviour
    {
        [SerializeField] private LineRenderer connector;

        private QNT_Timestamp fadeStartTime;
        private QNT_Timestamp startTime;
        private QNT_Timestamp endTime;
        private QNT_Duration duration;
        private Color startColor;
        private Color endColor;

        private bool isChain;

        public void ConnectChain(Target from, Target to, QNT_Timestamp chainStartTime)
        {
            if (from == null || to == null) return;
            startColor = from.TargetData.handType == TargetHandType.Left ? NRSettings.config.leftColor : NRSettings.config.rightColor;
            endColor = startColor;
            startTime = chainStartTime - Relative_QNT.FromBeatTime(1f);
            isChain = true;
            Setup(from, to);
        }
        public void ConnectDouble(Target from, Target to)
        {
            startColor = from.TargetData.handType == TargetHandType.Left ? NRSettings.config.leftColor : NRSettings.config.rightColor;
            endColor = to.TargetData.handType == TargetHandType.Left ? NRSettings.config.leftColor : NRSettings.config.rightColor;
            startTime = new QNT_Timestamp((ulong)from.TargetData.time) - Relative_QNT.FromBeatTime(1f);
            isChain = false;
            Setup(from, to);
        }

        private void Setup(Target from, Target to)
        {
            if (from == null || to == null) return;
            connector.SetPosition(0, from.TargetData.transformData.position);
            connector.SetPosition(1, to.TargetData.transformData.position);
            fadeStartTime = startTime - Relative_QNT.FromBeatTime(1f);
            endTime = new QNT_Timestamp((ulong)to.TargetData.time);
            duration = new QNT_Duration(startTime.tick - fadeStartTime.tick);
            startColor.a = 0f;
            endColor.a = 0f;
            connector.startColor = startColor;
            connector.endColor = endColor;

            StopCoroutine(HandleConnector());
            StartCoroutine(HandleConnector());
        }

        public void Reset()
        {
            connector.SetPosition(0, Vector3.zero);
            connector.SetPosition(0, Vector3.zero);
            startColor.a = 0f;
            endColor.a = 0f;
            connector.startColor = startColor;
            connector.endColor = endColor;
        }

        private IEnumerator HandleConnector()
        {
            while (true)
            {
                if (EditorTime.Time >= fadeStartTime && EditorTime.Time <= startTime)
                {
                    float percentage = (float)(EditorTime.Time.tick - (float)fadeStartTime.tick) / duration.tick;
                    if (isChain)
                    {
                        percentage *= .5f;
                    }
                    startColor.a = percentage;
                    endColor.a = percentage;
                    connector.startColor = startColor;
                    connector.endColor = endColor;
                }
                else if(EditorTime.Time > startTime && EditorTime.Time < endTime && startColor.a != 1f)
                {
                    startColor.a = 1f;
                    endColor.a = 1f;
                    connector.startColor = startColor;
                    connector.endColor = endColor;
                }
                else if((EditorTime.Time >= endTime || EditorTime.Time < fadeStartTime) && startColor.a > 0f)
                {
                    startColor.a = 0f;
                    endColor.a = 0f;
                    connector.startColor = startColor;
                    connector.endColor = endColor;
                }
                
                yield return null;
            }
        }
    }
}

