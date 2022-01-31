using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using NotReaper.ReviewSystem;
using System.Linq;
using UnityEngine.UI;

namespace NotReaper.MapBrowser
{
    public class MapBrowserEntry : MonoBehaviour
    {
        #region References
        [SerializeField] private Image curatedDisplay;
        [SerializeField] private Image outline;
        [SerializeField] private Image beginnerDisplay;
        [SerializeField] private Image standardDisplay;
        [SerializeField] private Image advancedDisplay;
        [SerializeField] private Image expertDisplay;
        [SerializeField] private TextMeshProUGUI songNameDisplay;
        [SerializeField] private TextMeshProUGUI artistAuthorNameDisplay;

        [Space]
        [Header("Icons")]
        [SerializeField] private Sprite spriteCurated;
        [SerializeField] private Sprite spriteSemi;
        [Space]
        [SerializeField] private Sprite beginnerSprite;
        [SerializeField] private Sprite standardSprite;
        [SerializeField] private Sprite advancedSprite;
        [SerializeField] private Sprite expertSprite;
        [Space]
        [SerializeField] private Sprite noBeginnerSprite;
        [SerializeField] private Sprite noStandardSprite;
        [SerializeField] private Sprite noAdvancedSprite;
        [SerializeField] private Sprite noExpertSprite;
        [SerializeField] private Image downloadedSprite;
        #endregion

        public MapData Data { get; private set; }
        private Color defaultColor;
        private SelectedMapEntry selectedEntry = null;
        private Color notDownloadedColor = new Color(.54f, .54f, .54f, .4f);
        private Color downloadedColor = new Color(.1f, .7f, .1f, 1f);
        private void Start()
        {
            defaultColor = outline.color;
        }

        public void SetMapData(MapData data)
        {
            Data = data;
            selectedEntry = MapEntrySpawnManager.Instance.GetSelectedMapEntry(data);
            UpdateDisplay();
        }

        public void ClearData()
        {
            Data = null;
            selectedEntry = null;
        }

        private Color noSpriteColor = new Color(1, 1, 1, 0);
        private Color selectedColor = new Color(.5f, 1, .5f, 1);
        //#7DFF7D
        private void UpdateDisplay()
        {
            if (Data is null)
            {
                return;
            }
            //SetSelected(Data.Selected);
            outline.color = Data.Selected ? selectedColor : defaultColor;
            songNameDisplay.text = Data.SongName.ToLower();
            downloadedSprite.color = Data.Downloaded ? downloadedColor : notDownloadedColor;
            artistAuthorNameDisplay.text = $"{Data.Artist} ・ map by {Data.Mapper}".ToLower();
            Sprite sprite = GetCuratedSprite(Data.State);
            curatedDisplay.sprite = sprite;
            curatedDisplay.color = sprite is null ? noSpriteColor : Color.white;
            SetDifficultySprites();
        }

        public void UpdateDownloadedIcon()
        {
            downloadedSprite.color = Data.Downloaded ? downloadedColor : notDownloadedColor;
        }

        private Sprite GetCuratedSprite(CurationState state)
        {
            switch (state)
            {
                case CurationState.Curated:
                    return spriteCurated;
                case CurationState.Semi:
                    return spriteSemi;
                default:
                    return null;
            }
        }

        private void SetDifficultySprites()
        {
            beginnerDisplay.sprite = Data.Beginner ? beginnerSprite : noBeginnerSprite;
            standardDisplay.sprite = Data.Standard ? standardSprite : noStandardSprite;
            advancedDisplay.sprite = Data.Advanced ? advancedSprite : noAdvancedSprite;
            expertDisplay.sprite = Data.Expert ? expertSprite : noExpertSprite;
        }

        public void SetSelected(bool selected)
        {
            Data.SetSelected(selected);
            outline.color = Data.Selected ? selectedColor : defaultColor;
            if (Data.Selected)
            {
                selectedEntry = MapEntrySpawnManager.Instance.SpawnSelectedEntry(Data);
            }
            else
            {
                if(selectedEntry != null)
                {
                    MapEntrySpawnManager.Instance.RemoveSelectedEntry(selectedEntry);
                    selectedEntry = null;
                }
            }
            
            MapBrowserWindow.Instance.UpdateDownloadButton();
        }

        public void OnSelected()
        {
            SetSelected(!Data.Selected);
        }
    }
}

