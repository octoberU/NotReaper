using NotReaper.UserInput;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorInputDisabler : MonoBehaviour
{
    private void Awake()
    {
        EditorInput.disableInputWhenActive.Add(gameObject);
    }

    public void Enable(bool enable)
    {
        if (enable && !EditorInput.disableInputWhenActive.Contains(gameObject)) EditorInput.disableInputWhenActive.Add(gameObject);
        else if (!enable && EditorInput.disableInputWhenActive.Contains(gameObject)) EditorInput.disableInputWhenActive.Remove(gameObject);
    }
}
