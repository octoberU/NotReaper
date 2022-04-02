using DG.Tweening;
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
using System.Linq;
using NotReaper.Models;
using UnityEngine.InputSystem;
using NotReaper.UI.Components;

namespace NotReaper.Modifier
{
    public class ModifierHandler : NRInput<ModifierKeybinds>
    {
        public static ModifierHandler Instance = null;
        public static bool inputFocused = false;
        public static bool activated;
        public static bool isLoading = false;
        public static bool pendingClose = false;
        [HideInInspector] public bool isHovering;

        [Header("References")]
        [SerializeField] private ModifierSelectionHandler selectionHandler;
        [SerializeField] private ModifierPool modifierPool;
        [NRInject] private ModifierWindow modifierWindow;
        [SerializeField] private NRDropdown dropdown;
        [SerializeField] private GameObject amountSlider;
        [SerializeField] private GameObject colorPicker;
        [SerializeField] private GameObject sidePanel;
        [SerializeField] private GameObject value1;
        [SerializeField] private GameObject value2;
        [SerializeField] private GameObject value3;
        [SerializeField] private GameObject value4;
        [SerializeField] private GameObject value5;
        [SerializeField] private GameObject option1;
        [SerializeField] private GameObject option2;
        [SerializeField] private GameObject independantBool;
        [SerializeField] private GameObject startTickButton;
        [SerializeField] private GameObject endTickButton;
        [SerializeField] private GameObject createModifierButton;
        //[SerializeField] private GameObject modifierPrefab;
        [SerializeField] private Transform leftMax;
        [SerializeField] private Transform rightMax;
        [SerializeField] private TextMeshProUGUI modifierCount;
        [SerializeField] private GameObject trackerText;
        public List<Modifier> modifiers = new List<Modifier>();
        private Modifier currentModifier;


        private LabelSetter slider;
        private bool init = false;
        private bool skipRefresh = false;
        private bool isHidden = false;

        private static bool IsPrivateBuild = false;
        [NRInject] private UIToolSelect toolSelect;
        public bool isEditingManipulation => dropdown.value == 19 || dropdown.value == 20 || dropdown.value == 21;

        protected override void Awake()
        {
            base.Awake();

            if (Instance is null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError("Trying to create another Modifier instance.");
                return;
            }
            value1.SetActive(false);
            value2.SetActive(false);            
            option1.SetActive(false);
            option2.SetActive(false);
            independantBool.SetActive(false);
            colorPicker.GetComponent<LabelSetter>().InitializeColorFields();
            colorPicker.SetActive(false);
            DeactivateSidePanel();
            slider = amountSlider.GetComponent<LabelSetter>();

            if (!IsPrivateBuild)
            {
                dropdown.items.RemoveAt(20);
                dropdown.items.RemoveAt(20);
                dropdown.items.RemoveAt(20);
            }

            EditorScale.onScaleChanged += OnScaleChanged;
        }

        internal bool IsDropdownOpen() => dropdown.isExpanded;

        private void UpdateModifierCount()
        {
            modifierCount.text = $"Modifier Count: {modifiers.Count}";
        }
        public List<Modifier> GetZOffsetModifiers()
        {
            List<Modifier> list = new List<Modifier>();
            foreach (Modifier m in modifiers) if (m.modifierType == ModifierType.zOffset) list.Add(m);
            return list;
        }

        public void DropCurrentModifier()
        {
            if (currentModifier is null) return;
            if (!currentModifier.isCreated)
            {
                currentModifier.Delete();
               // return;
            }
            else
            {
                modifiers.Add(currentModifier);
            }
           
            currentModifier = null;
            modifiers.Sort((mod1, mod2) => mod1.startTime.tick.CompareTo(mod2.startTime.tick));
        }

        public void CleanUp()
        {
            for(int i = 0; i < modifiers.Count; i++)
            {
                modifiers[i].Delete();
            }
            modifiers.Clear();
            currentModifier = null;
            ModifierSelectionHandler.Instance.CleanUp();
            GameObject[] stubbornModifiers = GameObject.FindGameObjectsWithTag("Modifier");
            for(int i = 0; i < stubbornModifiers.Length; i++)
            {
                modifierPool.Return(stubbornModifiers[i].GetComponent<Modifier>());
                //GameObject.Destroy(stubbornModifiers[i]);
            }
        }

        public void OnInputFocusChange(string _)
        {
            inputFocused = !inputFocused;
        }

        public void OnButtonClicked()
        {
            if(EditorState.Tool.Current == EditorTool.ModifierCreator)
            {
                pendingClose = true;
                EditorState.SelectTool(EditorState.Tool.Previous);
                pendingClose = false;
            }
            else
            {
                EditorState.SelectTool(EditorTool.ModifierCreator);
            }
        }

        public void ToggleModifiers()
        {
            toolSelect.SetInteractable(activated);
            if (activated)
            {
                EditorState.SelectTool(EditorTool.ModifierCreator);
                Activate(false);
            }
            else
            {
                EditorState.SelectTool(EditorTool.ModifierCreator);
                Activate(true);
            }
        }


