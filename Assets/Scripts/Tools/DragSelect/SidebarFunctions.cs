using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotReaper.Grid;
using NotReaper.Targets;
using NotReaper.UI;
using NotReaper.UserInput;
using NotReaper.Managers;
using NotReaper.Models;
using UnityEngine.UI;
using DG.Tweening;
using NotReaper.UI.Components;

namespace NotReaper.Tools {
    public class SidebarFunctions : MonoBehaviour
    {
        public Timeline timeline;
        
        public UndoRedoManager undoRedoManager;

        [SerializeField] private CanvasGroup targetPanel;
        [SerializeField] private CanvasGroup positionPanel;
        [SerializeField] private CanvasGroup deselectionPanel;
        [SerializeField] private RectTransform bottomPanel;
        [SerializeField] private RectTransform topPanel;
        [SerializeField] private Slider playbackSpeedSlider;
        [SerializeField] private List<CanvasGroup> panelsToHide = new();

        private List<CanvasGroup> buttonPanels = new();

        [SerializeField] private GameObject hiddenButtons;
        [Space, Header("Animation")]
        private float animationDuration = .15f;

        private CanvasGroup currentPanel = null;
        private RectTransform rect;
        private CanvasGroup backgroundCanvas;
        private bool isOpen = false;

        private void Awake()
        {
            playbackSpeedSlider.onValueChanged.AddListener(EditorAudio.SetPlaybackSpeed);
            EditorAudio.onPlaybackSpeedChanged += playbackSpeedSlider.SetValueWithoutNotify;
        }

        private void Start()
        {
            rect = GetComponent<RectTransform>();
            EditorNotes.onSelectedNoteCountChanged += OnNoteCountChanged;
            buttonPanels.Add(targetPanel);
            buttonPanels.Add(positionPanel);
            buttonPanels.Add(deselectionPanel);
            foreach(var canvas in panelsToHide)
            {
                canvas.alpha = 0f;
                canvas.gameObject.SetActive(true);
            }
            foreach(var canvas in buttonPanels)
            {
                canvas.alpha = 0f;
                canvas.gameObject.SetActive(false);
            }
            var size = topPanel.sizeDelta;
            size.y = 0f;
            topPanel.sizeDelta = size;
            backgroundCanvas = topPanel.GetComponent<CanvasGroup>();
            backgroundCanvas.alpha = 0f;
        }

        private void MoveBackground(bool grow, Action onComplete)
        {
            var size = topPanel.sizeDelta;
            size.y = grow ? rect.sizeDelta.y - bottomPanel.sizeDelta.y : 0f;
            var sequence = DOTween.Sequence();
            sequence.Append(topPanel.DOSizeDelta(size, animationDuration));
            sequence.Join(backgroundCanvas.DOFade(grow ? 1f : 0f, animationDuration));
            sequence.SetEase(grow ? Ease.OutQuart : Ease.InQuart);
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private void OnNoteCountChanged(int count)
        {
            bool open = count > 0;
            if (open == isOpen) return;
            isOpen = open;
            FadePanels(isOpen);           
        }

        private void FadePanels(bool fadeIn)
        {
            MoveBackground(fadeIn, () => 
            {
                foreach (var canvas in panelsToHide)
                {
                    FadePanel(canvas, fadeIn);
                }
            });
            
        }

        private void FadePanel(CanvasGroup canvas, bool fadeIn)
        {
            canvas.interactable = fadeIn;
            canvas.DOFade(fadeIn ? 1f : 0f, animationDuration);
        }

        private void SwapPanels(CanvasGroup to)
        {
            if (currentPanel != null)
            {
                var from = currentPanel;
                from.DOFade(0f, animationDuration).OnComplete(() =>
                {
                    from.gameObject.SetActive(false);
                    to.gameObject.SetActive(true);
                    to.DOFade(1f, animationDuration);
                });
            }
            else
            {
                to.gameObject.SetActive(true);
                to.DOFade(1f, animationDuration);
            }
            currentPanel = to;
        }
        public void FlipTargetsVertical() => EditorTargets.FlipSelectedTargetsVertical();
        public void FlipTargetsHorizontal() => EditorTargets.FlipSelectedTargetsHorizontal();
        public void SwapTargets() => EditorTargets.SwapSelecedTargetsColor();
        public void ReverseTargets() => EditorTargets.ReverseSelectedTargets();
        public void RotateLeft() => EditorTargets.RotateSelectedTargets(15);
        public void RotateRight() => EditorTargets.RotateSelectedTargets(-15);
        //public void ScaleUp() => timeline.Scale(EditorData.SelectedNotes, 1.1f);
        //public void ScaleDown() => timeline.Scale(EditorData.SelectedNotes, 0.9f);
        public void Undo() => UndoRedoManager.Undo();//undoRedoManager.Undo();
        public void Redo() => UndoRedoManager.Redo();
        public void DeselectBehavior(int behavior) => EditorTargets.DeselectBehavior((TargetBehavior)behavior);
        public void DeselectHand(int handType) => EditorTargets.DeselectHand((TargetHandType)handType);
        public void ScaleUpHorizontal() => EditorTargets.ScaleSelectedTargets(new(1.1f, 1f));
        public void ScaleUpVertical() => EditorTargets.ScaleSelectedTargets(new(1f, 1.1f));
        public void ScaleDownHorizontal() => EditorTargets.ScaleSelectedTargets(new(.9f, 1f));
        public void ScaleDownVertical() => EditorTargets.ScaleSelectedTargets(new(1f, .9f));
        public void ShowTargetPanel() => SwapPanels(targetPanel);
        public void ShowPositionPanel() => SwapPanels(positionPanel);
        public void ShowDeselectionPanel() => SwapPanels(deselectionPanel);
        //public void UpdatePlaybackSpeedSlider() => playbackSpeedSlider.SetValueWithoutNotify(EditorAudio.PlaybackSpeed);
    }
}