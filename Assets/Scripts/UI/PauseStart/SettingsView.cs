using DG.Tweening;
using NotReaper.UI.Components;
using NotReaper.UI.Customization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI
{
    public class SettingsView : View
    {
        [SerializeField] private CanvasGroup configPanel;
        [SerializeField] private CustomizationPanel customizationPanel;
        [SerializeField] private CanvasGroup previewPanel;
        [SerializeField] private NRIconInputGroup inputGroup;

        private void Awake()
        {
            inputGroup.enabled = false;
        }

        private void Start()
        {
            ShowConfigPanel();
        }

        public override void Hide() 
        {
            inputGroup.enabled = false;
            customizationPanel.EnablePreviewWindow(false);
            NRSettings.SaveSettingsJson();
        }

        public override void Show() 
        {
            inputGroup.enabled = true;
            customizationPanel.EnablePreviewWindow(true);
        }

        public void ShowConfigPanel()
        {
            configPanel.DOFade(1f, .3f);
            configPanel.interactable = true;
            configPanel.blocksRaycasts = true;
            customizationPanel.Hide();

            previewPanel.DOFade(0f, .3f);
            previewPanel.interactable = false;
            previewPanel.blocksRaycasts = false;

        }

        public void ShowCustomizationPanel()
        {
            configPanel.DOFade(0f, .3f);
            configPanel.interactable = false;
            configPanel.blocksRaycasts = false;

            previewPanel.DOFade(0f, .3f);
            previewPanel.interactable = false;
            previewPanel.blocksRaycasts = false;

            customizationPanel.Show();
        }

        public void ShowPreviewerPanel()
        {
            configPanel.DOFade(0f, .3f);
            customizationPanel.Hide();
            configPanel.interactable = false;
            configPanel.blocksRaycasts = false;
            previewPanel.DOFade(1f, .3f);
            previewPanel.interactable = true;
            previewPanel.blocksRaycasts = true;
        }
    }
}
