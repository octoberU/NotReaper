using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using TMPro;
using NotReaper.Targets;
using System.Linq;
using DG.Tweening;
using NotReaper.Timing;
using UnityEngine.UI;
using NotReaper.Overlays;
using NotReaper.Models;
using NotReaper.UI.Components;
using NotReaper.UI;
using NotReaper.UserInput;

namespace NotReaper.Tools.PathBuilder
{
    public class PathbuilderUI : NROverlay
    {
        [Header("References")]
        [SerializeField] private GameObject window;
        [SerializeField] private GameObject advancedControls;
        [SerializeField] private GameObject noSelectionControls;
        [Space, Header("Interval")]
        [SerializeField] internal HorizontalSelector intervalSelector;
        [SerializeField] private NRInputField nominatorInput;
        [SerializeField] private TextMeshProUGUI denominatorText;
        [SerializeField] private NRButton scopeButton;
        [Space, Header("Beatlength")]
        [SerializeField] private TextMeshProUGUI beatLengthText;
        [Space, Header("Hand")]
        [SerializeField] private NRButton handButton;
        [Space, Header("Silent Chain")]
        [SerializeField] private NRToggle silentChainToggle;

        [NRInject] private Pathbuilder pathbuilder;
        [NRInject] private MappingInput input;

        Vector3 defaultPos = new Vector3(292.77f, -93.5f, -10f);

        private bool hasLoadedData = false;
        internal bool isOpen => gameObject.activeInHierarchy;

        private void Awake()
        {
            rect = window.GetComponent<RectTransform>();
            rect.localPosition = defaultPos;
            nominatorInput.inputField.onSelect.AddListener(OnInputFocused);
            nominatorInput.inputField.onDeselect.AddListener(OnInputFocusLost);
        }

        private void OnInputFocused(string _)
            => KeybindManager.DisableMap(KeybindManager.Map.BehaviorSelect);

        private void OnInputFocusLost(string _)
            => KeybindManager.EnableMap(KeybindManager.Map.BehaviorSelect);

        public override void Show()
        {
            OnActivated();
            intervalSelector.elements = NRSettings.config.snaps;
            ActivateWindow();
        }

        public override void Hide()
        {
            hasLoadedData = false;
            ActivateWindow();
            OnDeactivated();
        }

        public override void ShowHelp()
            => NRHelp.Instance.ShowPathbuilder();

        private void ActivateWindow()
            => ShowControls();

        public void OnIntervalChanged(bool next)
        {
            if (next) intervalSelector.ForwardClick();
            else intervalSelector.PreviousClick();

            pathbuilder.OnDenominatorChanged(GetInterval());
        }

        public void OnBeatlengthChanged(bool increase)
            => pathbuilder.OnBeatlengthChanged(increase);

        public void OnNominatorChanged()
            => pathbuilder.OnNominatorChanged(GetCustomNominator());

        public void OnScopeChanged()
            => pathbuilder.ChangeScope();

        public void OnHandChanged() 
            => pathbuilder.ChangeAlternateHands();

        public void OnBakePressed() 
            => pathbuilder.BakeActiveTarget();

        public void OnSilentChainToggled() 
            => pathbuilder.ToggleSilentChain();


        public void OnCloseClicked() 
            => EditorState.SelectTool(EditorTool.Pathbuilder);


        public void OnLegacyClicked()
        {
            EditorState.SelectTool(EditorTool.Pathbuilder);
            input.ToggleChainbuilder();
        }


        internal void LoadData(PathbuilderData.Interval interval, QNT_Duration beatLength, bool isSegmentScope, bool alternateHands, bool isSilent)
        {
            hasLoadedData = true;
            SetCustomNominator(interval.nominator);
            SetSelectorToDenominator(interval.denominator);
            SetBeatlength(beatLength.tick);
            SetScopeButtonText(isSegmentScope);
            SetHandButtonText(alternateHands);
            SetSilentChainToggle(isSilent);
            ShowControls();
        }

        internal void ResetPanel()
        {
            hasLoadedData = false;
            SetSelectorToDenominator(4);
            SetCustomNominator(1);
            SetDenominatorText(4);
            SetBeatlength(480);
            SetHandButtonText(false);
            SetScopeButtonText(true);
            SetSilentChainToggle(false);
            ShowControls();
        }

        private void SetCustomNominator(object nominator)
            => nominatorInput.text = nominator.ToString();

        private void SetDenominatorText(object denominator)
            => denominatorText.text = denominator.ToString();

        private void SetBeatlength(object beatlength)
            => beatLengthText.text = beatlength.ToString();

        private void SetHandButtonText(bool alternate)
            => handButton.SetText(alternate ? "alternate" : "same");

        private void SetScopeButtonText(bool isSegmentScope)
            => scopeButton.SetText(isSegmentScope ? "segment" : "path");

        private void SetSilentChainToggle(bool isSilent)
            => silentChainToggle.selected = isSilent;

        private void SetSelectorToDenominator(object denominator)
        {
            string denominatorStr = denominator.ToString();
            for (int i = 0; i < intervalSelector.elements.Count; ++i)
            {
                var elementDenominator = ParseDenominator(intervalSelector.elements[i]);
                if (elementDenominator == denominatorStr)
                {
                    intervalSelector.defaultIndex = i;
                    intervalSelector.UpdateToIndex(i);
                    break;
                }
            }
            SetDenominatorText(denominator);
        }

        private void ShowControls()
        {
            advancedControls.SetActive(hasLoadedData);
            noSelectionControls.SetActive(!hasLoadedData);
        }

        private string ParseDenominator(string element)
            => element.Substring(element.LastIndexOf('/') + 1);

        private int GetCustomNominator()
        {
            int.TryParse(nominatorInput.text, out int result);
            return result;
        }

        private int GetInterval()
        {
            int.TryParse(ParseDenominator(intervalSelector.elements[intervalSelector.index]), out int result);
            return result;
        }

        [NRListener]
        protected override void OnEditorModeChanged(EditorMode mode)
        {
            if (!gameObject.activeInHierarchy) return;

            if (mode != EditorMode.Compose)
            {
                pathbuilder.Activate(false);
            }
        }
    }
}
