﻿using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NotReaper;
using NotReaper.UI;
using NotReaper.Timing;
using NotReaper.Targets;
using System;
using NotReaper.Modifier;
using NotReaper.UserInput;

namespace NotReaper.Modifier
{
    public class ModifierHandler : MonoBehaviour
    {
        public static ModifierHandler Instance = null;

        public static bool activated;
        [HideInInspector] public bool isHovering;

        [Header("References")]
        [SerializeField] private GameObject modifierWindow;
        [SerializeField] private TMP_Dropdown dropdown;
        [SerializeField] private GameObject amountSlider;
        [SerializeField] private GameObject colorPicker;
        [SerializeField] private GameObject value1;
        [SerializeField] private GameObject value2;
        [SerializeField] private GameObject option1;
        [SerializeField] private GameObject option2;
        [SerializeField] private GameObject startTickButton;
        [SerializeField] private GameObject endTickButton;
        [SerializeField] private GameObject createModifierButton;

        public List<ModifierData> modifierData = new List<ModifierData>();
        public List<Modifier> modifiers = new List<Modifier>();
        private ModifierData currentData;


        private LabelSetter slider;
        private TimelineEntry selectedModifier;
        private Vector3 activatePosition = new Vector3(292.1306f, -54.84449f, 1f);
        private bool init = false;
        private bool skipRefresh = false;
        private bool isHidden = false;

        public void Start()
        {
            if (Instance is null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError("Trying to create another Modifier instance.");
                return;
            }

            currentData = new ModifierData();
            currentData.type = ModifierType.AimAssist;
            currentData.shorthand = "AA";
            value1.SetActive(false);
            value2.SetActive(false);
            option1.SetActive(false);
            option2.SetActive(false);
            colorPicker.SetActive(false);
            slider = amountSlider.GetComponent<LabelSetter>();
        }

        public void OnButtonClicked()
        {
            EditorInput.I.SelectTool(EditorTool.ModifierCreator);
        }

        public void HideWindow(bool hide)
        {
            isHidden = hide;
            modifierWindow.SetActive(!hide);
        }

        public void Activate(bool activate)
        {
            if (!init && !activate) return;
            activated = activate;
            if (activate)
            {
                if (MiniTimeline.Instance != null)
                {
                    //Timeline.instance.SetScale(20);
                    ModifierTimeline.Instance.ShowModifiers(true);
                    Timeline.OptimizeInvisibleTargets();
                }
                modifierWindow.GetComponent<CanvasGroup>().DOFade(1.0f, 0.3f);
                modifierWindow.SetActive(isHidden ? false : true);
                modifierWindow.transform.localPosition = activatePosition;
                if (!init)
                {
                    init = true;
                }
            }
            else
            {
                if (MiniTimeline.Instance != null)
                {
                    ModifierTimeline.Instance.ShowModifiers(false);
                    Timeline.OptimizeInvisibleTargets();
                }
                modifierWindow.GetComponent<CanvasGroup>().DOFade(0.0f, 0.3f);
                if (!init)
                {
                    init = true;
                }
                else
                {
                    activatePosition = modifierWindow.transform.localPosition;
                }

                //modifierWindow.transform.localPosition = new Vector3(-2700, 0f, 0f);
                modifierWindow.SetActive(false);
            }
        }

        public IEnumerator ILoadModifiers()
        {
            yield return new WaitForSecondsRealtime(.1f);
            foreach (Modifier m in Timeline.audicaFile.modifiers.modifiers) LoadModifier(m);
        }

