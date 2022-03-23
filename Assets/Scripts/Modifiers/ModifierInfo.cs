using System.Collections;
using System.Collections.Generic;
using NotReaper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using NotReaper.UserInput;
using UnityEngine.InputSystem;

public class ModifierInfo : NRMenu
{
    public static bool isOpened = false;
    public static ModifierInfo Instance { get; private set; } = null;
    public CanvasGroup window;
    private CanvasGroup canvas;
    protected override void Awake()
    {
        base.Awake();
        if(Instance is null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Trying to create second ModiferInfo Instance.");
            return;
        }

        var t = transform;
        var position = t.localPosition;
        t.localPosition = new Vector3(0, position.y, position.z);
        canvas = GetComponent<CanvasGroup>();
        canvas.alpha = 0f;
        gameObject.SetActive(false);
    }

    public override void Show()
    {
        OnActivated();
        canvas.DOFade(1f, .3f);
        transform.position = Vector3.zero;
        isOpened = true;
    }



    public override void Hide()
    {
        isOpened = false;

        canvas.DOFade(0f, .3f).OnComplete(() =>
        {
            OnDeactivated();
        });
    }

    public override void ShowHelp() { }

    protected override void OnEscPressed(InputAction.CallbackContext context)
    {
        Hide();
    }
}
