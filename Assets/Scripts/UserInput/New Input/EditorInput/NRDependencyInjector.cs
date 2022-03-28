using NotReaper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NotReaper
{
    public class NRDependencyInjector : MonoBehaviour
    {
        private static Dictionary<Type, Dependency> dependencies = new Dictionary<Type, Dependency>();

        private List<InjectionContainer> pendingInjections = new List<InjectionContainer>();

        private int sceneCount;
        private int loadedSceneCount;
        private void Awake()
        {
            sceneCount = SceneManager.sceneCountInBuildSettings;
            SceneManager.sceneLoaded += OnSceneLoaded;

        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            loadedSceneCount += 1;
            if (loadedSceneCount == sceneCount)
            {
                DoInjection();
            }
        }

        private void DoInjection()
        {
            List<MonoBehaviour> behaviors = new();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var objects = SceneManager.GetSceneAt(i).GetRootGameObjects();

                foreach (var obj in objects)
                {
                    var mono = obj.GetComponent<MonoBehaviour>();
                    if (mono != null) behaviors.Add(mono);
                    var childMonos = obj.GetComponentsInChildren<MonoBehaviour>(true).ToList();
                    for (int j = childMonos.Count - 1; j >= 0; j--)
                    {
                        if (childMonos[j] == null)
                        {
                            childMonos.RemoveAt(j);
                        }
                    }
                    if (childMonos != null && childMonos.Count > 0)
                    {
                        behaviors.AddRange(childMonos);
                    }
                }


            }
            //var behaviors = FindObjectsOfType<MonoBehaviour>(true);
            RegisterDependencies(behaviors.ToArray());
            InjectDependencies();
        }

        #region Getter
        private static object Get(Type type)
        {
            if (!dependencies.ContainsKey(type))
            {
                throw new KeyNotFoundException($"{type.FullName} is not a registered dependency.");
            }

            return dependencies[type].Instance;
        }

        public static T Get<T>()
        {
            return (T)Get(typeof(T));
        }
        #endregion

        #region Dependency Injection
        private void RegisterDependencies(MonoBehaviour[] behaviors)
        {
            foreach (var behavior in behaviors)
            {
                #region Add Dependency
                var behaviorNamespace = behavior.GetType().Namespace;
                if (behaviorNamespace is null) continue;
                if (!behaviorNamespace.Contains("NotReaper") && !behaviorNamespace.Contains("TargetPreview")) continue;
                Type behaviorType = behavior.GetType();
                if (dependencies.ContainsKey(behaviorType))
                {
                    var dependency = dependencies[behaviorType];
                    dependency.IsSingleton = false;
                }
                else
                {
                    dependencies.Add(behaviorType, new Dependency(behavior, true));
                }
                #endregion

                #region Handle Custom Attribute Fields
                var fields = behaviorType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    var attributes = field.CustomAttributes;
                    foreach (var attribute in attributes)
                    {
                        if (attribute.AttributeType == typeof(NRInjectAttribute))
                        {
                            pendingInjections.Add(new InjectionContainer(field, behavior));
                            break;
                        }
                    }
                }
                #endregion

                #region Handle Custom Attribute Methods
                var methods = behaviorType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach(var method in methods)
                {
                    var attribute = method.GetCustomAttribute<NRListenerAttribute>();
                    if(attribute == null)
                    {
                        continue;
                    }
                    var parameters = method.GetParameters();
                    if (parameters is null || parameters.Length != 1) break;
                    var paramType = parameters[0].ParameterType;

                    if (paramType == typeof(EditorTool))
                    {
                        EditorState.ToolChanged += CreateDelegate<EditorState.ToolChangedEventHandler>(method, behavior);                               
                    }
                    else if (paramType == typeof(EditorMode))
                    {
                        EditorState.ModeChanged += CreateDelegate<EditorState.ModeChangedEventHandler>(method, behavior);
                    }
                    else if(paramType == typeof(TargetHandType))
                    {
                        EditorState.HandChanged += CreateDelegate<EditorState.HandChangedEventHandler>(method, behavior);
                    }
                    else if(paramType == typeof(SnappingMode))
                    {
                        EditorState.SnappingChanged += CreateDelegate<EditorState.SnappingChangedEventHandler>(method, behavior);
                    }
                    else if(paramType == typeof(TargetHitsound))
                    {
                        EditorState.HitsoundChanged += CreateDelegate<EditorState.HitsoundChangedEventHandler>(method, behavior);
                    }
                    else if(paramType == typeof(TargetBehavior))
                    {
                        EditorState.BehaviorChanged += CreateDelegate<EditorState.BehaviorChangedEventHandler>(method, behavior);
                    }
                    else if(paramType == typeof(bool))
                    {
                        EditorState.IsInUIChanged += CreateDelegate<EditorState.IsInUIChangedEventHandler>(method, behavior);
                    }
                }
                #endregion
            }

            #region Remove Non-Singleton Dependencies
            var remove = new List<Type>();
            foreach(var entry in dependencies)
            {
                if(!entry.Value.IsSingleton)
                {
                    remove.Add(entry.Key);
                }
            }
            foreach(var key in remove)
            {
                dependencies.Remove(key);
            }
            #endregion
        }

        private T CreateDelegate<T>(MethodInfo method, object target) where T : class
        {
            return Delegate.CreateDelegate(typeof(T), target, method) as T;
        }

        private void InjectDependencies()
        {
            foreach (var container in pendingInjections)
            {
                Type fieldType = container.Field.FieldType;

                if (dependencies.ContainsKey(fieldType))
                {
                    var dependency = dependencies[fieldType];
                    container.Field.SetValue(container.Instance, dependency.Instance);
                }
                else
                {
                    throw new KeyNotFoundException($"Can't inject dependency to {container.Field.Name} in {container.Instance}: No dependency of type {fieldType} registered.");
                }
            }
        }
        #endregion

        #region Enums
        private struct Dependency
        {
            public object Instance { get; set; }
            public bool IsSingleton { get; set; }

            public Dependency(object instance, bool isSingleton)
            {
                this.Instance = instance;
                this.IsSingleton = isSingleton;
            }
        }

        private struct InjectionContainer
        {
            public FieldInfo Field { get; }
            public object Instance { get; }

            public InjectionContainer(FieldInfo field, object instance)
            {
                this.Field = field;
                this.Instance = instance;
            }

        }
        #endregion
    }
}

