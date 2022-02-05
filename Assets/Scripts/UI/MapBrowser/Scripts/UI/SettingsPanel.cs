using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB;
using NotReaper.MapBrowser.API;
namespace NotReaper.MapBrowser.UI
{
    /// <summary>
    /// Represents the Settings Panel.
    /// </summary>
    public class SettingsPanel : FadingPanel
    {
        #region References
        [Header("Toggles")]
        [SerializeField] private Toggle toggleDefault;
        [SerializeField] private Toggle toggleCustom;
        [Space, Header("Buttons")]
        [SerializeField] private Button buttonSelectFolder;
        [Space, Header("Text")]
        [SerializeField] private TextMeshProUGUI textLocation;
        [SerializeField] private TextMeshProUGUI textDeleteAfterDays;
        [SerializeField] private TMP_InputField inputDays;
        #endregion

        internal bool isOpen = false;

        protected override void Awake()
        {
            base.Awake();
            NRSettings.OnLoad(() => LoadUI());
        }
        /// <summary>
        /// Toggles the visibility of the Settings Panel and saves settings if it gets closed.
        /// </summary>
        public void ToggleShow()
        {
            if (isOpen)
            {
                NRSettings.SaveSettingsJson();
                SearchManager.Instance.RescanDownloadedMaps();
            }
            isOpen = !isOpen;
            Show(isOpen);
        }

        #region UI Updates
        /// <summary>
        /// Sets values saved in settings in UI.
        /// </summary>
        private void LoadUI()
        {
            bool isDefault = NRSettings.config.downloadSaveLocation == 0;
            toggleDefault.SetIsOnWithoutNotify(isDefault);
            toggleCustom.SetIsOnWithoutNotify(!isDefault);
            UpdateSaveLocation();
            inputDays.SetTextWithoutNotify(NRSettings.config.downloadDeleteAfterDays.ToString());
            UpdateDaysText();
        }
        /// <summary>
        /// Updates location text and shows Select Folder button if custom location is selected.
        /// </summary>
        private void UpdateSaveLocation()
        {
            bool defaultLocation = NRSettings.config.downloadSaveLocation == 0;
            textLocation.text = defaultLocation ? "Default location: NotReaper/downloads" : "Custom location: " + NRSettings.config.downloadCustomSaveLocation;
            buttonSelectFolder.gameObject.SetActive(!defaultLocation);
        }
        /// <summary>
        /// Updates the text explaining after how many days maps get deleted.
        /// </summary>
        /// <param name="days">The amount of days selected.</param>
        private void UpdateDaysText()
        {
            string deleteText;
            int days = NRSettings.config.downloadDeleteAfterDays;
            if (days > 0)
            {
                deleteText = "automatically delete maps in\n" +
                    "default location after " +
                    $"<color=orange>{days}</color> day{(days == 1 ? "" : "s")}";
            }
            else
            {
                deleteText = "<color=orange>never</color> delete maps in\n" +
                    "default location";
            }
            textDeleteAfterDays.text = deleteText;
        }
        #endregion

        #region UI Events
        /// <summary>
        /// Called through UI when a toggle changes. Updates Save location.
        /// </summary>
        public void OnToggleChanged()
        {
            bool isDefault = toggleDefault.isOn;
            NRSettings.config.downloadSaveLocation = isDefault ? 0 : 1;
            UpdateSaveLocation();
        }
        /// <summary>
        /// Called through UI when the days input field changes. Updates days text.
        /// </summary>
        public void OnDaysChanged()
        {            
            if(!int.TryParse(inputDays.text, out int days))
            {
                inputDays.SetTextWithoutNotify("0");
            }
            NRSettings.config.downloadDeleteAfterDays = days;
            UpdateDaysText();
        }
        /// <summary>
        /// Opens a FolderBrowser dialog and lets the user pick a download location.
        /// </summary>
        public void OnSelectFolderClicked()
        {
            string[] folder = StandaloneFileBrowser.OpenFolderPanel("Pick Custom Location", NRSettings.config.downloadCustomSaveLocation, false);
            if(folder != null)
            {
                NRSettings.config.downloadCustomSaveLocation = folder[0];
                UpdateSaveLocation();
            }
        }
        #endregion
    }
}

