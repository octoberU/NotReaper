using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.UI.Components;
using UnityEngine;

namespace NotReaper
{
    public class SavingPrompt : MonoBehaviour
    {
        [SerializeField] private NRButton confirmButton;
        [SerializeField] private NRButton cancelButton;

        private Action<bool> callback;
        
        private void Awake()
        {
            confirmButton.onClick.AddListener(OnConfirm);
            cancelButton.onClick.AddListener(OnDecline);
        }
        
        private void OnConfirm() => OnClick(true);
        private void OnDecline() => OnClick(false);

        private void OnClick(bool confirm)
        {
            callback?.Invoke(confirm);
            gameObject.SetActive(false);
        }
        
        
        public void ShowPrompt(Action<bool> callback)
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
            
            gameObject.SetActive(true);
            this.callback = callback;
        }
        
        public void Hide() => gameObject.SetActive(false);
    }
}