        public void HideWindow(bool hide)
        {
            if (!activated) return;

            isHidden = hide;
            if(hide) modifierWindow.Hide();
            else modifierWindow.Show();
            //modifierWindow.SetActive(!hide);
        }

        public void Activate(bool activate)
        {
            if (!init && !activate) return;
            activated = activate;
            if (activate)
            {
                OnActivated();
                if (MiniTimeline.Instance != null)
                {
                    ShowModifiers(true);
                    EditorNotes.ShowTimelineTargets(false);
                }
                if (isHidden) modifierWindow.Hide();
                else modifierWindow.Show();
                if (!init)
                {
                    OnDropdownValueChanged();
                    init = true;
                }
                ModifierType mType = (ModifierType)dropdown.value;
                if(mType == ModifierType.ColorChange || mType == ModifierType.ColorUpdate || mType == ModifierType.SkyboxColor)
                {
                    colorPicker.SetActive(true);
                }
            }
            else
            {
                OnDeactivated();
                if (MiniTimeline.Instance != null)
                {
                    ShowModifiers(false);
                    EditorNotes.ShowTimelineTargets(true);
                }
                if (!init)
                {
                    OnDropdownValueChanged();
                    init = true;
                }
                if(currentModifier != null)
                {
                    DropCurrentModifier();
                }
                colorPicker.SetActive(false);
                //modifierWindow.SetActive(false);
                modifierWindow.Hide();
            }
        }

        public void ShowModifiers(bool show)
        {
            foreach (Modifier m in modifiers) m.Show(show);
            modifierCount.gameObject.SetActive(show);
        }

        private bool isUpdatingLevels = false;
        public IEnumerator IUpdateLevels()
        {
            if (ModifierUndoRedo.undoRedoActive) yield break;
            while (isUpdatingLevels) yield return new WaitForSeconds(.2f);
            isUpdatingLevels = true;
            modifiers.Sort((mod1, mod2) => mod1.startTime.tick.CompareTo(mod2.startTime.tick));
            foreach (Modifier m in modifiers)
            {
                m.UpdateLevel();
                yield return null;
            }
            isUpdatingLevels = false;
        }

        public IEnumerator LoadModifiers(List<ModifierDTO> modList, bool fromLoad = false, bool fromAction = false)
        {
            if (currentModifier != null)
            {
                CreateModifier();
            }
            foreach (ModifierDTO dto in modList)
            {
                //Modifier m = Instantiate(modifierPrefab).GetComponent<Modifier>();
                Modifier m = modifierPool.Spawn();
                m.LoadFromDTO(dto);
                m.shorthand = GetShorthand(m.modifierType);
                LoadModifier(m);
            }
            ModifierSelectionHandler.isPasting = false;
            yield return new WaitForSeconds(.001f);
            ShowModifiers(activated);
            isLoading = false;
            modifiers.Sort((mod1, mod2) => mod1.startTime.tick.CompareTo(mod2.startTime.tick));
        }

        public void LoadModifier(Modifier modifier)
        {
            currentModifier = modifier;
            SetStartTick(modifier.startTime);
            SetEndTick(modifier.endTime);
            CreateModifier(false);
        }


        public List<ModifierDTO> MapToDTO()
        {
            Modifier current = null;
            if (currentModifier != null && currentModifier.isSelected) current = currentModifier;
            ModifierSelectionHandler.Instance.DeselectAllModifiers();
            DropCurrentModifier();
            List<ModifierDTO> dtoList = new List<ModifierDTO>();
            foreach(Modifier m in modifiers)
            {
                dtoList.Add(m.GetDTO());
            }
            if (current != null) ModifierSelectionHandler.Instance.SelectModifier(current, true);
            return dtoList;
            
        }

        public void CreateModifier(bool save = false)
        {
            if (currentModifier is null) return;
            if (!currentModifier.startSet) return;
            if (!CanCreateModifier(currentModifier.modifierType, currentModifier.startTime)) return;
            currentModifier.CreateModifier(save);
            modifiers.Add(currentModifier);
            List<Modifier> lmo = new List<Modifier>();
            lmo.Add(currentModifier);
            if (!save && !isLoading && !ModifierSelectionHandler.isPasting) ModifierUndoRedo.Instance.AddAction(lmo, Action.Create);
            if(ModifierUndoRedo.recreating) ModifierUndoRedo.Instance.recreatedModifiers.Add(currentModifier);
            if (ModifierSelectionHandler.isPasting) ModifierSelectionHandler.Instance.tempCopiedModifiers.Add(currentModifier);
            currentModifier = null;
            OnDropdownValueChanged();
            UpdateModifierCount();
            if(!isLoading) modifiers.Sort((mod1, mod2) => mod1.startTime.tick.CompareTo(mod2.startTime.tick));
        }

