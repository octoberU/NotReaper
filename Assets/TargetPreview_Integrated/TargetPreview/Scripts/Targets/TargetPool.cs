using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TargetPreview.Targets;
using TargetPreview.ScriptableObjects;
using UnityEngine;

namespace TargetPreview.Display
{
    public class TargetPool : MonoBehaviour
    {
        [SerializeField] AssetContainer assetContainer;
        [SerializeField] VisualConfig visualConfig;
        [SerializeField] TargetDictionary targetPrefabs;

        [Serializable]
        public class TargetDictionary : SerializableDictionary<TargetBehavior, Target> {}

        Dictionary<TargetBehavior, Stack<Target>> targets = 
            Enum.GetValues(typeof(TargetBehavior))
            .Cast<TargetBehavior>()
            .ToDictionary(x => x, x => new Stack<Target>());

        [SerializeField] TargetManager targetManager;

        const int poolSize = 100;

        void Awake()
        {
            assetContainer.FillTextureArrays();
            visualConfig.UpdateStaticValues();
            FillPool(poolSize);
        }

        public void FillPool(int size)
        {
            foreach (var behavior in Enum.GetValues(typeof(TargetBehavior)).Cast<TargetBehavior>())
                for (int i = 0; i < size; i++)
                    CreateTarget(behavior);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="behaviour"></param>
        void CreateTarget(TargetBehavior behaviour)
        {
            Target allocatedTarget = Instantiate(targetPrefabs[behaviour], transform);
            allocatedTarget.TargetData = new TargetData(placeHolder: true);
            allocatedTarget.gameObject.SetActive(false);
            allocatedTarget.creator = this;
            targets[behaviour].Push(allocatedTarget);
        }
        
        /// <summary>Takes a target from the <see cref="TargetPool"/>. </summary>
        /// <returns>A reference to the target</returns>
        public Target Take(TargetData data)
        {
            var targetBehavior = data.behavior;
            if(targets[targetBehavior].Count == 0)
            {
                CreateTarget(targetBehavior);
            }

            Target storedTarget = targets[targetBehavior].Pop();
            
            storedTarget.TargetData = data;
            storedTarget.transform.SetParent(null);
            storedTarget.gameObject.SetActive(true);
            targetManager.AppendTarget(storedTarget);

            return storedTarget;
        }

        public void Return(Target target)
        {
            target.gameObject.SetActive(false);
            target.transform.SetParent(transform);
            targetManager.RemoveTarget(target);
            target.OnReturnedToPool();
            targets[target.TargetData.behavior].Push(target);
        }
    }

}