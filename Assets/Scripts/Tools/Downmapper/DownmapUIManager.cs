using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NotReaper.Managers;

public class DownmapUIManager : MonoBehaviour
{
    public static DownmapUIManager Instance = null;
    private DownmapConfig config;

    #region References
    #region General
    [Header("General")]
    [SerializeField] private GameObject window;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI loadedSettingsText;
    #endregion
    #region Streams
    [Space, Header("Streams")]
    [SerializeField] private Toggle toggleStreamEnable;
    [SerializeField] private Toggle toggleStream2Chain;
    [SerializeField] private TMP_InputField inputStreamSpeed;
    [SerializeField] private TMP_InputField inputStreamMaxTargets;
    #endregion
    #region Slots
    [Space, Header("Slots")]
    [SerializeField] private Toggle toggleSlotEnable;
    [SerializeField] private Toggle toggleSlotConvert;
    [SerializeField] private TMP_InputField inputSlotLeadinHorizontal;
    [SerializeField] private TMP_InputField inputSlotLeadinVertical;
    #endregion
    #region Sustains
    [Space, Header("Sustains")]
    [SerializeField] private Toggle toggleSustainEnable;
    [SerializeField] private TMP_InputField inputSustainLeadinTime;
    [SerializeField] private TMP_InputField inputSustainPauseSameHand;
    #endregion
    #region Chains
    [Space, Header("Chains")]
    [SerializeField] private Toggle toggleChainEnable;
    [SerializeField] private Toggle toggleChainRemoveTargets;
    [SerializeField] private Toggle toggleChainConvert;
    [SerializeField] private TMP_InputField inputChainLeadinTime;
    [SerializeField] private TMP_InputField inputChainPauseSameHand;
    [SerializeField] private TMP_InputField inputChainPauseOtherHand;
    #endregion
    #region Melees
    [Space, Header("Melees")]
    [SerializeField] private Toggle toggleMeleeEnable;
    [SerializeField] private Toggle toggleMeleeDelete;
    [SerializeField] private TMP_InputField inputMeleeLeadin;
    [SerializeField] private TMP_InputField inputMeleePause;
    #endregion
    #region Single Target Spacing
    [Space, Header("Single Target Spacing")]
    [SerializeField] private Toggle toggleSingleEnable;
    [SerializeField] private TMP_InputField inputSingleHalfNote;
    [SerializeField] private TMP_InputField inputSingleQuarterNote;
    [SerializeField] private TMP_InputField inputSingleEigthNote;
    [SerializeField] private TMP_InputField inputSingleSixteenthNote;
    #endregion
    #region Doubles
    [Space, Header("Doubles")]
    [SerializeField] private Toggle toggleDoubleEnable;
    [SerializeField] private Toggle toggleDoubleUncross;
    [SerializeField] private Toggle toggleHitsoundsOverBeat;
    [SerializeField] private TMP_InputField inputDoubleDistance;
    [SerializeField] private TMP_InputField inputDoubleLeadinTime;
    #endregion
    #endregion


    private void Awake()
    {
        if (Instance is null) Instance = this;
        else
        {
            Debug.LogWarning("DownmapUIManager already exists.");
            return;
        }
        
    }
    private void Start()
    {
        window.SetActive(false);
        config = DownmapConfig.Instance;
    }