        public bool CanCreateModifier(ModifierType type, QNT_Timestamp tick)
        {
            if(type == ModifierType.Speed || type == ModifierType.Fader)
            {
                if (currentModifier.endTime.tick == 0) return false;
            }
            if (type != ModifierType.ColorUpdate && type != ModifierType.PsychedeliaUpdate) return true;
            foreach (Modifier m in modifiers)
            {
                if (m.startTime < tick && m.endTime > tick)
                {
                    if (m.modifierType == ModifierType.ColorChange && type == ModifierType.ColorUpdate)
                    {
                        return true;
                    }
                    else if (m.modifierType == ModifierType.Psychedelia && type == ModifierType.PsychedeliaUpdate) return true;
                }
                else if (m.endTime.tick == 0)
                {
                    if (m.modifierType == ModifierType.ColorChange && type == ModifierType.ColorUpdate) return true;
                    else if (m.modifierType == ModifierType.Psychedelia && type == ModifierType.PsychedeliaUpdate) return true;
                }
            }
            return false;
        }

        public bool fillingData = false;
        public void FillData(Modifier modifier, bool shouldFill, bool isEmpty)
        {
             if (!shouldFill || isEmpty)
             {
                if (currentModifier != null)
                {
                    CreateModifier(true);
                }
                else
                {
                    OnDropdownValueChanged();
                }                
                 return;
             }
            fillingData = true;
            int modType = (int)modifier.modifierType;
            skipRefresh = dropdown.value != modType;
            //currentModifier = modifier;
            modifiers.Remove(modifier);
            dropdown.value = modType;
            value1.GetComponent<LabelSetter>().SetInputText(modifier.value1);
            value2.GetComponent<LabelSetter>().SetInputText(modifier.value2);
            value3.GetComponent<LabelSetter>().SetInputText(modifier.xoffset);
            value4.GetComponent<LabelSetter>().SetInputText(modifier.yoffset);
            value5.GetComponent<LabelSetter>().SetInputText(modifier.zoffset);
            option1.GetComponent<LabelSetter>().SetToggleState(modifier.option1);
            option2.GetComponent<LabelSetter>().SetToggleState(modifier.option2);
            independantBool.GetComponent<LabelSetter>().SetToggleState(modifier.independantBool);
            startTickButton.GetComponent<LabelSetter>().SetLabelText(modifier.startTime.tick.ToString());
            endTickButton.GetComponent<LabelSetter>().SetLabelText(modifier.endTime.tick.ToString());
            createModifierButton.GetComponent<LabelSetter>().SetLabelText("Update Modifier");
            if (modifier.modifierType != ModifierType.SkyboxColor)
            {
                colorPicker.GetComponent<LabelSetter>().SetColorSliderLeft(modifier.leftHandColor);
                colorPicker.GetComponent<LabelSetter>().SetColorSliderRight(modifier.rightHandColor);
            }
            else
            {              
                colorPicker.GetComponent<LabelSetter>().SetSkyboxColor(modifier.leftHandColor, modifier.amount);
            }

            amountSlider.GetComponent<LabelSetter>().SetSliderValue(modifier.amount);
            currentModifier = modifier;
            fillingData = false;
        }

        private void SetStartTick(QNT_Timestamp tick)
        {
            currentModifier.startSet = true;
            currentModifier.CreateModifierMark(true, tick, true);
        }
        private void SetEndTick(QNT_Timestamp tick)
        {
            SetEndTick(tick.tick);
        }

        public void SetStartTick()
        {
            
            InitializeModifier();
            ulong tick = EditorTime.Time.tick;
            currentModifier.startTime = new QNT_Timestamp(tick);
            if (tick != 0 && tick >= currentModifier.endTime.tick && currentModifier.endTime.tick != 0)
            {
                //UpdateEndTick(tick);
                ResetEndTick(tick);
                startTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                currentModifier.UpdateMark(Modifier.UpdateType.UpdateStart, tick);
            }
            else
            {
                startTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                if (currentModifier.startSet)
                {
                    UpdateEndTick(tick);
                    currentModifier.UpdateMark(Modifier.UpdateType.MoveStart, tick);
                }
                else
                {
                    UpdateEndTick(tick);
                    currentModifier.CreateModifierMark(true, currentModifier.startTime, false);
                }

            }
            currentModifier.startSet = true;           
        }

        private void UpdateEndTick(float tick)
        {
            InitializeModifier();
            if (!currentModifier.startSet) return;
            if (!currentModifier.hasEndMark && currentModifier.endTime.tick != 0)
            {
                endTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                currentModifier.endTime = currentModifier.startTime;
            }
        }

        private void ResetEndTick(float tick)
        {
            InitializeModifier();
            if (!currentModifier.startSet) return;

            endTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
            currentModifier.endTime = currentModifier.startTime;
        }

        public void SetEndTick(float loadTick = -1f)
        {
            InitializeModifier();
            if (!currentModifier.startSet) return;
            ulong tick = EditorTime.Time.tick;
            if (loadTick != -1f) tick = (ulong)loadTick;
            if (tick != 0 && tick <= currentModifier.startTime.tick)
            {
                tick = currentModifier.startTime.tick;
                currentModifier.endTime = new QNT_Timestamp(tick);
                endTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                currentModifier.UpdateMark(Modifier.UpdateType.UpdateEnd);
            }
            else if (tick == 0)
            {
                currentModifier.endTime = new QNT_Timestamp(tick);
                endTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                currentModifier.UpdateMark(Modifier.UpdateType.UpdateEnd);
            }
            else
            {
                currentModifier.endTime = new QNT_Timestamp(tick);
                endTickButton.GetComponent<LabelSetter>().SetLabelText(tick.ToString());
                currentModifier.CreateModifierMark(false, currentModifier.endTime, loadTick != -1f);
            }
        }
        private void OnScaleChanged(int targetScale)
        {
            foreach (Modifier m in modifiers) m.Scale(EditorScale.InvertedScaleAmount);
            if (currentModifier != null) currentModifier.Scale(EditorScale.InvertedScaleAmount);
        }

