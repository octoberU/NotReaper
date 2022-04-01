using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _quitting = false;
    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null && !_quitting)
                {
                    _instance = GameObject.FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        GameObject go = new(typeof(T).ToString());
                        _instance = go.AddComponent<T>();

                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null) _instance = GetComponent<T>();
        else if (_instance.GetInstanceID() != GetInstanceID())
        {
            Destroy(gameObject);
            throw new System.Exception($"Instance of {GetType().FullName} already exists.");
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _quitting = true;
    }
}
}


