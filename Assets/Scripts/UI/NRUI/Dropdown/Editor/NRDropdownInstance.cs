using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NRDropdownInstance : Editor
{
    private static GameObject clickedObject;
    [MenuItem("GameObject/NotReaper UI/NRDropdown", priority = 0)]
    public static void AddButton()
    {
        Create("NRDropdown");
    }

    private static GameObject Create(string objectName)
    {
        var instance = Instantiate(Resources.Load<NRDropdown>(objectName));
        instance.name = objectName;

        clickedObject = Selection.activeObject as GameObject;
        if (clickedObject != null)
        {
            instance.transform.SetParent(clickedObject.transform);
        }
        instance.transform.localScale = Vector3.one;
        instance.transform.localPosition = Vector3.zero;
        instance.Initialize();
        return instance.gameObject;
    }
}
