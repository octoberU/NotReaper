using System.Linq;
using UnityEngine;

public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
{
    static T _instance = null;
    
    
    [RuntimeInitializeOnLoadMethod]
    protected void Init()
    {
        _instance = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
    }
    
    public virtual void Initialize() {}
    

    public static T Instance
    {
        get
        {
            if (!_instance)
                _instance = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
            return _instance;
        }
    }
}