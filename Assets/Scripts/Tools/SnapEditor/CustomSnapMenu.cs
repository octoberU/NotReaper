/*File made by MeepsKitten*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using NotReaper.Tools.ChainBuilder;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Tools.CustomSnapMenu {
    public class CustomSnapMenu : MonoBehaviour {

        public SnapPresetScrollWindow PresetScrollWindow;
        public TMP_InputField inputField;
        public Button resetButton;
        private bool confirmationOfDestructiveActionRequired = true;
        public Button confirmButton;
        public GameObject window;
        [NRInject] private ChainBuilderWindow chainbuilderWindow;
        public Michsky.UI.ModernUIPack.HorizontalSelector HorizontalSnapSelector;
        public Michsky.UI.ModernUIPack.HorizontalSelector ChainbuilderIntervalSelector;

        public Color ErrorColor = new Color (255, 10, 10, 0.59f);
        public Color NormalColor;

        enum SnapEditorMode {
            addMode,
            subtractMode,
            invalid
        }

        private SnapEditorMode mode = SnapEditorMode.invalid;

        void Start () {
            Vector3 defaultPos;
            defaultPos.x = 0;
            defaultPos.y = 0;
            defaultPos.z = -10.0f;
            window.GetComponent<RectTransform> ().localPosition = defaultPos;
            window.GetComponent<CanvasGroup> ().alpha = 0.0f;
            window.SetActive (false);
            ResetColor ();
            NRSettings.OnLoad (loadSavedSnaps);
        }
        private void loadSavedSnaps () {
            HorizontalSnapSelector.elements = NRSettings.config.snaps;
            chainbuilderWindow.pathBuilderInterval.elements = HorizontalSnapSelector.elements;
            //ChainbuilderIntervalSelector.elements = HorizontalSnapSelector.elements;
        }
        public void OnSnapSet () {
            int snap = 0;
            bool success = int.TryParse (inputField.text, out snap);
            if (success) {                                          
                
                if(HorizontalSnapSelector.elements.Contains("1/" + snap))
                {
                    return;
                }
                
                //Timeline.Instance.SetSnap (snap);
                AdjustSnapArray (snap);
                mode = SnapEditorMode.subtractMode;
                SetConfirmButtonInfo ();
                PresetScrollWindow.UpdateSnapList();
            } else {
                ColorBlock colors = inputField.colors;
                colors.normalColor = ErrorColor;
                inputField.colors = colors;
            }
        }

        public void OnSnapClicked () {
            if (Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl)) {
                OpenWindow ();
            }
        }

        public void OpenWindow () {
            window.GetComponent<CanvasGroup> ().DOFade (1.0f, 0.3f);
            HorizontalSnapSelector.elements = NRSettings.config.snaps;
            confirmButton.interactable = true;
            PresetScrollWindow.Show ();
            window.SetActive (true);
        }

        public void CloseWindow () {
            window.GetComponent<CanvasGroup> ().DOFade (0.0f, 0.3f);
            inputField.GetComponent<TMP_InputField> ().ReleaseSelection ();
            ResetColor ();
            PresetScrollWindow.Hide ();
            window.SetActive (false);
        }

        public void OnSnapInputFieldChanged () {
            ResetColor ();

            int snap = 0;
            bool success = int.TryParse (inputField.text, out snap);
            mode = SnapEditorMode.invalid;
            if (success) {               
                if ((snap >= 1) && (snap <= 128)) {
                    mode = SnapEditorMode.addMode;
                }
                
                if(HorizontalSnapSelector.elements.Contains("1/" + snap))
                {
                    mode = SnapEditorMode.invalid;
                }
            }

            SetConfirmButtonInfo ();
        }

        public void OnSnapInputFieldEntered () {
            ResetColor ();
        }

        public void ResetColor () {
            ColorBlock colors = inputField.colors;
            colors.normalColor = NormalColor;
            inputField.colors = colors;
            confirmButton.interactable = true;
        }

        public void SetConfirmButtonInfo () {
            if (mode == SnapEditorMode.addMode) {
                confirmButton.interactable = true;
                ColorBlock colors = confirmButton.colors;
                colors.normalColor = Color.white;
                confirmButton.colors = colors;
                confirmButton.GetComponentInChildren<TextMeshProUGUI> ().text = "Add Snap Preset";
            } 
            else {
                confirmButton.interactable = false;
                ColorBlock colors = confirmButton.colors;
                colors.normalColor = ErrorColor;
                confirmButton.colors = colors;
            }
        }

        private void AdjustSnapArray (int snap) {

            if (mode == SnapEditorMode.addMode) {
                HorizontalSnapSelector.elements.Add ("1/" + snap);
                HorizontalSnapSelector.elements.Sort (delegate (string l, string r) {
                    return l.Substring (2).PadLeft (3, '0').CompareTo (r.Substring (2).PadLeft (3, '0'));
                });

            } else if (mode == SnapEditorMode.subtractMode) {
                RemoveSnap (snap);
            }

            ChainbuilderIntervalSelector.elements = HorizontalSnapSelector.elements;
            NRSettings.config.snaps = HorizontalSnapSelector.elements;
            NRSettings.SaveSettingsJson ();
        }

        public void RemoveSnap (int snap) {
            Debug.Log("1/" + snap);
            
            int indexseek = 0;
            int desiredIndex = -1;
            foreach (string element in HorizontalSnapSelector.elements) {
                if (element == ("1/" + snap)) {
                    desiredIndex = indexseek;
                    break;
                }
                ++indexseek;
            }

            if (desiredIndex != -1) {
                HorizontalSnapSelector.elements.RemoveAt (desiredIndex);
                mode = SnapEditorMode.addMode;
                SetConfirmButtonInfo ();
            }

            ChainbuilderIntervalSelector.elements = HorizontalSnapSelector.elements;
            NRSettings.config.snaps = HorizontalSnapSelector.elements;
            NRSettings.SaveSettingsJson ();
        }

        public List<string> deafultSnaps = new List<string> () {
            "1/1",
            "1/2",
            "1/3",
            "1/4",
            "1/6",
            "1/8",
            "1/12",
            "1/16",
            "1/24",
            "1/32",
            "1/48",
            "1/64"
        };

        public void OnResetButton () {
            if (confirmationOfDestructiveActionRequired) {
                ColorBlock colors = resetButton.colors;
                colors.normalColor = ErrorColor;
                resetButton.colors = colors;
                resetButton.GetComponentInChildren<TextMeshProUGUI> ().text = "Click again to confirm\n(there is no way to undo this)";
                confirmationOfDestructiveActionRequired = false;
            } else {
                NRSettings.config.snaps = deafultSnaps;
                HorizontalSnapSelector.elements = deafultSnaps;
                ChainbuilderIntervalSelector.elements = deafultSnaps;

                NRSettings.SaveSettingsJson ();
                confirmationOfDestructiveActionRequired = true;

                ColorBlock colors = resetButton.colors;
                colors.normalColor = Color.white;
                resetButton.colors = colors;
                resetButton.GetComponentInChildren<TextMeshProUGUI> ().text = "Reset snap presets";
                CloseWindow ();
            }
        }

    }

}