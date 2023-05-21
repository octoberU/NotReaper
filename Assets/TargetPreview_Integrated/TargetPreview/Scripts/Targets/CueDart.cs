using System;
using TargetPreview.ScriptableObjects;
using TargetPreview.Targets;
using UnityEngine;

namespace TargetPreview.Scripts.Targets
{
    public class CueDart : MonoBehaviour, IReceiveTimeUpdates
    {
        [SerializeField] CueManager cueManager;
        [SerializeField] LineRenderer lineRenderer;
        [SerializeField] TargetHandType handType;

        [SerializeField] float
            cueDartSpeed = 1f,
            timeToReachTarget = 2f,
            cueDartTailLag = 1f,
            fadeThreshold = 0.9f;
        
        Vector3 
            cueDartHead, 
            cueDartTail,
            gridCenter = new Vector3(0, 1.5f, 20f);

        Color startColor;
        
        Vector3[] lineRendererPositions = new Vector3[2];
        
        float headCompletion, tailCompletion;

        void Awake() =>
            TimeController.AddListener(this);

        void OnDestroy() =>
            TimeController.RemoveListener(this);

        void OnEnable() =>
            startColor = VisualConfig.GetColorForHandType(handType);

        public void OnTimeUpdated(float time)
        {
            TargetReference? foundTarget = null;
            TargetReference? previousTarget = null;
            foreach (var cueManagerActiveCue in cueManager.ActiveCues)
            {
                if (cueManagerActiveCue.cue.behavior is TargetBehavior.Melee or TargetBehavior.Dodge)
                    continue;
                
                if (cueManagerActiveCue.cue.handType == handType)
                {
                    if(cueManagerActiveCue.cue.timeMs >= time)
                    {
                        foundTarget = cueManagerActiveCue;
                    }
                    else
                    {
                        previousTarget = cueManagerActiveCue;
                    }
                    
                    if(foundTarget.HasValue && previousTarget.HasValue)
                    {
                        break;
                    }
                }

            }

            UpdateCueDartPosition(time, foundTarget, previousTarget);
            UpdateLineRenderer();
        }

        void UpdateCueDartPosition(float time, TargetReference? foundTarget, TargetReference? previousTarget)
        {
            Vector3 
                startPos = previousTarget?.target.transform.position ?? gridCenter,
                endPos = foundTarget?.target.transform.position ?? startPos;

            float cueTime = foundTarget?.cue.timeMs ?? 0f;
            float start = previousTarget?.cue.timeMs ?? (cueTime - timeToReachTarget);
            float end = cueTime;
            float t() => (time - start) / (end - start);
            cueDartHead = Vector3.Lerp(startPos, endPos, headCompletion = t());

            start -= cueDartTailLag;
            cueDartTail = Vector3.Lerp(startPos, endPos, tailCompletion = t() * t());
        }

        void UpdateLineRenderer()
        {
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            lineRendererPositions[0] = cueDartHead;
            lineRendererPositions[1] = cueDartTail;
            lineRenderer.SetPositions(lineRendererPositions);
            lineRenderer.endColor = Color.Lerp(startColor, endColor,  1 - headCompletion);
            lineRenderer.startColor = Color.Lerp(startColor, endColor,  1 - headCompletion) * new Color(1,1,1, 0f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = startColor;
            Gizmos.DrawSphere(cueDartHead, 1f);
            Gizmos.DrawWireSphere(cueDartTail, 1f);
        }

        public void UpdateColor()
            => OnEnable();
    }
}