        public void OnValue1Changed()
        {
            InitializeModifier();
            if(currentModifier.modifierType == ModifierType.ArenaBrightness) 
            {
                if (currentModifier.option2)
                {
                    if (int.TryParse(value1.GetComponent<LabelSetter>().GetText(), out int amount))
                    {
                        if (amount < 0) value1.GetComponent<LabelSetter>().SetInputText("0");
                        else if (amount > 100) value1.GetComponent<LabelSetter>().SetInputText("100");
                    }                    
                }
            }
            currentModifier.value1 = value1.GetComponent<LabelSetter>().GetText();
        }

        public void OnValue2Changed()
        {
            InitializeModifier();
            if (currentModifier.modifierType == ModifierType.ArenaBrightness)
            {
                if (currentModifier.option2)
                {
                    if (int.TryParse(value2.GetComponent<LabelSetter>().GetText(), out int amount))
                    {
                        if (amount < 0) value2.GetComponent<LabelSetter>().SetInputText("0");
                        else if (amount > 100) value2.GetComponent<LabelSetter>().SetInputText("100");
                    }
                }
            }
            currentModifier.value2 = value2.GetComponent<LabelSetter>().GetText();
        }

        public void OnValue3Changed()
        {
            InitializeModifier();
            currentModifier.xoffset = value3.GetComponent<LabelSetter>().GetText();
        }

        public void OnValue4Changed()
        {
            InitializeModifier();
            currentModifier.yoffset = value4.GetComponent<LabelSetter>().GetText();
        }

        public void OnValue5Changed()
        {
            InitializeModifier();
            currentModifier.zoffset = value5.GetComponent<LabelSetter>().GetText();
        }

        public void OnOption1Changed()
        {
            if (fillingData) return;
            InitializeModifier();
            currentModifier.option1 = option1.GetComponent<LabelSetter>().GetToggleState();
            SetHintText(currentModifier.modifierType);
        }

        public void OnIndependantBoolChanged()
        {
            InitializeModifier();
            currentModifier.independantBool = independantBool.GetComponent<LabelSetter>().GetToggleState();
            SetHintText(currentModifier.modifierType);
        }

        public void OnOption2Changed()
        {
            if (fillingData) return;
            InitializeModifier();
            currentModifier.option2 = option2.GetComponent<LabelSetter>().GetToggleState();
            SetHintText(currentModifier.modifierType);
        }

        public void OnAmountChanged()
        {
            InitializeModifier();
            currentModifier.amount = amountSlider.GetComponentInChildren<Slider>().value;
        }

        public void OnLeftColorChanged()
        {
            InitializeModifier();
            if(currentModifier.modifierType == ModifierType.ColorChange || currentModifier.modifierType == ModifierType.ColorUpdate)
            {
                currentModifier.leftHandColor = colorPicker.GetComponent<LabelSetter>().GetLeftColor();
            }
            else
            {
                currentModifier.leftHandColor = colorPicker.GetComponent<LabelSetter>().GetSkyboxColor();
                currentModifier.amount = colorPicker.GetComponent<LabelSetter>().GetSaturation();
            }
        }

        public void OnRightColorChanged()
        {
            InitializeModifier();
            if(currentModifier.modifierType == ModifierType.ColorChange || currentModifier.modifierType == ModifierType.ColorUpdate)
            {
                currentModifier.rightHandColor = colorPicker.GetComponent<LabelSetter>().GetRightColor();
            }
            else
            {
                currentModifier.leftHandColor = colorPicker.GetComponent<LabelSetter>().GetSkyboxColor();
                currentModifier.amount = colorPicker.GetComponent<LabelSetter>().GetSaturation();
            }
        }

        public void DeleteModifier()
        {
            ModifierSelectionHandler.Instance.DeleteSelectedModifiers();
            currentModifier = null;
            OnDropdownValueChanged();
        }

        public void DeleteModifier(Modifier modifier)
        {
            if (modifiers.Contains(modifier))
            {
                modifiers.Remove(modifier);
            }

            modifierPool.Return(modifier);
        }

        public void OnDeleteButtonClicked()
        {
            if (currentModifier != null)
            {
                if (currentModifier.isCreated)
                {
                    DeleteModifier();
                }
                else
                {
                    currentModifier.Delete();
                    currentModifier = null;
                    OnDropdownValueChanged();
                }
               
            }
        }

        public void OnEndTickButtonClicked()
        {
            InitializeModifier();
            if (!currentModifier.startSet) return;
            if(currentModifier.modifierType == ModifierType.zOffset)
            {
                int count = 0;
                foreach(Target t in EditorNotes.OrderedNotes)
                {                   
                    if (t.data.time >= currentModifier.startTime && t.data.time <= EditorTime.Time) count++;
                }
                value1.GetComponent<LabelSetter>().SetInputText(count.ToString());
            }
            else
            {
                SetEndTick(-1);
            }
            
        }

