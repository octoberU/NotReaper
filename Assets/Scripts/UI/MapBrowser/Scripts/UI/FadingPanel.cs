using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
namespace NotReaper.MapBrowser.UI
{
    /// <summary>
    /// Handles Fadein/out behavior. Should be implemented as base class.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FadingPanel : MonoBehaviour
    {
        [SerializeField] private bool hiddenOnStart = false;

        private CanvasGroup canvas;

        protected virtual void Awake()
        {
            canvas = GetComponent<CanvasGroup>();
            canvas.blocksRaycasts = !hiddenOnStart;
            canvas.interactable = !hiddenOnStart;
            canvas.alpha = hiddenOnStart ? 0f : 1f;
        }
        /// <summary>
        /// Shows the panel.
        /// </summary>
        /// <param name="show">True if panel should be shown.</param>
        public virtual void Show(bool show)
        {
            canvas.blocksRaycasts = show;
            canvas.interactable = show;
            canvas.DOFade(show ? 1f : 0f, .3f);
            //gameObject.SetActive(show);
        }
    }
}

