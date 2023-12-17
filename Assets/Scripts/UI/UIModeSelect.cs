using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NotReaper.Timing;
using NotReaper.UserInput;
using UnityEngine;
using UnityEngine.UI;
using NotReaper.Models;
using NotReaper.Statistics;
using NotReaper.MenuBrowser;
using NotReaper.UI.Volume;
using NotReaper.Tools.Presets;
using NotReaper.MapPreview;
using NotReaper.Tools.CustomSnapMenu;

namespace NotReaper.UI {

    public class UIModeSelect : MonoBehaviour {

        [NRInject] private UIInput uiInput;
        [NRInject] private MappingInput mappingInput;
        [NRInject] private StatisticsUI stats;
        public GameObject slider;
        public RectTransform sliderRTrans;
        public List<GameObject> buttons = new();
        public CanvasGroup volumeButton;
        public GameObject menuBrowserButton;
        [NRInject] private UIMetadata uIMetadata;
        [NRInject] private UITiming uITiming;
        [NRInject] private VolumeOverlay volume;
        [NRInject] private PresetUI presets;
        [NRInject] private Preview3DManager preview;
        [NRInject] private CustomSnapMenu snapEditor;
        public UISettings uISettings;
        public float startOffset = 80f;
        public float indexOffset = 66.6f;      

        public void EnableButtons(bool enable)
        {
            foreach(var button in buttons)
            {
                button.SetActive(enable);
            }
        }

        public void OpenSongInfo() => EditorState.SelectMode(EditorMode.Metadata);

        public void OpenVolumeOverlay() => volume.Show();

        public void OpenMenuBrowser() => MenuPickerUI.Instance.Show();

        public void OpenPathbuilder() => mappingInput.TogglePathbuilder();
        public void OpenLegacyPathbuilder() => mappingInput.ToggleChainbuilder();

        public void OpenHelp() => uiInput.ShowHelpWindow();

        public void OpenTiming() => uiInput.ShowTimingPointsWindow();

        public void OpenModifiers() => mappingInput.ToggleModifiers();

        public void OpenReviews() => uiInput.ShowReviewWindow();

        public void OpenStatistics() => stats.Show();

        public void OpenPresets() => presets.Show();
        public void OpenPreview() => preview.Show();
        public void OpenSnapEditor() => snapEditor.OnSnapClicked();

        public void UpdateUI(EditorMode mode) 
        {
            if(mode == EditorMode.Metadata)
            {
                uIMetadata.Show();
            }
            else if(mode == EditorMode.Compose)
            {
                if(uIMetadata.IsActiveMenu)
                    uIMetadata.Hide();
            }
        }
    }

}