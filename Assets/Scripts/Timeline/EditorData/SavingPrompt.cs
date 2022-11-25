using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NotReaper
{
    public class SavingPrompt : NRMenu
    {
        [SerializeField] private NRButton confirmButton;
        [SerializeField] private NRButton cancelButton;

        private Action<bool> callback;
        
        protected override void Awake()
        {
            base.Awake();
            confirmButton.onClick.AddListener(OnConfirm);
            cancelButton.onClick.AddListener(OnDecline);
            gameObject.SetActive(false);
        }

        public override void Show()
        {
            if (callback == null)
            {
                Debug.LogError("Callback is null!");
                return;
            }

            if (!EditorFile.IsAudicaFileLoaded)
            {
                callback.Invoke(false);
                return;
            }
            
            OnActivated();
        }

        private void OnConfirm() => OnClick(true);
        private void OnDecline() => OnClick(false);

        private void OnClick(bool confirm)
        {
            callback?.Invoke(confirm);
            callback = null;
            OnDeactivated();
        }
        
        
        public void ShowPrompt(Action<bool> callback)
        {
            this.callback = callback;
            Show();
        }

        public override void Hide()
        {
            callback?.Invoke(false);
            callback = null;
            OnDeactivated();
        }
        public override void ShowHelp()
        {
            
        }

        protected override void OnEscPressed(InputAction.CallbackContext context) => Hide();
    }
}