        public void LoadModifier(Modifier modifier)
        {
            ModifierType type;
            Enum.TryParse(modifier.type, true, out type);

            currentData.amount = modifier.amount;
            currentData.startTick = new QNT_Timestamp((uint)modifier.startTick);
            currentData.endTick = new QNT_Timestamp((uint)modifier.endTick);
            currentData.type = type;
            currentData.startPosX = modifier.startPosX;
            currentData.endPosX = modifier.endPosX;
            SetShorthand(currentData.type);

            switch (type)
            {
                case ModifierType.ArenaBrightness:
                    ArenaBrightness ab = modifier as ArenaBrightness;
                    currentData.option1 = ab.continuous;
                    currentData.option2 = ab.strobo;
                    break;
                case ModifierType.ArenaRotation:
                    ArenaRotation ar = modifier as ArenaRotation;
                    currentData.option1 = ar.continuous;
                    break;
                case ModifierType.ColorChange:
                    ColorChange cc = modifier as ColorChange;
                    currentData.leftHandColor = cc.leftHandColor;
                    currentData.rightHandColor = cc.rightHandColor;
                    break;
                case ModifierType.ColorUpdate:
                    ColorUpdate cu = modifier as ColorUpdate;
                    currentData.leftHandColor = cu.leftHandColor;
                    currentData.rightHandColor = cu.rightHandColor;
                    break;
                case ModifierType.zOffset:
                    ZOffset zo = modifier as ZOffset;
                    currentData.value1 = zo.transitionNumberOfTargets.ToString();
                    break;
                case ModifierType.ArenaChange:
                    ArenaChange ac = modifier as ArenaChange;
                    currentData.value1 = ac.arena1;
                    currentData.value2 = ac.arena2;
                    currentData.option1 = ac.preload;
                    break;
                default:
                    break;
            }
            SetStartTick(new QNT_Timestamp((ulong)modifier.startTick));
            SetEndTick(new QNT_Timestamp((ulong)modifier.endTick));
            CreateLoadedModifier();
        }

        private void CreateLoadedModifier()
        {
            Modifier m = CreateModifierType();
            m.startPosX = currentData.startPosX;
            m.endPosX = currentData.endPosX;
            currentData.modifier = m;
            modifiers.Add(m);
            modifierData.Add(currentData);
            ModifierTimeline.Instance.CreateModifier(currentData, true);
            Timeline.audicaFile.modifiers.modifiers = modifiers;
            currentData = new ModifierData();
            OnDropdownValueChanged();
        }

        public void CreateModifier()
        {
            if (!currentData.startSet) return;
            if (!ModifierTimeline.Instance.CanCreateModifier(currentData.type, currentData.startTick)) return;
            modifierData.Add(currentData);
            Timeline.audicaFile.modifiers.modifiers = modifiers;
            Modifier m = CreateModifierType();
            m.startPosX = ModifierTimeline.Instance.GetStartPosX();
            m.endPosX = ModifierTimeline.Instance.GetEndPosX();
            currentData.modifier = m;
            modifiers.Add(m);
            ModifierTimeline.Instance.CreateModifier(currentData);

            currentData = new ModifierData();
            if (selectedModifier is null)
            {
                OnDropdownValueChanged();
            }
            else
            {
                DeselectModifier(false, true);
            }
        }

        public bool IsDropdownExpanded()
        {
            return dropdown.IsExpanded;
        }

        public void SelectModifier(ModifierTimeline.ModifierContainer container, TimelineEntry entry)
        {
            if (entry == selectedModifier)
            {
                DeselectModifier();
                return;
            }
            else if (selectedModifier != null)
            {
                DeselectModifier(true);
            }
            modifiers.Remove(container.data.modifier);
            Timeline.audicaFile.modifiers.modifiers = modifiers;
            ModifierTimeline.Instance.SelectModifier(container);
            selectedModifier = entry;
            skipRefresh = true;
            dropdown.value = (int)container.data.type;


            entry.Select(true);
            amountSlider.GetComponent<LabelSetter>().SetSliderValue(container.data.amount);
            value1.GetComponent<LabelSetter>().SetInputText(container.data.value1);
            value2.GetComponent<LabelSetter>().SetInputText(container.data.value2);
            option1.GetComponent<LabelSetter>().SetToggleState(container.data.option1);
            option2.GetComponent<LabelSetter>().SetToggleState(container.data.option2);
            startTickButton.GetComponent<LabelSetter>().SetLabelText(container.data.startTick.tick.ToString());
            endTickButton.GetComponent<LabelSetter>().SetLabelText(container.data.endTick.tick.ToString());
            createModifierButton.GetComponent<LabelSetter>().SetLabelText("Update Modifier");
            colorPicker.GetComponent<LabelSetter>().SetColorSliderLeft(container.data.leftHandColor);
            colorPicker.GetComponent<LabelSetter>().SetColorSliderRight(container.data.rightHandColor);
            currentData = container.data;
        }

        public void DeselectModifier(bool swap = false, bool fromCreate = false)
        {
            if (selectedModifier is null) return;
            selectedModifier.Select(false);
            selectedModifier = null;
            currentData = new ModifierData();
            if (!fromCreate) ModifierTimeline.Instance.DropMarksSave();
            if (swap) return;
            OnDropdownValueChanged();
        }

