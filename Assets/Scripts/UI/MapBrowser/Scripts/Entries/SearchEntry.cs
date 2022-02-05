using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using NotReaper.ReviewSystem;
using System.Linq;
using UnityEngine.UI;
using NotReaper.MapBrowser.UI;
using DG.Tweening;
namespace NotReaper.MapBrowser.Entries
{
    /// <summary>
    /// Represents a search entry.
    /// </summary>
    public class SearchEntry : MonoBehaviour
    {
        #region References
        [Header("Images")]
        [SerializeField] private Image curatedDisplay;
        [SerializeField] private Image outline;
        [SerializeField] private Image beginnerDisplay;
        [SerializeField] private Image standardDisplay;
        [SerializeField] private Image advancedDisplay;
        [SerializeField] private Image expertDisplay;
        [SerializeField] private Image downloadedSprite;
        [Space, Header("Text")]
        [SerializeField] private TextMeshProUGUI songNameDisplay;
        [SerializeField] private TextMeshProUGUI artistAuthorNameDisplay;
        [Space, Header("Curated Icons")]
        [SerializeField] private Sprite spriteCurated;
        [SerializeField] private Sprite spriteSemi;
        [Space, Header("Difficulty Icons")]
        [SerializeField] private Sprite beginnerSprite;
        [SerializeField] private Sprite standardSprite;
        [SerializeField] private Sprite advancedSprite;
        [SerializeField] private Sprite expertSprite;
        [Space]
        [SerializeField] private Sprite noBeginnerSprite;
        [SerializeField] private Sprite noStandardSprite;
        [SerializeField] private Sprite noAdvancedSprite;
        [SerializeField] private Sprite noExpertSprite;
        #endregion

        #region Fields
        public MapData Data { get; private set; }

        private SelectedEntry selectedEntry = null;

        private Color defaultOutlineColor;
        private Color notDownloadedColor = new Color(.54f, .54f, .54f, .4f);
        private Color downloadedColor = new Color(.1f, .7f, .1f, 1f);
        private Color noSpriteColor = new Color(1, 1, 1, 0);
        private Color selectedColor = new Color(.5f, 1, .5f, 1);

        private CanvasGroup canvas;
        #endregion

        #region Awake and OnEnable
        private void Awake()
        {
            canvas = GetComponent<CanvasGroup>();
            defaultOutlineColor = outline.color;
        }

        private void OnEnable()
        {
            canvas.alpha = 0f;
            canvas.DOFade(1f, .5f);
        }
        #endregion

        #region Data Handling
        /// <summary>
        /// Sets this entry's MapData and upadates it's display.
        /// </summary>
        /// <param name="data">The MapData to assing to this entry.</param>
        public void SetMapData(MapData data)
        {
            Data = data;
            selectedEntry = SpawnManager.Instance.GetSelectedEntry(data);
            UpdateDisplay();
        }
        /// <summary>
        /// Clears MapData and selectedEntry from this entry.
        /// </summary>
        public void ClearData()
        {
            Data = null;
            selectedEntry = null;
        }
        #endregion

        #region Display Handling
        /// <summary>
        /// Updates this entry's display.
        /// </summary>
        private void UpdateDisplay()
        {
            if (Data is null)
            {
                return;
            }
            outline.color = Data.Selected ? selectedColor : defaultOutlineColor;
            songNameDisplay.text = Data.SongName.ToLower();
            downloadedSprite.color = Data.Downloaded ? downloadedColor : notDownloadedColor;
            artistAuthorNameDisplay.text = $"{Data.Artist} ・ map by {Data.Mapper}".ToLower();
            Sprite sprite = GetCuratedSprite();
            curatedDisplay.sprite = sprite;
            curatedDisplay.color = sprite is null ? noSpriteColor : Color.white;
            SetDifficultySprites();
        }
        /// <summary>
        /// Updates the downloaded icon of this entry.
        /// </summary>
        public void UpdateDownloadedIcon()
        {
            downloadedSprite.color = Data.Downloaded ? downloadedColor : notDownloadedColor;
        }
        /// <summary>
        /// Get the appropriate curation sprite for this entry's state.
        /// </summary>
        /// <returns></returns>
        private Sprite GetCuratedSprite()
        {
            switch (Data.State)
            {
                case CurationState.Curated:
                    return spriteCurated;
                case CurationState.Semi:
                    return spriteSemi;
                default:
                    return null;
            }
        }
        /// <summary>
        /// Sets the difficulty sprites according to available difficulties.
        /// </summary>
        private void SetDifficultySprites()
        {
            beginnerDisplay.sprite = Data.Beginner ? beginnerSprite : noBeginnerSprite;
            standardDisplay.sprite = Data.Standard ? standardSprite : noStandardSprite;
            advancedDisplay.sprite = Data.Advanced ? advancedSprite : noAdvancedSprite;
            expertDisplay.sprite = Data.Expert ? expertSprite : noExpertSprite;
        }
        #endregion

        #region Selection Handling
        /// <summary>
        /// Sets this entry selected and spawns a selected entry if necessary.
        /// </summary>
        /// <param name="selected">True if selected.</param>
        public void SetSelected(bool selected)
        {
            Data.SetSelected(selected);
            outline.color = Data.Selected ? selectedColor : defaultOutlineColor;
            if (Data.Selected)
            {
                selectedEntry = SpawnManager.Instance.SpawnSelectedEntry(Data);
            }
            else
            {
                if(selectedEntry != null)
                {
                    SpawnManager.Instance.RemoveSelectedEntry(selectedEntry);
                    selectedEntry = null;
                }
            }

            UIManager.Instance.UpdateDownloadButton();
        }
        /// <summary>
        /// Called through UI, when entry is selected with a mouse click.
        /// </summary>
        public void OnSelected()
        {
            if (Data.Downloaded) return;
            SetSelected(!Data.Selected);
        }
        #endregion
    }
}

