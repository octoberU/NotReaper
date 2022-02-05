using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Linq;

namespace NotReaper.MapBrowser.UI
{
    /// <summary>
    /// Responsible for the Filter Panel.
    /// </summary>
    public class FilterPanel : FadingPanel
    {
        #region References
        [Header("Buttons")]
        [SerializeField] private Button buttonClearSearch;
        [SerializeField] private Button buttonAddPage;
        [Space, Header("Toggles")]
        [SerializeField] private Toggle toggleSong;
        [SerializeField] private Toggle toggleArtist;
        [SerializeField] private Toggle toggleMapper;
        [Space]
        [SerializeField] private Toggle toggleAlmostCurated;
        [SerializeField] private Toggle toggleCurated;
        [Space]
        [SerializeField] private Toggle toggleBeginner;
        [SerializeField] private Toggle toggleStandard;
        [SerializeField] private Toggle toggleAdvanced;
        [SerializeField] private Toggle toggleExpert;
        #endregion

        private List<Toggle> toggles;
        private void Start()
        {
            //get all the toggles of this instance using reflection.
            toggles = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(t => t.FieldType == typeof(Toggle)).Select(t => (Toggle)t.GetValue(this)).ToList();
        }

        #region UI
        /// <summary>
        /// Sets the button interactable.
        /// </summary>
        /// <param name="enable">True if interactable.</param>
        public void EnableAddPageButton(bool enable)
        {
            buttonAddPage.interactable = enable;
        }

        /// <summary>
        /// Sets all toggles to off.
        /// </summary>
        public void Clear()
        {
            foreach (var toggle in toggles) toggle.isOn = false;
        }
        #endregion

        #region Getter
        /// <summary>
        /// Get CurationState based on the UI's toggle state.
        /// </summary>
        /// <returns>The selected CurationState.</returns>
        public CurationState GetSelectedCurationState()
        {
            return toggleCurated.isOn ? CurationState.Curated : toggleAlmostCurated.isOn ? CurationState.Semi : CurationState.None;
        }
        /// <summary>
        /// Get FilterState based on the UI's toggle state.
        /// </summary>
        /// <returns>The selected FilterState.</returns>
        public FilterState GetSelectedFilterState()
        {
            return toggleSong.isOn ? FilterState.Song : toggleArtist.isOn ? FilterState.Artist : toggleMapper.isOn ? FilterState.Mapper : FilterState.All;
        }
        /// <summary>
        /// Get selected difficulties based on the UI's toggle state.
        /// </summary>
        /// <returns>The selected difficulties.</returns>
        public bool[] GetSelectedDifficulties()
        {
            return new bool[] { toggleBeginner.isOn, toggleStandard.isOn, toggleAdvanced.isOn, toggleExpert.isOn };
        }
        #endregion
    
    }

}
