using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using TMPro;
using NotReaper.Targets;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Schema;
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
        [SerializeField] private GameObject advancedModeRoot;
        [SerializeField] private GameObject window;
        [SerializeField] private GameObject noSelectionControls;
        [SerializeField] private GameObject bakeButton;
        [SerializeField] private NRButton toggleModeButton;
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

        [Space(10), Header("Simple Mode")]
        [SerializeField] private GameObject simpleModeRoot;
        [SerializeField] internal HorizontalSelector simpleIntervalSelector;
        [SerializeField] private NRInputSliderCombo angleSlider;
        [SerializeField] private NRInputSliderCombo angleIncrementSlider;
        [SerializeField] private NRInputSliderCombo stepDistanceSlider;
        [SerializeField] private NRInputSliderCombo stepIncrementSlider;
        [SerializeField] private TextMeshProUGUI simpleBeatLength;

        [NRInject] private Pathbuilder pathbuilder;
        [NRInject] private MappingInput input;

        Vector3 defaultPos = new Vector3(292.77f, -93.5f, -10f);

        private bool hasLoadedData = false;
        internal bool isOpen => gameObject.activeInHierarchy;

        private PathbuilderMode currentMode = PathbuilderMode.Advanced;

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

        #region Simple Mode
        public void ToggleMode()
        {
            currentMode = currentMode == PathbuilderMode.Simple ? PathbuilderMode.Advanced : PathbuilderMode.Simple;
            bool isSimple = currentMode == PathbuilderMode.Simple;
            simpleModeRoot.SetActive(isSimple);
            advancedModeRoot.SetActive(isSimple);
            pathbuilder.SetMode(currentMode);
            toggleModeButton.SetText(currentMode.ToString());
        }
        
        public void OnSimpleIntervalChanged(bool next)
        {
            if(next) simpleIntervalSelector.ForwardClick();
            else simpleIntervalSelector.PreviousClick();
            
            pathbuilder.OnSimpleDenominatorChanged(GetInterval());
        }

        public void OnAngleChanged(float angle) => pathbuilder.OnSimpleAngleChanged(angle);
        public void OnAngleIncrementChanged(float increment) => pathbuilder.OnSimpleAngleIncrementChanged(increment);

        public void OnStepDistanceChanged(float distance) => pathbuilder.OnSimpleStepDistanceChanged(distance);

        public void OnStepIncrementChanged(float increment) => pathbuilder.OnSimpleStepIncrementChanged(increment);
        #endregion
        public override void Show()
        {
            OnActivated();
            intervalSelector.elements = NRSettings.config.snaps;
            simpleIntervalSelector.elements = NRSettings.config.snaps;
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


        internal void LoadData(PathbuilderData.Interval interval, QNT_Duration beatLength, bool isSegmentScope, bool alternateHands, bool isSilent, PathbuilderMode mode, PathbuilderData.SimpleModeData simpleData)
        {
            currentMode = mode;
            hasLoadedData = true;
            SetCustomNominator(interval.nominator);
            SetSelectorToDenominator(interval.denominator, intervalSelector);
            SetSelectorToDenominator(simpleData.interval, simpleIntervalSelector);
            SetBeatlength(beatLength.tick);
            SetScopeButtonText(isSegmentScope);
            SetHandButtonText(alternateHands);
            SetSilentChainToggle(isSilent);
            LoadSimpleData(simpleData);
            ShowControls();
            toggleModeButton.SetText(currentMode.ToString());
        }

        private void LoadSimpleData(PathbuilderData.SimpleModeData simpleData)
        {
            angleSlider.SetValueWithoutNotify(simpleData.angle);
            angleIncrementSlider.SetValueWithoutNotify(simpleData.angleIncrement);
            stepDistanceSlider.SetValueWithoutNotify(simpleData.stepDistance);
            stepIncrementSlider.SetValueWithoutNotify(simpleData.stepIncrement);
            SetSimpleBeatlength(simpleData.beatLength);
        }

        internal void ResetPanel()
        {
            hasLoadedData = false;
            SetSelectorToDenominator(4, intervalSelector);
            SetSelectorToDenominator(4, simpleIntervalSelector);
            SetCustomNominator(1);
            SetDenominatorText(4);
            SetBeatlength(480);
            SetSimpleBeatlength(480);
            SetHandButtonText(false);
            SetScopeButtonText(true);
            SetSilentChainToggle(false);
            ShowControls();
            toggleModeButton.SetText(currentMode.ToString());
        }

        private void SetCustomNominator(object nominator)
            => nominatorInput.text = nominator.ToString();

        private void SetDenominatorText(object denominator)
            => denominatorText.text = denominator.ToString();

        private void SetBeatlength(object beatlength)
            => beatLengthText.text = beatlength.ToString();
        
        private void SetSimpleBeatlength(object beatlength)
            => simpleBeatLength.text = beatlength.ToString();
        

        private void SetHandButtonText(bool alternate)
            => handButton.SetText(alternate ? "alternate" : "same");

        private void SetScopeButtonText(bool isSegmentScope)
            => scopeButton.SetText(isSegmentScope ? "segment" : "path");

        private void SetSilentChainToggle(bool isSilent)
            => silentChainToggle.selected = isSilent;

        private void SetSelectorToDenominator(object denominator, HorizontalSelector selector)
        {
            string denominatorStr = denominator.ToString();
            for (int i = 0; i < selector.elements.Count; ++i)
            {
                var elementDenominator = ParseDenominator(selector.elements[i]);
                if (elementDenominator == denominatorStr)
                {
                    selector.defaultIndex = i;
                    selector.UpdateToIndex(i);
                    break;
                }
            }
            SetDenominatorText(denominator);
        }

        private void ShowControls()
        {
            noSelectionControls.SetActive(!hasLoadedData);
            advancedModeRoot.SetActive(currentMode == PathbuilderMode.Advanced && hasLoadedData);
            simpleModeRoot.SetActive(currentMode == PathbuilderMode.Simple && hasLoadedData);
            bakeButton.SetActive(hasLoadedData);
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
            var selector = currentMode == PathbuilderMode.Advanced ? intervalSelector : simpleIntervalSelector;
            int.TryParse(ParseDenominator(selector.elements[selector.index]), out int result);
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
