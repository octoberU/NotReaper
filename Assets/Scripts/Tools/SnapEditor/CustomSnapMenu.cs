/*File made by MeepsKitten*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using NotReaper.Tools.ChainBuilder;
using NotReaper.Tools.PathBuilder;
using NotReaper.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NotReaper.Tools.CustomSnapMenu
{
    public class CustomSnapMenu : NRMenu
    {

        public SnapPresetScrollWindow PresetScrollWindow;
        public TMP_InputField inputField;
        public NRButton resetButton;
        private bool confirmationOfDestructiveActionRequired = true;
        public NRButton confirmButton;
        public GameObject window;
        [NRInject] private ChainBuilderWindow chainbuilderWindow;
        private HorizontalSelector timelineBeatSnapSelector;
        private HorizontalSelector chainBuilderIntervalSelector;
        private HorizontalSelector pathbuilderIntervalSelector;

        public Color ErrorColor = new Color(255, 10, 10, 0.59f);
        public Color NormalColor;

        enum SnapEditorMode
        {
            addMode,
            subtractMode,
            invalid
        }

        private SnapEditorMode mode = SnapEditorMode.invalid;

        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            timelineBeatSnapSelector = NRDependencyInjector.Get<Timeline>().beatSnapSelector;
            chainBuilderIntervalSelector = chainbuilderWindow.pathBuilderInterval;
            pathbuilderIntervalSelector = NRDependencyInjector.Get<PathbuilderUI>().intervalSelector;
            ResetColor();
            NRSettings.OnLoad(LoadSavedSnaps);
            gameObject.SetActive(false);
        }
        private void LoadSavedSnaps()
        {
            timelineBeatSnapSelector.elements = NRSettings.config.snaps;
            chainbuilderWindow.pathBuilderInterval.elements = timelineBeatSnapSelector.elements;
            pathbuilderIntervalSelector.elements = timelineBeatSnapSelector.elements;
            UpdateIndex();
        }

        private void UpdateIndex()
        {
            var snaps = timelineBeatSnapSelector.elements;
            var foundIndex = 0;
            for (int i = 0; i < snaps.Count; i++)
            {
                var snap = snaps[i];
                int.TryParse(snap.Substring(2), out int parsed);
                if (EditorBeatSnap.BeatSnap == parsed)
                {
                    foundIndex = i;
                    break;
                }
            }
            timelineBeatSnapSelector.defaultIndex = foundIndex;
            timelineBeatSnapSelector.index = foundIndex;
        }

        public void OnSnapSet()
        {
            int snap = 0;
            bool success = int.TryParse(inputField.text, out snap);
            if (success)
            {

                if (timelineBeatSnapSelector.elements.Contains("1/" + snap))
                {
                    return;
                }

                //Timeline.Instance.SetSnap (snap);
                AdjustSnapArray(snap);
                mode = SnapEditorMode.subtractMode;
                SetConfirmButtonInfo();
                PresetScrollWindow.UpdateSnapList();
            }
            else
            {
                ColorBlock colors = inputField.colors;
                colors.normalColor = ErrorColor;
                inputField.colors = colors;
            }
        }

        public void OnSnapClicked()
        {
            if (KeybindManager.Global.Modifier.IsCtrlDown())           
                OpenWindow();
        }

        public void OpenWindow() => Show();

        public void CloseWindow() => Hide();

        public void OnSnapInputFieldChanged()
        {
            ResetColor();

            int snap = 0;
            bool success = int.TryParse(inputField.text, out snap);
            mode = SnapEditorMode.invalid;
            if (success)
            {
                if ((snap >= 1) && (snap <= 128))
                {
                    mode = SnapEditorMode.addMode;
                }

                if (timelineBeatSnapSelector.elements.Contains("1/" + snap))
                {
                    mode = SnapEditorMode.invalid;
                }
            }

            SetConfirmButtonInfo();
        }

        public void OnSnapInputFieldEntered()
        {
            ResetColor();
        }

        public void ResetColor()
        {
            ColorBlock colors = inputField.colors;
            colors.normalColor = NormalColor;
            inputField.colors = colors;
            confirmButton.interactable = true;
        }

        public void SetConfirmButtonInfo()
        {
            if (mode == SnapEditorMode.addMode)
            {
                confirmButton.interactable = true;
            }
            else
            {
                confirmButton.interactable = false;
            }
        }

        private void AdjustSnapArray(int snap)
        {

            if (mode == SnapEditorMode.addMode)
            {
                timelineBeatSnapSelector.elements.Add("1/" + snap);
                timelineBeatSnapSelector.elements.Sort(delegate (string l, string r)
                {
                    return l.Substring(2).PadLeft(3, '0').CompareTo(r.Substring(2).PadLeft(3, '0'));
                });

            }
            else if (mode == SnapEditorMode.subtractMode)
            {
                RemoveSnap(snap);
            }

            chainBuilderIntervalSelector.elements = timelineBeatSnapSelector.elements;
            pathbuilderIntervalSelector.elements = timelineBeatSnapSelector.elements;
            NRSettings.config.snaps = timelineBeatSnapSelector.elements;
            NRSettings.SaveSettingsJson();
            UpdateIndex();
            EditorBeatSnap.UpdateSnapList();
        }

        public void RemoveSnap(int snap)
        {
            int indexseek = 0;
            int desiredIndex = -1;
            foreach (string element in timelineBeatSnapSelector.elements)
            {
                if (element == ("1/" + snap))
                {
                    desiredIndex = indexseek;
                    break;
                }
                ++indexseek;
            }

            if (desiredIndex != -1)
            {
                timelineBeatSnapSelector.elements.RemoveAt(desiredIndex);
                mode = SnapEditorMode.addMode;
                SetConfirmButtonInfo();
            }

            chainBuilderIntervalSelector.elements = timelineBeatSnapSelector.elements;
            pathbuilderIntervalSelector.elements = timelineBeatSnapSelector.elements;
            NRSettings.config.snaps = timelineBeatSnapSelector.elements;
            NRSettings.SaveSettingsJson();
            UpdateIndex();
            EditorBeatSnap.UpdateSnapList();
        }

        public List<string> deafultSnaps = new List<string>() {
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

        public void OnResetButton()
        {
            if (confirmationOfDestructiveActionRequired)
            {
                resetButton.GetComponentInChildren<TextMeshProUGUI>().text = "Click again to confirm\n(there is no way to undo this)";
                confirmationOfDestructiveActionRequired = false;
            }
            else
            {
                NRSettings.config.snaps = deafultSnaps;
                timelineBeatSnapSelector.elements = deafultSnaps;
                chainBuilderIntervalSelector.elements = deafultSnaps;
                pathbuilderIntervalSelector.elements = deafultSnaps;

                NRSettings.SaveSettingsJson();
                confirmationOfDestructiveActionRequired = true;
                CloseWindow();
            }
        }

        public override void Show()
        {
            OnActivated();
            timelineBeatSnapSelector.elements = NRSettings.config.snaps;
            confirmButton.interactable = true;
            PresetScrollWindow.Show();
        }

        public override void Hide()
        {
            inputField.GetComponent<TMP_InputField>().ReleaseSelection();
            ResetColor();
            PresetScrollWindow.Hide();
            OnDeactivated();
        }

        public override void ShowHelp() { }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }
    }

}