        private Modifier CreateModifierType()
        {
            Modifier m = null;
            float st = currentData.startTick.tick;
            float et = currentData.endTick.tick;
            float amount = currentData.amount;
            string type = currentData.type.ToString();
            string val1 = currentData.value1;
            string val2 = currentData.value2;
            bool opt1 = currentData.option1;
            bool opt2 = currentData.option2;
            float[] colLeft = currentData.leftHandColor;
            float[] colRight = currentData.rightHandColor;

            switch (currentData.type)
            {
                case ModifierType.AimAssist:
                    m = new AimAssistChange(type, st, et, amount);
                    break;
                case ModifierType.ArenaBrightness:
                    m = new ArenaBrightness(type, st, et, amount, opt1, opt2);
                    break;
                case ModifierType.ArenaRotation:
                    m = new ArenaRotation(type, st, et, amount, opt1);
                    break;
                case ModifierType.ColorChange:
                    m = new ColorChange(type, st, et, colLeft, colRight);
                    break;
                case ModifierType.ColorSwap:
                    m = new ColorSwap(type, st, et);
                    break;
                case ModifierType.ColorUpdate:
                    m = new ColorUpdate(type, st, colLeft, colRight);
                    break;
                case ModifierType.Fader:
                    m = new Fader(type, st, et, amount);
                    break;
                case ModifierType.HiddenTelegraphs:
                    m = new HiddenTelegraphs(type, st, et);
                    break;
                case ModifierType.InvisibleGuns:
                    m = new InvisibleGuns(type, st, et);
                    break;
                case ModifierType.Particles:
                    m = new Particles(type, st, et, amount);
                    break;
                case ModifierType.Psychedelia:
                    m = new Psychedelia(type, st, et, amount);
                    break;
                case ModifierType.PsychedeliaUpdate:
                    m = new PsychedeliaUpdate(type, st, amount);
                    break;
                case ModifierType.Speed:
                    m = new SpeedChange(type, st, et, amount);
                    break;
                case ModifierType.zOffset:
                    float f = 0f;
                    float.TryParse(val1, out f);
                    m = new ZOffset(type, st, et, amount, f);
                    break;
                case ModifierType.ArenaChange:
                    m = new ArenaChange(type, st, et, val1, val2, opt1);
                    break;
            }
            return m;
        }

        private void SetStartTick(QNT_Timestamp tick)
        {
            currentData.startSet = true;
            ModifierTimeline.Instance.SetModifierMark(currentData.type, currentData.startTick, currentData.shorthand, true, currentData.startPosX, true);
        }
        private void SetEndTick(QNT_Timestamp tick)
        {
            SetEndTick(tick.tick);
        }

        public void SetStartTick()
        {
            ulong tick = Timeline.time.tick;
            if (tick != 0 && tick >= currentData.endTick.tick && currentData.endTick.tick != 0)
            {

                currentData.startTick = new QNT_Timestamp(tick);
                UpdateEndTick(tick);
                startTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                ModifierTimeline.Instance.UpdateMark(ModifierTimeline.UpdateType.UpdateStart, tick);
            }
            else
            {
                currentData.startTick = new QNT_Timestamp(tick);
                startTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                if (currentData.startSet && currentData.endTick != currentData.startTick)
                {
                    ModifierTimeline.Instance.UpdateMark(ModifierTimeline.UpdateType.MoveStart, tick);
                }
                else
                {
                    UpdateEndTick(tick);
                    ModifierTimeline.Instance.SetModifierMark(currentData.type, currentData.startTick, currentData.shorthand, true, currentData.startPosX, false);
                }

            }
            currentData.startSet = true;
        }

        private void UpdateEndTick(float tick)
        {
            if (!ModifierTimeline.Instance.endMarkExists && currentData.endTick.tick != 0)
            {
                endTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                currentData.endTick = currentData.startTick;
            }
        }

        public void SetEndTick(float loadTick = -1f)
        {
            if (!ModifierTimeline.Instance.startSet) return;
            ulong tick = Timeline.time.tick;
            if (loadTick != -1f) tick = (ulong)loadTick;
            if (tick != 0 && tick <= currentData.startTick.tick)
            {
                tick = currentData.startTick.tick;
                currentData.endTick = new QNT_Timestamp(tick);
                endTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                ModifierTimeline.Instance.UpdateMark(ModifierTimeline.UpdateType.UpdateEnd);
            }
            else if (tick == 0)
            {
                currentData.endTick = new QNT_Timestamp(tick);
                endTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                ModifierTimeline.Instance.UpdateMark(ModifierTimeline.UpdateType.UpdateEnd);
            }
            else
            {
                currentData.endTick = new QNT_Timestamp(tick);
                endTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                ModifierTimeline.Instance.SetModifierMark(currentData.type, currentData.endTick, currentData.shorthand, false, currentData.endPosX, loadTick != -1f);
            }
        }

