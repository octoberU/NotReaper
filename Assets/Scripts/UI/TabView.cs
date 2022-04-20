using DG.Tweening;
using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI
{
    public class TabView
    {
        private List<CanvasGroup> views = new();
        private List<NRButton> buttons = new();

        private CanvasGroup activeView = null;

        public void AddView(CanvasGroup view, NRButton button)
        {
            if (!views.Contains(view))
            {
                views.Add(view);
                buttons.Add(button);
            }
        }

        public void HideAllViews()
        {
            foreach (var view in views)
            {
                view.alpha = 0f;
                view.interactable = false;
                view.blocksRaycasts = false;
            }
        }

        public void SetDefaultView(CanvasGroup view, NRButton button)
        {
            view.alpha = 1f;
            view.interactable = true;
            view.blocksRaycasts = true;

            activeView = view;
            button.Select(false, false);
        }

        public void ChangeView(CanvasGroup newView, NRButton button)
        {
            if(activeView == null)
            {
                SetDefaultView(newView, button);
                return;
            }

            var oldView = activeView;
            oldView.DOFade(0f, .3f);
            oldView.interactable = false;
            oldView.blocksRaycasts = false;

            newView.DOFade(1f, .3f);
            newView.interactable = true;
            newView.blocksRaycasts = true;

            activeView = newView;
            button.Select(false, false);
        }

        public void ChangeView(int index)
            => ChangeView(views[index], buttons[index]);
    }
}