        private void DeactivateSidePanel()
        {
            sidePanel.SetActive(false);
            value3.SetActive(false);
            value4.SetActive(false);
            value5.SetActive(false);
            independantBool.SetActive(false);
        }
        private void ActivateSidePanel()
        {
            sidePanel.SetActive(true);
            value3.SetActive(true);
            value4.SetActive(true);
            value5.SetActive(true);
        }

        public void OnDropdownValueChanged()
        {
            if (!skipRefresh) ResetCurrentData();
            endTickButton.GetComponentsInChildren<TextMeshProUGUI>()[0].text = "Set End Tick";
            option1.GetComponent<LabelSetter>().EnableToggleGroup(true);
            trackerText.SetActive(false);
            ModifierType type = (ModifierType)dropdown.value;
            switch (type)
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
                    DeactivateSidePanel();
                    break;
                case ModifierType.ArenaBrightness:
                    amountSlider.SetActive(true);
                    endTickButton.GetComponent<LabelSetter>().SetLabelText("");
                    endTickButton.SetActive(false);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    colorPicker.SetActive(false);
                    option1.SetActive(true);
                    option2.SetActive(true);
                    option1.GetComponent<LabelSetter>().SetLabelText("Continuous");
                    option2.GetComponent<LabelSetter>().SetLabelText("Strobo");
                    DeactivateSidePanel();
                    break;
                case ModifierType.ArenaRotation:
                    amountSlider.SetActive(true);
                    endTickButton.GetComponent<LabelSetter>().SetLabelText("");
                    endTickButton.SetActive(false);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    option1.SetActive(true);
                    option2.SetActive(true);
                    option1.GetComponent<LabelSetter>().SetLabelText("Continuous");
                    option2.GetComponent<LabelSetter>().SetLabelText("Incremental");
                    DeactivateSidePanel();
                    break;
                case ModifierType.ColorChange:
                    amountSlider.SetActive(false);
                    endTickButton.SetActive(true);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    colorPicker.SetActive(true);
                    colorPicker.GetComponent<LabelSetter>().SetIsColorPicker(true);
                    DeactivateSidePanel();
                    break;
                case ModifierType.ColorUpdate:
                    amountSlider.SetActive(false);
                    endTickButton.GetComponent<LabelSetter>().SetLabelText("");
                    endTickButton.SetActive(false);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    colorPicker.SetActive(true);
                    colorPicker.GetComponent<LabelSetter>().SetIsColorPicker(true);
                    DeactivateSidePanel();
                    break;
                case ModifierType.SkyboxColor:
                    amountSlider.SetActive(false);
                    endTickButton.SetActive(true);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    option1.SetActive(false);
                    option2.SetActive(true);
                    colorPicker.SetActive(true);
                    colorPicker.GetComponent<LabelSetter>().SetIsColorPicker(false);
                    option2.GetComponent<LabelSetter>().SetLabelText("Reset");
                    DeactivateSidePanel();
                    break;
                case ModifierType.SkyboxLimiter:
                    amountSlider.SetActive(true);
                    endTickButton.GetComponent<LabelSetter>().SetLabelText("");
                    endTickButton.SetActive(false);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    DeactivateSidePanel();
                    break;
                case ModifierType.PsychedeliaUpdate:
                    amountSlider.SetActive(true);
                    endTickButton.GetComponent<LabelSetter>().SetLabelText("");
                    endTickButton.SetActive(false);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    DeactivateSidePanel();
                    break;
                case ModifierType.ColorSwap:
                case ModifierType.HiddenTelegraphs:
                case ModifierType.InvisibleGuns:
                    endTickButton.SetActive(true);
                    endTickButton.GetComponent<LabelSetter>().SetLabelText("");
                    amountSlider.SetActive(false);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    DeactivateSidePanel();
                    break;
                case ModifierType.zOffset:
                    amountSlider.SetActive(true);
                    endTickButton.GetComponentsInChildren<TextMeshProUGUI>()[0].text = "Set Transition";
                    endTickButton.GetComponentInChildren<LabelSetter>().SetLabelText("Length");                   
                    endTickButton.SetActive(true);
                    value2.SetActive(false);
                    option1.SetActive(false);
                    option2.SetActive(false);
                    value1.SetActive(true);
                    value1.GetComponent<LabelSetter>().SetLabelText("Transition Target Amount");
                    colorPicker.SetActive(false);
                    DeactivateSidePanel();
                    break;
                case ModifierType.ArenaChange:
                    amountSlider.SetActive(false);
                    endTickButton.GetComponent<LabelSetter>().SetLabelText("");
                    endTickButton.SetActive(false);
                    value1.SetActive(true);
                    value2.SetActive(true);
                    option1.SetActive(true);
                    Debug.Log("Set text to Arena Option 1/2 and Preload");
                    value1.GetComponent<LabelSetter>().SetLabelText("Arena Option 1");
                    value2.GetComponent<LabelSetter>().SetLabelText("Arena Option 2");
                    option1.GetComponent<LabelSetter>().SetLabelText("Preload");
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    DeactivateSidePanel();
                    break;
                case ModifierType.OverlaySetter:
                    amountSlider.SetActive(false);
                    endTickButton.GetComponent<LabelSetter>().SetLabelText("");
                    endTickButton.SetActive(false);
                    value1.SetActive(true);
                    value2.SetActive(true);
                    value1.GetComponent<LabelSetter>().SetLabelText("Song Info");
                    value2.GetComponent<LabelSetter>().SetLabelText("Mapper");
                    option1.SetActive(false);
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    DeactivateSidePanel();
                    break;
                case ModifierType.TextPopup:
                    amountSlider.SetActive(false);
                    endTickButton.SetActive(true);
                    value1.SetActive(true);
                    value2.SetActive(true);                   
                    option1.SetActive(true);
                    option2.SetActive(false);
                    independantBool.SetActive(true);
                    value1.GetComponent<LabelSetter>().SetLabelText("Text");
                    value2.GetComponent<LabelSetter>().SetLabelText("Size");
                    value3.GetComponent<LabelSetter>().SetLabelText("X offset");
                    value4.GetComponent<LabelSetter>().SetLabelText("Y offset");
                    value5.GetComponent<LabelSetter>().SetLabelText("Z offset");
                    option1.GetComponent<LabelSetter>().SetLabelText("Glow");
                    independantBool.GetComponent<LabelSetter>().SetLabelText("Face Forward");
                    colorPicker.SetActive(false);
                    ActivateSidePanel();
                    break;
                case ModifierType.AutoLighting:
                    endTickButton.SetActive(true);
                    amountSlider.SetActive(true);
                    value1.SetActive(false);
                    value2.SetActive(false);
                    option1.SetActive(true);
                    option1.GetComponent<LabelSetter>().SetLabelText("Alternative");
                    option2.SetActive(false);
                    colorPicker.SetActive(false);
                    DeactivateSidePanel();
                    break;
                case ModifierType.ArenaPosition:
                case ModifierType.ArenaSpin:
                case ModifierType.ArenaScale:
                    endTickButton.SetActive(true);
                    amountSlider.SetActive(false);
                    value1.SetActive(false);
                    option1.SetActive(true);
                    option2.SetActive(true);
                    colorPicker.SetActive(false);
                    trackerText.SetActive(true);
                    value2.SetActive(false);
                    value3.GetComponent<LabelSetter>().SetLabelText("X");
                    value4.GetComponent<LabelSetter>().SetLabelText("Y");
                    value5.GetComponent<LabelSetter>().SetLabelText("Z");
                    option1.GetComponent<LabelSetter>().SetLabelText("Reset");
                    option2.GetComponent<LabelSetter>().SetLabelText("Preload");
                    if (type == ModifierType.ArenaSpin)
                    {
                        independantBool.SetActive(true);
                        independantBool.GetComponent<LabelSetter>().SetLabelText("Shortest way");
                    }
                    else
                    {
                        independantBool.SetActive(false);
                    }
                    GetCurrentManipulationValues(type);
                    ActivateSidePanel();
                    break;
            }
            SetHintText(type);
            SetMinMax(type);
            if (!skipRefresh) createModifierButton.GetComponent<LabelSetter>().SetLabelText("Create Modifier");
           