    public void ApplyValues()
    {
        DownmapConfig.DownmapPrefrences prefs = DownmapConfig.Instance.Preferences;
        //Streams
        toggleStreamEnable.isOn = prefs.Streams.enabled;
        toggleStream2Chain.isOn = prefs.Streams.stream2Chain;
        inputStreamMaxTargets.text = prefs.Streams.maxConsecutiveTargets.ToString();
        inputStreamSpeed.text = prefs.Streams.maxStreamSpeed.ToString();

        //Slots
        toggleSlotEnable.isOn = prefs.Slots.enabled;
        toggleSlotConvert.isOn = prefs.Slots.convert;
        inputSlotLeadinHorizontal.text = prefs.Slots.leadinHorizontal.ToString();
        inputSlotLeadinVertical.text = prefs.Slots.leadinVertical.ToString();

        //Sustains
        toggleSustainEnable.isOn = prefs.Sustains.enabled;
        inputSustainLeadinTime.text = prefs.Sustains.leadinTime.ToString();
        inputSustainPauseSameHand.text = prefs.Sustains.pauseAfter.ToString();

        //Chains
        toggleChainEnable.isOn = prefs.Chains.enabled;
        toggleChainRemoveTargets.isOn = prefs.Chains.isolate;
        toggleChainConvert.isOn = prefs.Chains.convert;
        inputChainLeadinTime.text = prefs.Chains.leadinTime.ToString();
        inputChainPauseSameHand.text = prefs.Chains.pauseSameHand.ToString();
        inputChainPauseOtherHand.text = prefs.Chains.pauseOtherHand.ToString();

        //Melees
        toggleMeleeEnable.isOn = prefs.Melees.enabled;
        toggleMeleeDelete.isOn = prefs.Melees.deleteAll;
        inputMeleeLeadin.text = prefs.Melees.leadinTime.ToString();
        inputMeleePause.text = prefs.Melees.pauseTime.ToString();

        //Single Target Spacing
        toggleSingleEnable.isOn = prefs.SingleTargetSpacing.enabled;
        inputSingleHalfNote.text = prefs.SingleTargetSpacing.halfNote.ToString();
        inputSingleQuarterNote.text = prefs.SingleTargetSpacing.quarterNote.ToString();
        inputSingleEigthNote.text = prefs.SingleTargetSpacing.eighthNote.ToString();
        inputSingleSixteenthNote.text = prefs.SingleTargetSpacing.sixteenthNote.ToString();

        //Doubles
        toggleDoubleEnable.isOn = prefs.Doubles.enabled;
        toggleDoubleUncross.isOn = prefs.Doubles.uncross;
        toggleHitsoundsOverBeat.isOn = prefs.Doubles.hitsoundsOverBeat;
        inputDoubleDistance.text = prefs.Doubles.maxDistance.ToString();
        inputDoubleLeadinTime.text = prefs.Doubles.leadinTime.ToString();
    }

