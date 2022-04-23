using NotReaper.Models;
using NotReaper.Overlays;
using NotReaper.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.ReviewSystem
{
    public class ReviewOverlay : NROverlay
    {
        [SerializeField] private OnHover onHover;
        [SerializeField] private GameObject scrollbar;
        [NRInject] ReviewManager manager;
        protected override void Start()
        {
            onHover.onHover.AddListener(OnHoverChanged);
            base.Start();
        }

        private void OnHoverChanged(bool isHovering)
        {
            bool enable = !(isHovering && scrollbar.activeInHierarchy);
            manager.EnableScrubbing(enable);
        }
        public override void Show() => OnActivated();
        public override void Hide() => OnDeactivated();
        public override void ShowHelp() => NRHelp.Instance.ShowReview();
        public void OnDeleteCommentClicked() => manager.RemoveComment();
        public void OnSelectCuesClicked() => manager.SelectCues(true);
        public void OnSaveCommentCLicked() => manager.SaveComment();
        public void OnNewCommentClicked() => manager.NewComment();
        public void OnShowSuggestionClicked() => manager.ShowSuggestion();
        public void OnCheckCommentClicked() => manager.ToggleCommentChecked();
        public void OnMakeSuggestionClicked() => manager.EditSuggestion();
        public void OnToggleModeClicked() => manager.ToggleMode();
        public void OnSaveAndOpenClicked() => manager.Export();
        public void OnLoadClicked() => manager.Load();
        public void OnPreviousClicked() => manager.PreviousComment();
        public void OnNextClicked() => manager.NextComment();
        public void OnHideCommentsClicked() => manager.ToggleComments();
        public void OnCloseClicked() => manager.ToggleWindow();

        [NRListener]
        protected override void OnEditorModeChanged(EditorMode mode)
        {
            if (!gameObject.activeInHierarchy) return;

            if(mode != EditorMode.Compose)
            {
                manager.ToggleWindow();
            }
        }
    }
}