            skipRefresh = false;
        }

        public void UpdateManipulationValues()
        {
            GetCurrentManipulationValues((ModifierType)dropdown.value);
        }

        private void GetCurrentManipulationValues(ModifierType type)
        {
            Vector3 current = Vector3.zero;
            Vector3 lastValues = Vector3.zero;
            if (type == ModifierType.ArenaScale) lastValues = new Vector3(1f, 1f, 1f);
            bool found = false;
            foreach(Modifier m in modifiers)
            {
                if (m.startTime > EditorTime.Time) break;
                if (m.modifierType != type) continue;
                found = true;
                float x, y, z;
                if (!float.TryParse(m.xoffset, out x)) x = lastValues.x;
                if (!float.TryParse(m.yoffset, out y)) y = lastValues.y;
                if (!float.TryParse(m.zoffset, out z)) z = lastValues.z;
                Vector3 amnt = new Vector3(x, y, z);
                if (m.option1)
                {
                    if (type == ModifierType.ArenaScale) current = new Vector3(1f, 1f, 1f);
                    else current = Vector3.zero;
                }
                else current = amnt;

                lastValues = current;
            }
            if (!found && type == ModifierType.ArenaScale) current = new Vector3(1f, 1f, 1f);
            string text = "Current ";
            text += (type == ModifierType.ArenaPosition ? "position" : type == ModifierType.ArenaSpin ? "rotation" : "scale") + "\n\n";
            text += $"{current.x}/{current.y}/{current.z}";
            trackerText.GetComponent<LabelSetter>().SetTrackerText(text);
        }

