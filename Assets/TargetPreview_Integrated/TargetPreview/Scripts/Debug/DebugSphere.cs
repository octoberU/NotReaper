using System;
using UnityEngine;
using System.Collections;
using TargetPreview.Targets;
using System.Collections.Generic;
using System.IO;
using TargetPreview.Display;
using System.Linq;
using AudicaTools;
using EasyButtons;
using TargetPreview.Math;
using TargetPreview.Scripts;
using TargetPreview.Scripts.Targets;
using TargetPreview.Scripts.Targets.Extensions;
using Random = UnityEngine.Random;

namespace Assets.TargetPreview.Scripts.Debug
{
    public class DebugSphere : MonoBehaviour
    {
        List<Target> sphereTargets = new List<Target>();

        [SerializeField] TargetManager targetManager;
        [SerializeField] CueManager cueManager;
        [SerializeField] string mockAudicaName = "GivenUp-Continuum.audica";
        [SerializeField] int pitchCount = 12;
        [SerializeField] Vector3 randomOffset;
        [SerializeField] Vector3 offset;
        [SerializeField] bool play = false;
        [SerializeField] float 
            debugTime = 0,
            timeOffsetPerTarget = 480,
            playbackSpeed = 1f;
       
        float lastDebugTime = 0;

#if UNITY_EDITOR
        void Start() =>
            LoadMockAudicaFile();
#endif
        void Update()
        {
            if (debugTime > 0 && debugTime != lastDebugTime)
            {
                lastDebugTime = debugTime;
                TimeController.SetTime(debugTime);
            }

            if (play)
            {
                debugTime += (Time.deltaTime * 1000f) * playbackSpeed;
            }
        }

        [ContextMenu("Create debug sphere")]
        [Button]
        void CreateDebugSphere()
        {
            TargetPool targetPool = FindObjectOfType<TargetPool>();

            if (sphereTargets.Any())
            {
                foreach (Target target in sphereTargets)
                {
                    targetPool.Return(target);
                }
            }

            sphereTargets.Clear();
            
            var cues = new List<TargetCue>();

            for (int i = 0; i < (pitchCount - 1); i++)
            {
                var targetBehavior = (TargetBehavior)Random.Range(0, 5);
                var targetHandType = (TargetHandType)Random.Range(1, 3);
                if (i >= TargetTransform.meleePitchBottomLeft)
                {
                    targetBehavior = TargetBehavior.Melee;
                    targetHandType = TargetHandType.Either;
                }

                TargetPosition targetPos = TargetTransform.CalculateTargetTransform(i,
                    (Random.Range(-randomOffset.x, randomOffset.x) + offset.x,
                        Random.Range(-randomOffset.y, randomOffset.y) + offset.y,
                        Random.Range(-randomOffset.x, randomOffset.z) + offset.z));

                var cue = new TargetCue()
                {
                    tick = (int)(i * timeOffsetPerTarget),
                    tickLength = 128,
                    pitch = i,
                    behavior = targetBehavior,
                    handType = targetHandType,
                };
                
                cues.Add(cue);
            }

            cueManager.TargetCues = cues.ToArray();
        }

#if UNITY_EDITOR
        [ContextMenu("Load mock audica file")]
        [Button]
        void LoadMockAudicaFile()
        {
            cueManager.TargetCues =
                GetTestAudica()
                    .expert
                    .cues
                    .AsTargetCues();
        }
        
        public Audica GetTestAudica()
        {
            string testAudicaPath = Path.Combine(Application.dataPath, "TargetPreview", "Editor", "Tests", "TestResources",
                mockAudicaName);
            string fullPath = Path.GetFullPath(testAudicaPath);

            return new Audica(fullPath);
        }
#endif
        
    }
}