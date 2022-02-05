using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace NotReaper.MapBrowser.UI
{
    /// <summary>
    /// Responsible for the Download Panel.
    /// </summary>
    public class DownloadPanel : FadingPanel
    {
        #region References
        [Header("Buttons")]
        [SerializeField] private Button buttonDownload;
        [SerializeField] private Button buttonClear;
        [Space, Header("Scroller")]
        [SerializeField] private Scrollbar scrollbar;
        [SerializeField] private ScrollRect scrollRect;
        [Space, Header("Text")]
        [SerializeField] private TextMeshProUGUI selectedMapCount;
        #endregion

        private float timeToScroll = 1f;
        private float originalScrollSensitivity;

        private void Start()
        {
            originalScrollSensitivity = scrollRect.scrollSensitivity;
        }

        #region UI
        /// <summary>
        /// Sets the clear button interactable.
        /// </summary>
        /// <param name="enable">True if interactable.</param>
        public void EnableClearButton(bool enable)
        {
            buttonClear.interactable = enable;
        }
        /// <summary>
        /// Shows the clear button.
        /// </summary>
        /// <param name="show">True if clear button should be shown.</param>
        public void ShowClearButton(bool show)
        {
            buttonClear.gameObject.SetActive(show);
        }
        /// <summary>
        /// Sets the download button interactable.
        /// </summary>
        /// <param name="enable">True if interactable.</param>
        public void EnableDownloadButton(bool enable)
        {
            buttonDownload.interactable = enable;
        }
        /// <summary>
        /// Updates the amount of selected maps.
        /// </summary>
        /// <param name="count">The amount of selected maps.</param>
        public void UpdateSelectedMapCount(int count)
        {
            selectedMapCount.text = count == 0 ? "no maps selected" : $"{count} {(count == 1 ? "map" : "maps")} selected";
            Show(count != 0);
        }
        #endregion

        #region Scroller
        /// <summary>
        /// Sets the scrollbar and sroll rect interactable.
        /// </summary>
        /// <param name="interactable">True if interactable.</param>
        public void SetScrollbarInteractable(bool interactable)
        {
            scrollbar.interactable = interactable;
            scrollRect.scrollSensitivity = interactable ? originalScrollSensitivity : 0f;
        }
        /// <summary>
        /// Moves the scroll rect so that RectTransform is in the middle.
        /// </summary>
        /// <param name="rect">The RectTransform to move to.</param>
        public void MoveScroller(RectTransform rect)
        {
            if (rect is null) return;
            StartCoroutine(MoveScroller(GetScrollerSnapPosition(rect)));
        }
        /// <summary>
        /// Gets the desired position based on RectTransform's position.
        /// </summary>
        /// <param name="rectTransform">The RectTransform to move to.</param>
        /// <returns>The desired position.</returns>
        private Vector2 GetScrollerSnapPosition(RectTransform rectTransform)
        {

            Canvas.ForceUpdateCanvases();
            Vector2 viewportLocalPosition = scrollRect.viewport.localPosition;
            Vector2 childLocalPosition = rectTransform.localPosition;
            Vector2 result = new Vector2(
                0 - (viewportLocalPosition.x + childLocalPosition.x),
                0 - (viewportLocalPosition.y + childLocalPosition.y)
            );
            return result;
        }
        /// <summary>
        /// Moves the scroll rect so that RectTransform is in the middle.
        /// </summary>
        /// <param name="newPosition">The position to move to.</param>
        /// <returns></returns>
        private IEnumerator MoveScroller(Vector3 newPosition)
        {
            Vector3 startPos = scrollRect.content.localPosition;
            float time = 0f;
            while (time < 1f)
            {
                time += Time.deltaTime / timeToScroll;
                scrollRect.content.localPosition = Vector3.Lerp(startPos, newPosition, time);
                yield return null;
            }
        }
        #endregion
    }
}