        public void RemoveModifier(Modifier mod)
        {
            modifiers.Remove(mod);
            selectedModifier = null;
            OnDropdownValueChanged();
        }

        public void OnValue1Changed()
        {
            currentData.value1 = value1.GetComponent<LabelSetter>().GetText();
        }

        public void OnValue2Changed()
        {
            currentData.value2 = value2.GetComponent<LabelSetter>().GetText();
        }

        public void OnOption1Changed()
        {
            currentData.option1 = !currentData.option1;
        }

        public void OnOption2Changed()
        {
            currentData.option2 = !currentData.option2;
        }

        public void OnAmountChanged()
        {
            currentData.amount = amountSlider.GetComponentInChildren<Slider>().value;
        }

        public void OnLeftColorChanged()
        {
            currentData.leftHandColor = colorPicker.GetComponent<LabelSetter>().GetLeftColor();
        }

        public void OnRightColorChanged()
        {
            currentData.rightHandColor = colorPicker.GetComponent<LabelSetter>().GetRightColor();
        }

        public void DeleteModifier()
        {
            if (selectedModifier is null) OnDropdownValueChanged();
            else selectedModifier.DeleteModifier();
        }

        public void OnDropdownValueChanged()
        {
            if (!skipRefresh)
            {
                skipRefresh = false;
                if (selectedModifier is null)
                {
                    ModifierTimeline.Instance.DropMarks();
                }
                else
                {
                    Debug.Log("deselecting from dropdown");
                    DeselectModifier();
                }
            }


            ResetCurrentData();

            currentData.type = (ModifierType)dropdown.value;

            switch (currentData.type)
            {
                case ModifierType.AimAssist:
                case ModifierType.Fader:
                case ModifierType.Particles:
                case ModifierType.Psychedelia:
                case ModifierType.Speed:
                    amountSlider.SetActive(true);
                    endTickButton.SetActive(true);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    break;
                case ModifierType.ArenaBrightness:
                    amountSlider.SetActive(true);
                    endTickButton.SetActive(true);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    colorPicker.SetActive(false);
                    option1.GetComponentInChildren<LabelSetter>().SetLabelText("Continuous");
                    option2.GetComponentInChildren<LabelSetter>().SetLabelText("Strobo");
                    option1.SetActive(true);
                    option2.SetActive(true);
                    break;
                case ModifierType.ArenaRotation:
                    amountSlider.SetActive(true);
                    endTickButton.SetActive(true);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    option1.GetComponentInChildren<LabelSetter>().SetLabelText("Continuous");
                    option1.SetActive(true);
                    break;
                case ModifierType.ColorChange:
                    amountSlider.SetActive(false);
                    endTickButton.SetActive(true);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    colorPicker.SetActive(true);
                    break;
                case ModifierType.ColorUpdate:
                    amountSlider.SetActive(false);
                    endTickButton.SetActive(false);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    colorPicker.SetActive(true);
                    break;
                case ModifierType.PsychedeliaUpdate:
                    amountSlider.SetActive(true);
                    endTickButton.SetActive(false);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    break;
                case ModifierType.ColorSwap:
                case ModifierType.HiddenTelegraphs:
                case ModifierType.InvisibleGuns:
                    endTickButton.SetActive(true);
                    amountSlider.SetActive(false);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    break;
                case ModifierType.zOffset:
                    amountSlider.SetActive(true);
                    endTickButton.SetActive(true);
                    value2.SetActive(false);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    value1.GetComponent<LabelSetter>().SetLabelText("Transition Target Amount");
                    value1.SetActive(true);
                    colorPicker.SetActive(false);
                    break;
                case ModifierType.ArenaChange:
                    amountSlider.SetActive(false);
                    endTickButton.SetActive(false);
                    value1.GetComponent<LabelSetter>().SetLabelText("Arena Option 1");
                    value2.GetComponent<LabelSetter>().SetLabelText("Arena Option 2");
                    option1.GetComponent<LabelSetter>().SetLabelText("Preload");
                    value1.SetActive(true);
                    value2.SetActive(true);
                    option1.SetActive(true);
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    break;
            }
            SetMinMax(currentData.type);
            SetShorthand(currentData.type);
            createModifierButton.GetComponent<LabelSetter>().SetLabelText("Create Modifier");
        }