        private void PickLastUsedColor()
        {
            if (currentModifier.modifierType != ModifierType.ColorChange && currentModifier.modifierType != ModifierType.ColorUpdate) return;
            Modifier closestColorModifier = null;
            foreach(Modifier m in modifiers)
            {
                if (m.modifierType != ModifierType.ColorChange && m.modifierType != ModifierType.ColorUpdate) continue;
                if (m.startTime >= EditorTime.Time) continue;
                if(closestColorModifier is null)
                {
                    closestColorModifier = m;
                    continue;
                }
                if (closestColorModifier.startTime < m.startTime) closestColorModifier = m;
            }
            if(closestColorModifier != null)
            {
                currentModifier.leftHandColor = closestColorModifier.leftHandColor;
                currentModifier.rightHandColor = closestColorModifier.rightHandColor;
                colorPicker.GetComponent<LabelSetter>().SetColorSliderLeft(currentModifier.leftHandColor);
                colorPicker.GetComponent<LabelSetter>().SetColorSliderRight(currentModifier.rightHandColor);
            }
            else
            {
                currentModifier.leftHandColor = colorPicker.GetComponent<LabelSetter>().GetLeftColor();
                currentModifier.rightHandColor = colorPicker.GetComponent<LabelSetter>().GetRightColor();
            }
        }

        private void SetHintText(ModifierType type)
        {
            string text;
            value1.GetComponent<NRInputField>().contentType = TMP_InputField.ContentType.Standard;
            value2.GetComponent<NRInputField>().contentType = TMP_InputField.ContentType.Standard;
            switch (type)
            {
                case ModifierType.AimAssist:
                case ModifierType.Particles:
                case ModifierType.Speed:
                    text = "Default: 100";
                    break;
                case ModifierType.ArenaBrightness:
                    value1.SetActive(false);
                    value2.SetActive(false);
                    if(currentModifier != null)
                    {
                        if (currentModifier.option2)
                        {
                            text = "<size=6.9>Amount represents flashes per beat (1/4 note)";
                            slider.SetMinValue(1f);
                            slider.SetMaxValue(128f);
                            LabelSetter ls1 = value1.GetComponent<LabelSetter>();
                            LabelSetter ls2 = value2.GetComponent<LabelSetter>();
                            ls1.SetLabelText("OFF Brightness");
                            ls2.SetLabelText("ON Brightness");
                            value1.GetComponent<NRInputField>().contentType = TMP_InputField.ContentType.IntegerNumber;
                            value2.GetComponent<NRInputField>().contentType = TMP_InputField.ContentType.IntegerNumber;
                            value1.SetActive(true);
                            value2.SetActive(true);
                            //float amnt = currentModifier.amount >= 1f && currentModifier.amount <= 128f ? currentModifier.amount : 1f;
                            //slider.SetSliderValue(currentModifier.amount);
                            endTickButton.SetActive(true);
                            break;
                        }
                        if (currentModifier.option1)
                        {
                            SetMinMax(type);
                            text = "Amount represents speed";
                            endTickButton.SetActive(true);
                            break;
                        }
                    }
                    SetMinMax(type);
                    endTickButton.SetActive(false);
                    text = "Default: 100";
                    break;
                case ModifierType.ArenaRotation:
                    if(currentModifier != null)
                    {
                        if (currentModifier.option2)
                        {
                            text = "Amount represents speed at end tick";
                            endTickButton.SetActive(true);
                            break;
                        }
                        if (currentModifier.option1)
                        {
                            text = "Amount represents rotation speed";
                            endTickButton.SetActive(true);
                            break;
                        }
                    }
                    endTickButton.SetActive(false);
                    text = "Default: 0";
                    break;
                case ModifierType.Psychedelia:
                case ModifierType.PsychedeliaUpdate:
                    text = "Amount represents cycle speed";
                    break;
                case ModifierType.zOffset:
                    text = "Default: 0";
                    break;
                case ModifierType.Fader:
                    text = "Fades from current brightness to amount";
                    break;
                case ModifierType.AutoLighting:
                    text = "Controls the maximum amount of brightness allowed\n(Default: 100)";
                    break;
                case ModifierType.SkyboxLimiter:
                    text = "Sets maximum allowed brightness\n(Default: 100 | Off: 0)";
                    break;
                default:
                    text = "";
                    break;
            }                   
            amountSlider.GetComponent<LabelSetter>().SetHintText(text);
        }

        private void InitializeModifier()
        {            
            if (currentModifier != null) return;
                
            ModifierType type = (ModifierType)dropdown.value;
            string shorthand = GetShorthand(type);
            currentModifier = modifierPool.Spawn();//Instantiate(modifierPrefab).GetComponent<Modifier>();
            currentModifier.modifierType = type;
            currentModifier.shorthand = shorthand;
            PickLastUsedColor();
        }

