using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NotReaper.MapBrowser.API;

namespace NotReaper.MapBrowser.Entries
{
    /// <summary>
    /// Represents a selected map entry.
    /// </summary>
    public class SelectedEntry : MonoBehaviour
    {
        #region References
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI songName;
        [SerializeField] private TextMeshProUGUI artistMapper;
        [Space, Header("Images")]
        [SerializeField] private Image progress;
        [Space, Header("Animations")]
        [SerializeField] private GameObject successAnimation;
        [SerializeField] private GameObject failedAnimation;
        [Space, Header("Buttons")]
        [SerializeField] private Button button;
        #endregion
        public MapData Data { get; private set; }

        #region Data Handling
        /// <summary>
        /// Sets the MapData of this entry.
        /// </summary>
        /// <param name="data">The MapData to assing to this entry.</param>
        public void SetData(MapData data)
        {
            this.Data = data;
            button.interactable = true;
            songName.text = data.SongName;
            artistMapper.text = $"{data.Artist} ・ {data.Mapper}".ToLower();       
            progress.fillAmount = 0f;
            successAnimation.SetActive(Data.Downloaded);
            failedAnimation.SetActive(false);
        }
        /// <summary>
        /// Clears this entry's data and resets animations.
        /// </summary>
        public void ClearData()
        {
            progress.fillAmount = 0f;
            ResetAnimations();
            //also deselect cached map
            SearchManager.Instance.DeselectCachedMap(Data.RequestUrl, Data.ID);
            Data = null;
        }
        #endregion

        #region Selection Handling
        /// <summary>
        /// Called through UI, when entry is deselected with a mouse click.
        /// </summary>
        public void OnDeselected()
        {
            successAnimation.SetActive(false);
            SearchManager.Instance.DeselectCachedMap(Data.RequestUrl, Data.ID);
            SpawnManager.Instance.RemoveSelectedEntry(this);
        }
        /// <summary>
        /// Sets this entry interactable.
        /// </summary>
        /// <param name="interactable">True if it should be interactable.</param>
        /// <remarks>We set this to false during download procedure so we can't deselect entries during it.</remarks>
        public void SetButtonInteractable(bool interactable)
        {
            button.interactable = interactable;
        }
        #endregion

        #region Animation Handling
        /// <summary>
        /// Plays animations and updates downloaded icon on corresponding search entry.
        /// </summary>
        /// <param name="success">True if download was successful.</param>
        public void OnDownloaded(bool success)
        {
            if (success) successAnimation.SetActive(true);
            else failedAnimation.SetActive(true);
            if(Data.BrowserEntry) Data.BrowserEntry.UpdateDownloadedIcon();
        }
        /// <summary>
        /// Reset this entry's animations.
        /// </summary>
        public void ResetAnimations()
        {
            successAnimation.SetActive(false);
            failedAnimation.SetActive(false);
        }
        #endregion
    }
}

