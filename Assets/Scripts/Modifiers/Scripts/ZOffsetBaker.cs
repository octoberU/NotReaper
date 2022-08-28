using System;
using NotReaper.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ZOffsetBaker : MonoBehaviour
    {
        public static ZOffsetBaker Instance { get; private set; }


        [NRInject] private ModifierManager manager;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Tried to create second ZOffsetBaker instance!");
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public List<Cue> Bake(List<Cue> cues)
        {
            Dictionary<Cue, float> oldOffsetDict = new Dictionary<Cue, float>();
            foreach (Cue c in cues) oldOffsetDict.Add(c, c.zOffset);
            List<Content> zOffsetList = manager.GetZOffsetModifiers();
            zOffsetList.Sort((mod1, mod2) => mod1.startTime.CompareTo(mod2.startTime));
            foreach(Modifier m in zOffsetList)
            {
                float currentCount = 1f;
                m.Data.option1 = true;
                bool endTickSet = m.endTime.tick != 0 && m.startTime.tick != m.endTime.tick;
                foreach (Cue cue in cues)
                {
                    if (cue.tick < (int)m.startTime.tick) continue;
                    if (cue.tick > (int)m.endTime.tick && endTickSet) break;
                    if (cue.behavior != TargetBehavior.Melee && cue.behavior != TargetBehavior.Mine)
                    {
                        float.TryParse(m.Data.value1, out float transitionNumberOfTargets);
                        if (transitionNumberOfTargets > 0)
                        {
                            cue.zOffset = Mathf.Lerp(cue.zOffset * 100f, m.Data.amount, currentCount / (float)transitionNumberOfTargets);
                        }
                        else
                        {
                            cue.zOffset = m.Data.amount;
                        }
                        cue.zOffset /= 100f;
                        cue.zOffset += oldOffsetDict[cue];
                        if (currentCount < transitionNumberOfTargets) currentCount++;
                    }
                }
            }
            return cues;
        }

        private void Unbake()
        {
            List<Content> zOffsetList = manager.GetZOffsetModifiers();
            foreach (Modifier m in zOffsetList) m.Data.option1 = false;
        }

        public void OnBakeButtonPressed()
        {
            EditorFile.AudicaFile.desc.bakedzOffset = true;
            EditorIO.SaveMap();
        }

        public void OnUnbakeButtonPressed()
        {
            EditorFile.AudicaFile.desc.bakedzOffset = false;
            Unbake();
            EditorIO.SaveMap();
        }
    }

}
