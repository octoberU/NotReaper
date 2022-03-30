using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.ObjectPooling
{
    /// <summary>
    /// Baseclass that handles object pooling.
    /// </summary>
    /// <typeparam name="T">The object you want to pool.</typeparam>
    public class ObjectPool<T> : MonoBehaviour where T : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private T prefab;
        [SerializeField] private Transform prefabParent;
        [Space, Header("Pool Settings")]
        [SerializeField] private int poolSize;
        [SerializeField] private int maxSize;
        [SerializeField] private PoolingMode mode;

        private List<T> pool = new List<T>();
        private List<T> activeObjects = new List<T>();

        private void Start()
        {
            InitializePool();
        }
        /// <summary>
        /// Initializes the pool with the given settings.
        /// </summary>
        private void InitializePool()
        {

            for(int i = 0; i < poolSize; i++)
            {
                InstantiateObject();
            }

            if (mode == PoolingMode.Cap && maxSize < poolSize) maxSize = poolSize;
        }
        /// <summary>
        /// Gets an object from the pool.
        /// </summary>
        /// <returns>A pooled object.</returns>
        public virtual T Spawn()
        {
            T obj;

            if(pool.Count == 0)
            {
                if (mode == PoolingMode.Grow || (mode == PoolingMode.Cap && pool.Count + activeObjects.Count <= maxSize))
                {
                    obj = InstantiateObject();
                }
                else
                {
                    return null;
                }
            }
            
            obj = pool[0];
            pool.RemoveAt(0);       
            obj.gameObject.SetActive(true);
            activeObjects.Add(obj);
            return obj;
        }
        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="pooledObject">The object to return to the pool.</param>
        public virtual void Return(T pooledObject)
        {
            if (activeObjects.Contains(pooledObject))
            {
                activeObjects.Remove(pooledObject);
                pool.Add(pooledObject);
                pooledObject.transform.position = Vector3.zero;
                pooledObject.gameObject.SetActive(false);
            }
        }
        /// <summary>
        /// Instantiates a new instance of the object.
        /// </summary>
        /// <returns>The new instance of T.</returns>
        private T InstantiateObject()
        {
            T obj = Instantiate(prefab, prefabParent);
            pool.Add(obj);
            obj.gameObject.SetActive(false);
            return obj;
        }

        /// <summary>
        /// Determines how the pooler behaves. Grow will automatically grow the pool once the pool is empty and more objects are needed, Cap will never spawn more than maxSize.
        /// </summary>
        private enum PoolingMode
        {
            Grow,
            Cap
        }
    }

}
