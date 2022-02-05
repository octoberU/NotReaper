using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.MapBrowser.UI
{
    /// <summary>
    /// Responsible for the Downloading Overlay.
    /// </summary>
    public class DownloadOverlay : MonoBehaviour
    {
        #region References
        [Header("Overlays")]
        [SerializeField] private FadingPanel downloadingOverlay;
        [SerializeField] private FadingPanel downloadDoneOverlay;
        [Space, Header("Buttons")]
        [SerializeField] private GameObject retryButton;
        [SerializeField] private GameObject cancelButton;
        [Space, Header("Text")]
        [SerializeField] private TextMeshProUGUI downloadProgressText;
        [SerializeField] private TextMeshProUGUI failedMapsText;
        [Space, Header("Animators")]
        [SerializeField] private Animator animatorDone;
        #endregion
        
        #region Hide and Show
        /// <summary>
        /// Shows the Downloading overlay.
        /// </summary>
        /// <param name="show">True if it should be shown.</param>
        public void ShowDownloadingOverlay(bool show)
        {
            downloadingOverlay.Show(show);
            cancelButton.SetActive(show);
        }
        /// <summary>
        /// Shows the Download Done overlay and starts it's animation.
        /// </summary>
        public void PlayDownloadDoneAnimation()
        {
            downloadDoneOverlay.gameObject.SetActive(false);
            downloadDoneOverlay.gameObject.SetActive(true);
            downloadDoneOverlay.Show(true);
        }
        /// <summary>
        /// Shows the cancel button.
        /// </summary>
        /// <param name="show">True if it should be shown.</param>
        public void ShowCancelButton(bool show)
        {
            cancelButton.SetActive(show);
        }
        /// <summary>
        /// Hides both the Downloading and Downloading Done overlays.
        /// </summary>
        public void HideOverlays()
        {
            downloadingOverlay.Show(false);
            downloadDoneOverlay.Show(false);
            retryButton.SetActive(false);
            downloadProgressText.text = "";
        }
        #endregion

        #region Text Handling
        /// <summary>
        /// Sets the text for the progrogress text field.
        /// </summary>
        /// <param name="text">The text to set it to.</param>
        public void SetProgressText(string text)
        {
            downloadProgressText.text = text;
        }
        /// <summary>
        /// Sets the amount of failed maps.
        /// </summary>
        /// <param name="count">The amount of failed maps.</param>
        public void SetFailedMapsCount(int count)
        {
            animatorDone.SetBool("success", count == 0);
            if (count == 0)
            {
                failedMapsText.text = "";
                return;
            }
            failedMapsText.text = $"{count} map{(count == 1 ? "" : "s")} failed to download.\nwould you like to retry?";
            retryButton.SetActive(true);
        }
        #endregion
    }
}