    #region OnClick Methods
    #region General
    public void ShowWindow(bool show)
    {
        if (show)
        {
            switch (DifficultyManager.I.loadedIndex)
            {
                case 0:
                    return;
                case 1:
                    difficultyText.text = $"<color=orange>Advanced";
                    break;
                case 2:
                    difficultyText.text = $"<color=lightblue>Standard";
                    break;
                case 3:
                    difficultyText.text = $"<color=green>Beginner";
                    break;
            }
            OnLoadCustomClicked();
        }
        window.SetActive(show);
        window.transform.position = show ? new Vector3(0f, 0f, 0f) : new Vector3(-3700f, 0f, 0f);

    }
    public void OnDownmapClicked()
    {
        Downmapper.Instance.Downmap();
        ShowWindow(false);
    }
    public void OnLoadDefaultsClicked()
    {
        
        SetLoadedSettings(!config.SetDefaultValues(DifficultyManager.I.loadedIndex));
        ApplyValues();
    }
    public void OnLoadCustomClicked()
    {
        SetLoadedSettings(config.LoadCustomValues(DifficultyManager.I.loadedIndex));
        ApplyValues();
    }
    public void OnSaveCustomClicked()
    {
        config.SaveCustomValues(DifficultyManager.I.loadedIndex);
    }
    public void SetLoadedSettings(bool custom)
    {
        if (custom)
        {
            loadedSettingsText.text = "Custom Settings loaded";
        }
        else
        {
            loadedSettingsText.text = "Default Settings loaded";
        }
    }
    public void OnEnableToggled(int index)
    {
        DownmapFunction function = (DownmapFunction)index;
        switch (function)
        {
            case DownmapFunction.Streams:
                config.Preferences.Streams.enabled = toggleStreamEnable.isOn;
                break;
            case DownmapFunction.Slots:
                config.Preferences.Slots.enabled = toggleSlotEnable.isOn;
                break;
            case DownmapFunction.Sustains:
                config.Preferences.Sustains.enabled = toggleSustainEnable.isOn;
                break;
            case DownmapFunction.Chains:
                config.Preferences.Chains.enabled = toggleChainEnable.isOn;
                break;
            case DownmapFunction.Melees:
                config.Preferences.Melees.enabled = toggleMeleeEnable.isOn;
                break;
            case DownmapFunction.SingleTargetSpacing:
                config.Preferences.SingleTargetSpacing.enabled = toggleSingleEnable.isOn;
                break;
            case DownmapFunction.Doubles:
                config.Preferences.Doubles.enabled = toggleDoubleEnable.isOn;
                break;
            default:
                break;

        }
    }
    #endregion
    #region Streams
    public void StreamMaxSpeedChanged()
    {
        if(uint.TryParse(inputStreamSpeed.text, out uint result))
        {
            config.Preferences.Streams.maxStreamSpeed = result;
        }
        else
        {
            string text = inputStreamSpeed.text;
            if (text.Length > 0) inputStreamSpeed.text = text.Substring(0, text.Length - 1);
        }
    }
    public void StreamMaxTargetsChanged()
    {
        if (int.TryParse(inputStreamMaxTargets.text, out int result))
        {
            if(result < 0)
            {
                result *= -1;
                inputStreamMaxTargets.text = result.ToString();
            }
            config.Preferences.Streams.maxConsecutiveTargets = result;
        }
        else
        {
            string text = inputStreamSpeed.text;
            if (text.Length > 0) inputStreamSpeed.text = text.Substring(0, text.Length - 1);
        }
    }
    public void Stream2ChainToggled()
    {
        config.Preferences.Streams.stream2Chain = toggleStream2Chain.isOn;
    }
    #endregion
    #region Slots
    public void SlotConvertToggled()
    {
        config.Preferences.Slots.convert = toggleSlotConvert.isOn;
    }
    public void SlotLeadinHorizontalChanged()
    {
        if (uint.TryParse(inputSlotLeadinHorizontal.text, out uint result))
        {
            config.Preferences.Slots.leadinHorizontal = result;
        }
        else
        {
            string text = inputSlotLeadinHorizontal.text;
            if (text.Length > 0) inputSlotLeadinHorizontal.text = text.Substring(0, text.Length - 1);
        }
    }
    public void SlotLeadinVerticalChanged()
    {
        if (uint.TryParse(inputSlotLeadinVertical.text, out uint result))
        {
            config.Preferences.Slots.leadinVertical = result;
        }
        else
        {
            string text = inputSlotLeadinVertical.text;
            if (text.Length > 0) inputSlotLeadinVertical.text = text.Substring(0, text.Length - 1);
        }
    }
    #endregion
    #region Sustains
    public void SustainLeadinChanged()
    {
        if (uint.TryParse(inputSustainLeadinTime.text, out uint result))
        {
            config.Preferences.Sustains.leadinTime = result;
        }
        else
        {
            string text = inputSustainLeadinTime.text;
            if (text.Length > 0) inputSustainLeadinTime.text = text.Substring(0, text.Length - 1);
        }
    }
    public void SustainPauseSameHandChanged()
    {
        if (uint.TryParse(inputSustainPauseSameHand.text, out uint result))
        {
            config.Preferences.Sustains.pauseAfter = result;
        }
        else
        {
            string text = inputSustainPauseSameHand.text;
            if (text.Length > 0) inputSustainPauseSameHand.text = text.Substring(0, text.Length - 1);
        }
    }
    #endregion
    #region Chains
    public void ChainIsolateToggled()
    {
        config.Preferences.Chains.isolate = toggleChainRemoveTargets.isOn;
    }
    public void ChainConvertToggled()
    {
        config.Preferences.Chains.convert = toggleChainConvert.isOn;
    }
    public void ChainLeadinChanged()
    {
        if (uint.TryParse(inputChainLeadinTime.text, out uint result))
        {
            config.Preferences.Chains.leadinTime = result;
        }
        else
        {
            string text = inputChainLeadinTime.text;
            if (text.Length > 0) inputChainLeadinTime.text = text.Substring(0, text.Length - 1);
        }
    }
    public void ChainPauseSameHandChanged()
    {
        if (uint.TryParse(inputChainPauseSameHand.text, out uint result))
        {
            config.Preferences.Chains.pauseSameHand = result;
        }
        else
        {
            string text = inputChainPauseSameHand.text;
            if (text.Length > 0) inputChainPauseSameHand.text = text.Substring(0, text.Length - 1);
        }
    }
    public void ChainPauseSameOtherChanged()
    {
        if (uint.TryParse(inputChainPauseOtherHand.text, out uint result))
        {
            config.Preferences.Chains.pauseOtherHand = result;
        }
        else
        {
            string text = inputChainPauseOtherHand.text;
            if (text.Length > 0) inputChainPauseOtherHand.text = text.Substring(0, text.Length - 1);
        }
    }
    #endregion
    #region Melees
    public void MeleeDeleteToggled()
    {
        config.Preferences.Melees.deleteAll = toggleMeleeDelete.isOn;
    }
    public void MeleeLeadinChanged()
    {
        if (uint.TryParse(inputMeleeLeadin.text, out uint result))
        {
            config.Preferences.Melees.leadinTime = result;
        }
        else
        {
            string text = inputMeleeLeadin.text;
            if (text.Length > 0) inputMeleeLeadin.text = text.Substring(0, text.Length - 1);
        }
    }
    public void MeleePauseChanged()
    {
        if (uint.TryParse(inputMeleePause.text, out uint result))
        {
            config.Preferences.Melees.pauseTime = result;
        }
        else
        {
            string text = inputMeleePause.text;
            if(text.Length > 0) inputMeleePause.text = text.Substring(0, text.Length - 1);

        }
    }
    #endregion
    #region Single Target Spacing
    public void SingleTargetHalfChanged()
    {
        if (float.TryParse(inputSingleHalfNote.text, out float result))
        {
            if (result < 0)
            {
                result *= -1;
                inputSingleHalfNote.text = result.ToString();
            }

            config.Preferences.SingleTargetSpacing.halfNote = result;
        }
        else
        {
            string text = inputSingleHalfNote.text;
            if (text.Length > 0) inputSingleHalfNote.text = text.Substring(0, text.Length - 1);
        }
    }
    public void SingleTargetQuarterChanged()
    {
        if (float.TryParse(inputSingleQuarterNote.text, out float result))
        {
            if (result < 0)
            {
                result *= -1;
                inputSingleQuarterNote.text = result.ToString();
            }
            config.Preferences.SingleTargetSpacing.quarterNote = result;
        }
        else
        {
            string text = inputSingleQuarterNote.text;
            if (text.Length > 0) inputSingleQuarterNote.text = text.Substring(0, text.Length - 1);
        }
    }
    public void SingleTargetEighthChanged()
    {
        if (float.TryParse(inputSingleEigthNote.text, out float result))
        {
            if (result < 0)
            {
                result *= -1;
                inputSingleEigthNote.text = result.ToString();
            }
            config.Preferences.SingleTargetSpacing.eighthNote = result;
        }
        else
        {
            string text = inputSingleEigthNote.text;
            if (text.Length > 0) inputSingleEigthNote.text = text.Substring(0, text.Length - 1);
        }
    }
    public void SingleTargetSixteenthChanged()
    {
        if (float.TryParse(inputSingleSixteenthNote.text, out float result))
        {
            if (result < 0)
            {
                result *= -1;
                inputSingleSixteenthNote.text = result.ToString();
            }
            config.Preferences.SingleTargetSpacing.sixteenthNote = result;
        }
        else
        {
            string text = inputSingleSixteenthNote.text;
            if (text.Length > 0) inputSingleSixteenthNote.text = text.Substring(0, text.Length - 1);
        }
    }
    #endregion
    #region Doubles
    public void DoubleUncrossToggled()
    {
        config.Preferences.Doubles.uncross = toggleDoubleUncross.isOn;
    }
    public void DoubleMaxDistanceChanged()
    {
        if (float.TryParse(inputDoubleDistance.text, out float result))
        {
            if (result < 0)
            {
                result *= -1;
                inputDoubleDistance.text = result.ToString();
            }
            config.Preferences.Doubles.maxDistance = result;
        }
        else
        {
            string text = inputDoubleDistance.text;
            if (text.Length > 0) inputDoubleDistance.text = text.Substring(0, text.Length - 1);
        }
    }
    public void DoubleLeadinTimeChanged()
    {
        if(ulong.TryParse(inputDoubleLeadinTime.text, out ulong result))
        {
            config.Preferences.Doubles.leadinTime = result;
        }
        else
        {
            string text = inputDoubleLeadinTime.text;
            if (text.Length > 0) inputDoubleLeadinTime.text = text.Substring(0, text.Length - 1);
        }
    }
    public void DoubleHitsoundsOverBeatToggled()
    {
        config.Preferences.Doubles.hitsoundsOverBeat = toggleHitsoundsOverBeat.isOn;
    }
    #endregion
    #endregion
    public enum DownmapFunction
    {
        Streams = 0,
        Slots = 1,
        Sustains = 2,
        Chains = 3,
        Melees = 4,
        SingleTargetSpacing = 5,
        Doubles = 6
    }
}
