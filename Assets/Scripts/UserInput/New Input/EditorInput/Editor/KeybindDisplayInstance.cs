using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NotReaper.Keybinds
{
    public class KeybindDisplayInstance : Editor
    {
        
        private static GameObject clickedObject;
        [MenuItem("GameObject/NotReaper Keybinds/KeybindDisplay", priority = 0)]
        public static void AddButton()
        {
            Create("KeybindDisplay");
        }

        private static GameObject Create(string objectName)
        {
            var instance = Instantiate(Resources.Load<KeybindDisplay>(objectName));
            instance.name = objectName;

            clickedObject = Selection.activeObject as GameObject;
            if (clickedObject != null)
            {
                instance.transform.SetParent(clickedObject.transform);
            }
            instance.transform.localScale = Vector3.one;
            instance.transform.localPosition = Vector3.zero;
            return instance.gameObject;
        }
        
    }
}