        private string GetShorthand(ModifierType type)
        {
            string sh = "";
            switch (type)
            {
                case ModifierType.AimAssist:
                    sh = "AA";
                    break;
                case ModifierType.ArenaBrightness:
                    sh = "SB";
                    break;
                case ModifierType.ArenaRotation:
                    sh = "SR";
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
                    sh = "SF";
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
                case ModifierType.OverlaySetter:
                    sh = "OS";
                    break;
                case ModifierType.TextPopup:
                    sh = "TP";
                    break;
                case ModifierType.AutoLighting:
                    sh = "AL";
                    break;
                case ModifierType.ArenaPosition:
                    sh = "AP";
                    break;
                case ModifierType.ArenaSpin:
                    sh = "AR";
                    break;
                case ModifierType.ArenaScale:
                    sh = "AS";
                    break;
                case ModifierType.SkyboxColor:
                    sh = "SC";
                    break;
                case ModifierType.SkyboxLimiter:
                    sh = "SL";
                    break;
                default:
                    break;
            }
            return sh;
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
                    slider.SetMinValue(-500f);
                    slider.SetMaxValue(500f);
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
                    slider.SetMaxValue(500f);
                    break;
                case ModifierType.ColorChange:
                case ModifierType.ColorUpdate:
                case ModifierType.SkyboxColor:
                    colorPicker.GetComponent<LabelSetter>().SetMinMaxColorSliders(0f, 1f);
                    if(type == ModifierType.SkyboxColor)
                    {
                        colorPicker.GetComponent<LabelSetter>().SetSaturationRightValue(1f);
                    }
                    else
                    {
                        colorPicker.GetComponent<LabelSetter>().SetSaturationRightValue(0f);
                    }
                    break;
                case ModifierType.AutoLighting:
                    slider.SetMinValue(0f);
                    slider.SetMaxValue(100f);
                    break;
                case ModifierType.SkyboxLimiter:
                    slider.SetMinValue(0f);
                    slider.SetMaxValue(100f);
                    break;
                default:
                    break;
            }
        }

        private void ResetCurrentData()
        {
            if(currentModifier != null)
            {
                if (!currentModifier.isCreated)
                {
                    currentModifier.Delete();
                }
                else
                {
                    CreateModifier();
                }
                   
                currentModifier = null;
            }
            amountSlider.GetComponent<LabelSetter>().SetSliderValue(0f);
            startTickButton.GetComponent<LabelSetter>().SetLabelText("0");
            endTickButton.GetComponent<LabelSetter>().SetLabelText("0");
            value1.GetComponent<LabelSetter>().SetInputText("");
            value2.GetComponent<LabelSetter>().SetInputText("");
            value3.GetComponent<LabelSetter>().SetInputText("");
            value4.GetComponent<LabelSetter>().SetInputText("");
            value5.GetComponent<LabelSetter>().SetInputText("");
            option1.GetComponent<LabelSetter>().SetToggleState(false);
            option2.GetComponent<LabelSetter>().SetToggleState(false);
            independantBool.GetComponent<LabelSetter>().SetToggleState(false);
            colorPicker.GetComponent<LabelSetter>().SetColorSliderLeft(new float[] { 0f, 0f, 0f });
            colorPicker.GetComponent<LabelSetter>().SetColorSliderRight(new float[] { 0f, 0f, 0f });
            colorPicker.GetComponent<LabelSetter>().InitializeColorFields();
        }

        protected override void RegisterCallbacks()
        {
            actions.Modifiers.BakeZOffset.performed += _ => ZOffsetBaker.Instance.ToggleWindow();
            selectionHandler.RegisterCallbacks(actions);
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            ToggleModifiers();
        }

        protected override void SetRebindConfiguration(ref RebindConfiguration options, ModifierKeybinds myKeybinds)
        {
            options.SetAssetTitle("Modifiers").SetRebindable(true);
            options.AddHiddenKeybinds(myKeybinds.Modifiers.LeftMouseClick);
            options.AddHiddenKeybinds(myKeybinds.Modifiers.Scrub);
            options.AddNonRebindableKeybinds(myKeybinds.Modifiers.RemoveModifier);
            options.AddNonRebindableKeybinds(myKeybinds.Modifiers.DragSelect);
            options.AddNonRebindableKeybinds(myKeybinds.Modifiers.Delete);
        }

        //public enum ModifierType { AimAssist = 0, ColorChange = 1, ColorUpdate = 2, ColorSwap = 3, HiddenTelegraphs = 4, InvisibleGuns = 5, Particles = 6, Psychedelia = 7, PsychedeliaUpdate = 8, Speed = 9, zOffset = 10, ArenaRotation = 11, ArenaBrightness = 12, ArenaChange = 13, Fader = 14, OverlaySetter = 15, TextPopup = 16, AutoLighting = 17, ArenaPosition = 18, ArenaSpin = 19, ArenaScale = 20, SkyboxColor = 21 }
        public enum ModifierType
        {
            AimAssist = 0,
            ArenaChange = 1,
            ColorChange = 2,
            ColorSwap = 3,
            ColorUpdate = 4,
            HiddenTelegraphs = 5,
            InvisibleGuns = 6,
            OverlaySetter = 7,
            Particles = 8,
            Psychedelia = 9,
            PsychedeliaUpdate = 10,
            AutoLighting = 11,
            SkyboxColor = 12,
            ArenaBrightness = 13,
            Fader = 14,
            SkyboxLimiter = 15,
            ArenaRotation = 16,
            Speed = 17,
            TextPopup = 18,
            zOffset = 19,
            ArenaPosition = 20,
            ArenaSpin = 21,
            ArenaScale = 22,
        }
    }
}



