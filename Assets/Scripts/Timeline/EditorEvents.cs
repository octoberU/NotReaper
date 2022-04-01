using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EditorEvents
{
    public static UnityEvent onReset;

    public static void RaiseEvent(UnityEvent eventToRaise)
    {
        eventToRaise?.Invoke();
    }
}