        private void SetShorthand(ModifierType type)
        {
            string sh = "";
            switch (type)
            {
                case ModifierType.AimAssist:
                    sh = "AA";
                    break;
                case ModifierType.ArenaBrightness:
                    sh = "AB";
                    break;
                case ModifierType.ArenaRotation:
                    sh = "AR";
                    break;
                case ModifierType.ColorChange:
                    sh = "CC";
                    break;
                case ModifierType.ColorSwap:
                    sh = "CS";
                    break;
                case ModifierType.ColorUpdate:
                    sh = "CU";
                    break;
                case ModifierType.Fader:
                    sh = "FA";
                    break;
                case ModifierType.HiddenTelegraphs:
                    sh = "HT";
                    break;
                case ModifierType.InvisibleGuns:
                    sh = "IG";
                    break;
                case ModifierType.Particles:
                    sh = "PA";
                    break;
                case ModifierType.Psychedelia:
                    sh = "PS";
                    break;
                case ModifierType.PsychedeliaUpdate:
                    sh = "PU";
                    break;
                case ModifierType.Speed:
                    sh = "SP";
                    break;
                case ModifierType.zOffset:
                    sh = "ZO";
                    break;
                case ModifierType.ArenaChange:
                    sh = "AC";
                    break;
                default:
                    break;
            }
            currentData.shorthand = sh;
        }

        private void SetMinMax(ModifierType type)
        {
            switch (type)
            {
                case ModifierType.AimAssist:
                case ModifierType.ArenaBrightness:
                    slider.SetMinValue(0f);
                    slider.SetMaxValue(100f);
                    break;
                case ModifierType.ArenaRotation:
                    slider.SetMinValue(0f);
                    slider.SetMaxValue(1000f);
                    break;
                case ModifierType.Fader:
                    slider.SetMinValue(0f);
                    slider.SetMaxValue(100f);
                    break;
                case ModifierType.Particles:
                    slider.SetMinValue(0f);
                    slider.SetMaxValue(1000f);
                    break;
                case ModifierType.Psychedelia:
                case ModifierType.PsychedeliaUpdate:
                    slider.SetMinValue(0f);
                    slider.SetMaxValue(10000f);
                    break;
                case ModifierType.Speed:
                    slider.SetMinValue(0f);
                    slider.SetMaxValue(200f);
                    break;
                case ModifierType.zOffset:
                    slider.SetMinValue(-100f);
                    slider.SetMaxValue(300f);
                    break;
                case ModifierType.ColorChange:
                case ModifierType.ColorUpdate:
                    colorPicker.GetComponent<LabelSetter>().SetMinMaxColorSliders(0f, 1f);
                    break;
                default:
                    break;
            }
        }

        private void ResetCurrentData()
        {
            currentData = new ModifierData();
            startTickButton.GetComponent<LabelSetter>().SetLabelText("0");
            endTickButton.GetComponent<LabelSetter>().SetLabelText("0");
            value1.GetComponent<LabelSetter>().SetLabelText("");
            value2.GetComponent<LabelSetter>().SetLabelText("");
            option1.GetComponent<LabelSetter>().SetToggleState(false);
            option2.GetComponent<LabelSetter>().SetToggleState(false);
            colorPicker.GetComponent<LabelSetter>().SetColorSliderLeft(new float[] { 0f, 0f, 0f });
            colorPicker.GetComponent<LabelSetter>().SetColorSliderRight(new float[] { 0f, 0f, 0f });
        }

        [Serializable]
        public struct ModifierData
        {
            public ModifierType type;
            public string shorthand;
            public QNT_Timestamp startTick;
            public QNT_Timestamp endTick;
            public float amount;
            public string value1;
            public string value2;
            public bool option1;
            public bool option2;
            public bool startSet;
            public Modifier modifier;
            public float startPosX;
            public float endPosX;
            public float[] leftHandColor;
            public float[] rightHandColor;
        }

        public enum ModifierType { AimAssist = 0, ColorChange = 1, ColorUpdate = 2, ColorSwap = 3, HiddenTelegraphs = 4, InvisibleGuns = 5, Particles = 6, Psychedelia = 7, PsychedeliaUpdate = 8, Speed = 9, zOffset = 10, ArenaRotation = 11, ArenaBrightness = 12, ArenaChange = 13, Fader = 14 }

    }
}


