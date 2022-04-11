using TargetPreview.Targets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TargetPreview.Math;
using TargetPreview.Display;
using NotReaper.Timing;

namespace NotReaper.MapPreview
{
    public class PreviewSpawner : MonoBehaviour
    {
        [SerializeField] private LinePool linePool;
        [NRInject] private TargetPool targetPool;
        private Target previousLeftChainTarget;
        private Target previousRightChainTarget;
        private Target previousTarget;

        private Dictionary<Targets.Target, Target> spawnedTargets = new();
        private Dictionary<Targets.Target, LineConnector> chainConnectors = new();
        private Dictionary<Targets.Target, LineConnector> dualines = new();

        #region Spawning
        internal void SpawnTarget(Targets.Target target)
        {
            if (spawnedTargets.ContainsKey(target))
            {
                return;
            }

            var cue = target.ToCue();
            var position = TargetTransform.CalculateTargetTransform(cue.pitch, ((float)cue.gridOffset.x, (float)cue.gridOffset.y, cue.zOffset));
            TargetData data = new TargetData(ConvertBehavior(target.data.behavior), ConvertHandType(target.data.handType), (uint)target.data.time.tick, position);
            var spawned = targetPool.Take(data);
            if (data.behavior == TargetBehavior.ChainStart)
            {
                if (data.handType == TargetHandType.Left)
                    previousLeftChainTarget = spawned;
                else
                    previousRightChainTarget = spawned;

            }
            else if (data.behavior == TargetBehavior.Chain)
            {
                var chainStart = TargetFinder.FindChainStart(target);
                if (chainStart != null)
                {
                    var line = linePool.Spawn();
                    chainConnectors.Add(target, line);
                    if (data.handType == TargetHandType.Left)
                    {

                        line.ConnectChain(previousLeftChainTarget, spawned, chainStart.data.time);
                        previousLeftChainTarget = spawned;

                    }
                    else
                    {
                        line.ConnectChain(previousRightChainTarget, spawned, chainStart.data.time);
                        previousRightChainTarget = spawned;
                    }
                }
            }

            if (previousTarget != null)
            {
                if (!IsMeleeDodgeOrChainNode(previousTarget) && !IsMeleeDodgeOrChainNode(spawned))
                {
                    if (previousTarget.TargetData.time == spawned.TargetData.time)
                    {
                        if (previousTarget.TargetData.handType != spawned.TargetData.handType)
                        {
                            var line = linePool.Spawn();
                            dualines.Add(target, line);
                            line.ConnectDouble(previousTarget, spawned);
                        }
                    }
                }
            }

            previousTarget = spawned;
            spawnedTargets.Add(target, spawned);
        }
        #endregion

        #region Despawning
        internal void ReturnTarget(Targets.Target target)
        {
            if (!spawnedTargets.ContainsKey(target))
            {
                return;
            }
            if (chainConnectors.ContainsKey(target))
            {
                var connector = chainConnectors[target];
                connector.Reset();
                linePool.Return(chainConnectors[target]);
                chainConnectors.Remove(target);
            }
            if (dualines.ContainsKey(target))
            {
                var connector = dualines[target];
                connector.Reset();
                linePool.Return(dualines[target]);
                dualines.Remove(target);
            }
            targetPool.Return(spawnedTargets[target]);
            spawnedTargets.Remove(target);
        }
        internal void ClearSpawnedTargets()
        {
            foreach (var target in spawnedTargets)
            {
                targetPool.Return(target.Value);
            }
            spawnedTargets.Clear();
        }
        internal void ClearSpawnedChainConnectors()
        {
            foreach (var connector in chainConnectors)
            {
                linePool.Return(connector.Value);
            }
            chainConnectors.Clear();
        }
        internal void ClearSpawnedDualines()
        {
            foreach (var connector in dualines)
            {
                linePool.Return(connector.Value);
            }
            dualines.Clear();
        }
        #endregion

        #region Utility
        internal bool HasSpawnedTargets() => spawnedTargets.Count > 0;
        /// <summary>
        /// Get spawned targets dictionary
        /// </summary>
        /// <returns>All spawned targets</returns>
        internal Dictionary<Targets.Target, Target> GetSpawnedTargets() => spawnedTargets;
        /// <summary>
        /// Get spawned preview targets
        /// </summary>
        /// <returns></returns>
        internal List<Target> GetSpawnedPreviewTargets()
        {
            List<Target> targets = new();
            foreach(var t in spawnedTargets)
            {
                targets.Add(t.Value);
            }
            return targets;
        }
        /// <summary>
        /// Get spawned preview targets from time x to y
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        internal List<Target> GetSpawnedPreviewTargets(QNT_Timestamp from, QNT_Timestamp to)
        {
            List<Target> targets = new();
            foreach(var target in spawnedTargets)
            {
                if(target.Key.data.time >= from && target.Key.data.time <= to)
                {
                    targets.Add(target.Value);
                }
            }
            return targets;
        }
        /// <summary>
        /// Gets a preview target belonging to a NRTarget
        /// </summary>
        /// <param name="target">The NR Target</param>
        /// <returns></returns>
        internal Target GetPreviewTarget(Targets.Target target)
        {
            if (spawnedTargets.ContainsKey(target))
            {
                return spawnedTargets[target];
            }
            else
            {
                return null;
            }
        }
        private TargetBehavior ConvertBehavior(Models.TargetBehavior behavior) =>
    behavior switch
    {
        Models.TargetBehavior.Standard => TargetBehavior.Standard,
        Models.TargetBehavior.Sustain => TargetBehavior.Hold,
        Models.TargetBehavior.Vertical => TargetBehavior.Vertical,
        Models.TargetBehavior.Horizontal => TargetBehavior.Horizontal,
        Models.TargetBehavior.Melee => TargetBehavior.Melee,
        Models.TargetBehavior.Mine => TargetBehavior.Dodge,
        Models.TargetBehavior.Legacy_Pathbuilder => TargetBehavior.ChainStart,
        Models.TargetBehavior.ChainStart => TargetBehavior.ChainStart,
        Models.TargetBehavior.ChainNode => TargetBehavior.Chain,
        _ => TargetBehavior.Standard
    };

        private TargetHandType ConvertHandType(Models.TargetHandType hand) =>
            hand switch
            {
                Models.TargetHandType.Left => TargetHandType.Left,
                Models.TargetHandType.Right => TargetHandType.Right,
                Models.TargetHandType.Either => TargetHandType.Either,
                Models.TargetHandType.None => TargetHandType.None,
                _ => TargetHandType.Left
            };
        private bool IsMeleeDodgeOrChainNode(Target target)
        {
            return target.TargetData.behavior == TargetBehavior.Melee || target.TargetData.behavior == TargetBehavior.Dodge || target.TargetData.behavior == TargetBehavior.Chain;
        }
        #endregion
    }
}
