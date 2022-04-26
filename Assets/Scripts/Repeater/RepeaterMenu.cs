using DG.Tweening;
using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Overlays;
using NotReaper.Timing;
using NotReaper.UI;
using NotReaper.UI.Components;
using NotReaper.UserInput;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NotReaper.Repeaters
{
    public class RepeaterMenu : NROverlay
    {
        [Header("References")]
        [SerializeField] private NRInputField inputID;
        [SerializeField] private NRButton buttonInsertCreateRepeater;
        [SerializeField] private NRButton buttonRenameRepeater;
        [SerializeField] private NRButton buttonMakeUnique;
        [SerializeField] private NRButton buttonDelete;
        [SerializeField] private NRButton buttonDeleteChildren;
        [SerializeField] private NRButton buttonClose;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private NRToggle toggleFlipTargetColors;
        [SerializeField] private NRToggle toggleMirrorHorizontally;
        [SerializeField] private NRToggle toggleMirrorVertically;
        [SerializeField] private NRInputField inputRename;
        [SerializeField] private RepeaterListEntry repeaterListEntryPrefab;
        [SerializeField] private GameObject hint;
        [SerializeField] private Transform contentParent;
        [SerializeField] private ScrollRect scroller;
        [SerializeField] private OnHover onHover;
        private RepeaterManager manager;
        private List<RepeaterListEntry> repeaterListEntries;
        private bool isRenaming = false;
        private State state = State.Disabled;
        private RepeaterIndicator activeSection;
        //private Timeline timeline;
        //private CanvasGroup canvas;
        public bool isActive { get; private set; }
        public bool IsHovering => _isHoveringList && scroller.verticalScrollbar.gameObject.activeInHierarchy;
        private bool _isHoveringList;
        protected override void Start()
        {
            manager = NRDependencyInjector.Get<RepeaterManager>();
            timeline = NRDependencyInjector.Get<Timeline>();
            repeaterListEntries = new();
            //GetComponent<Canvas>().worldCamera = Camera.main;
            canvas = GetComponent<CanvasGroup>();
            canvas.alpha = 0f;
            transform.position = Vector3.zero;
            onHover.onHover.AddListener(OnListHover);
            inputID.inputField.onSelect.AddListener(OnInputFocused);
            inputRename.inputField.onSelect.AddListener(OnInputFocused);
            inputID.inputField.onDeselect.AddListener(OnInputFocusLost);
            inputRename.inputField.onDeselect.AddListener(OnInputFocusLost);
            Reset();
            base.Start();
            //gameObject.SetActive(false);
        }

        private void Reset()
        {
            buttonMakeUnique.gameObject.SetActive(false);
            buttonDelete.SetText("delete");
            buttonDelete.gameObject.SetActive(false);
            buttonDeleteChildren.gameObject.SetActive(false);
            hint.gameObject.SetActive(false);
            inputID.text = "";
            state = State.Disabled;
            isRenaming = false;
            buttonRenameRepeater.SetText("rename");
            inputRename.text = "";
            inputRename.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
            toggleMirrorHorizontally.selected = false;
            toggleFlipTargetColors.selected = false;
            toggleMirrorVertically.selected = false;
            if(activeSection != null)
            {
                activeSection.SetSectionActive(false);
                activeSection = null;
            }
        }

        internal void SelectRepeater(string id)
        {
            inputID.text = id;
            UpdateState();
        }

        public void ValidateID()
        {
            UpdateState();
        }

        public override void Show()
        {
            isActive = true;
            EditorState.SetIsInUI(true);
            EditorState.LockInUI();
            EditorNotes.onSelectedNoteCountChanged += OnNoteCountChanged;
            manager.SetRepeatersInteractable(true);
            manager.Activate();
            UpdateState();
            OnActivated();
            canvas.DOFade(1f, .3f);
        }

        public override void Hide()
        {
            isActive = false;
            EditorState.UnlockInUI();
            EditorState.SetIsInUI(false);
            EditorNotes.onSelectedNoteCountChanged += OnNoteCountChanged;
            manager.Deactivate();
            canvas.DOFade(0f, .3f).OnComplete(() =>
            {
                manager.SetRepeatersInteractable(false);
                Reset();
                OnDeactivated();
            });        
        }

        private void OnNoteCountChanged(int count)
        {
            UpdateState();
        }

        public override void ShowHelp()
        {
            NRHelp.Instance.ShowRepeater();
        }

        public void OnRenameClicked()
        {
            if (!isRenaming)
            {
                inputRename.gameObject.SetActive(true);
                buttonRenameRepeater.SetText("apply");
            }
            else
            {
                if (string.IsNullOrEmpty(inputRename.text))
                {
                    NotificationCenter.SendNotification("Please enter a new ID into the rename input field.", NotificationType.Warning);
                    return;
                }
                if (manager.RepeaterExists(inputRename.text))
                {
                    NotificationCenter.SendNotification("ID already exists. Please choose a different one.", NotificationType.Warning);
                    return;
                }
                manager.RenameRepeater(inputID.text, inputRename.text);
                inputID.text = inputRename.text;
                inputRename.text = "";
                inputRename.gameObject.SetActive(false);
                buttonRenameRepeater.SetText("rename");
            }
            isRenaming = !isRenaming;
        }

        public void UpdateRepeaterID(string oldID, string newID)
        {
            repeaterListEntries.First(e => e.GetID() == oldID).SetID(newID);
        }

        public void OnInsertCreateClicked()
        {
            if (!buttonInsertCreateRepeater.gameObject.activeInHierarchy)
                return;

            if (string.IsNullOrEmpty(inputID.text))
            {
                NotificationCenter.SendNotification("Please enter an ID to create or insert a repeater.", NotificationType.Error);
                return;
            }

            if (EditorTime.Time.tick < (EditorTempo.GetTempoForTime(EditorTime.Time).timeSignature.Numerator * 2) * Constants.QuarterNoteDuration.tick)
            {
                NotificationCenter.SendNotification("Nice try, but no, you can't place repeaters inside the intro zone either.", NotificationType.Warning);
                return;
            }

            UpdateState();
            if (state == State.Insert)
            {
                manager.AddRepeater(inputID.text, EditorTime.Time);
            }
            else
            {
                if(!manager.AddRepeater(inputID.text, EditorNotes.SelectedNotes.First().data.time, EditorNotes.SelectedNotes.Last().data.time))
                {
                    return;
                }
                SpawnRepeaterEntry(inputID.text);
                EditorNotes.DeselectAllTargets();
            }
            if(activeSection != null)
            {
                activeSection.SetSectionActive(false);
                activeSection = null;
            }
            UpdateState();
        }

        public void SpawnRepeaterEntry(string ID)
        {
            if (repeaterListEntries.Any(entry => entry.GetID() == ID))
                return;

            var entry = Instantiate(repeaterListEntryPrefab, contentParent);
            entry.SetID(ID);
            repeaterListEntries.Add(entry);
        }

        public void OnMakeUniqueClicked()
        {
            if (activeSection == null || !buttonMakeUnique.gameObject.activeInHierarchy)
                return;

            if (manager.MakeSectionUnique(activeSection.GetSection(), out string newID))
            {
                activeSection.SetText(newID);
                SpawnRepeaterEntry(newID);
                UpdateState();
            }
        }

        public void OnDeleteClicked()
        {
            if (activeSection == null)
                return;

            string id = activeSection.GetSection().ID;
            if (activeSection.GetSection().isParent)
            {
                manager.RemoveAllRepeatersWithID(id);
                inputID.text = "";
            }
            else
            {
                manager.RemoveRepeater(activeSection.GetSection());
            }
            activeSection = null;
            UpdateState();
        }

        public void OnDeleteChildrenClicked()
        {
            manager.RemoveAllChildRepeaters(activeSection.GetSection().ID);
            UpdateState();
        }

        public void OnFlipTargetColorsToggled()
        {
            if (activeSection == null || !settingsPanel.activeInHierarchy)
                return;

            manager.FlipRepeaterTargetColors(activeSection.GetSection().ID, activeSection.GetSection().startTime, toggleFlipTargetColors.isOn);
        }

        public void OnMirrorHorizontallyToggled()
        {
            if (activeSection == null || !settingsPanel.activeInHierarchy)
                return;

            manager.MirrorRepeaterHorizontally(activeSection.GetSection().ID, activeSection.GetSection().startTime, toggleMirrorHorizontally.isOn);
        }

        public void OnMirrorVerticallyToggled()
        {
            if (activeSection == null || !settingsPanel.activeInHierarchy)
                return;

            manager.MirrorRepeaterVertically(activeSection.GetSection().ID, activeSection.GetSection().startTime, toggleMirrorVertically.isOn);
        }

        public void UpdateToggles()
        {
            if (!isActive || activeSection == null)
                return;

            var section = activeSection.GetSection();
            toggleMirrorHorizontally.selected = section.mirrorHorizontally;
            toggleMirrorVertically.selected = section.mirrorVertically;
            toggleFlipTargetColors.selected = section.flipTargetColors;
        }

        public void OnBakeClicked()
        {
            if (activeSection == null)
                return;

            manager.BakeRepeaterSection(activeSection.GetSection());
            activeSection = null;
            UpdateState();
        }

        public void OnRepeaterNameInputChanged()
        {
            if (manager.RepeaterExists(inputID.text))
                buttonInsertCreateRepeater.SetText("insert");
            else
                buttonInsertCreateRepeater.SetText("create");
        }

        private void UpdateState()
        {
            string currentID = inputID.text;
            hint.SetActive(false);
            buttonInsertCreateRepeater.gameObject.SetActive(true);
            buttonMakeUnique.gameObject.SetActive(false);
            buttonDelete.gameObject.SetActive(false);
            buttonDeleteChildren.gameObject.SetActive(false);
            buttonRenameRepeater.gameObject.SetActive(false);
            settingsPanel.SetActive(false);
            if (manager.RepeaterExists(inputID.text))
            {
                buttonRenameRepeater.gameObject.SetActive(true);
            }

            if (manager.RepeaterExists(currentID))
            {
                buttonInsertCreateRepeater.SetText("insert");
                state = State.Insert;
            }
            else
            {
                if (EditorNotes.SelectedNotes.Count < 2)
                {
                    hint.SetActive(true);
                    buttonInsertCreateRepeater.gameObject.SetActive(false);
                    state = State.Disabled;
                    return;
                }
                buttonInsertCreateRepeater.SetText("create");
                state = State.Create;
            }
            
            if(activeSection != null)
            {
                buttonDelete.gameObject.SetActive(true);
                
                if (activeSection.GetSection().isParent)
                {
                    buttonDelete.SetText("delete all");
                    buttonDeleteChildren.gameObject.SetActive(true);
                }
                else
                {
                    buttonDelete.SetText("delete");
                    buttonMakeUnique.gameObject.SetActive(true);
                    settingsPanel.SetActive(true);
                }
            }
        }

        public void SetActiveSection(RepeaterIndicator section)
        {
            if (activeSection != null) activeSection.SetSectionActive(false);
            section.SetSectionActive(true);
            activeSection = section;
            inputID.text = activeSection.GetSection().ID;
            toggleFlipTargetColors.selected = activeSection.GetSection().flipTargetColors;
            toggleMirrorVertically.selected = activeSection.GetSection().mirrorVertically;
            toggleMirrorHorizontally.selected = activeSection.GetSection().mirrorHorizontally;
            UpdateState();
        }

        public void RemoveEntry(string id)
        {
            var entry = repeaterListEntries.Where(e => e.GetID() == id).FirstOrDefault();

            if (entry == null)
                return;

            repeaterListEntries.Remove(entry);
            Destroy(entry.gameObject);
        }

        public void RemoveAllEntries()
        {
            for (int i = repeaterListEntries.Count - 1; i >= 0; i--)
            {
                Destroy(repeaterListEntries[i].gameObject);
            }
            repeaterListEntries.Clear();
        }

        public void OnListHover(bool isHovering)
        {
            _isHoveringList = isHovering;
            manager.EnableScrubbing(!isHovering);
        }

        private void OnInputFocused(string _) => manager.OnInputFocused(true);
        private void OnInputFocusLost(string _) => manager.OnInputFocused(false);

        [NRListener]
        protected override void OnEditorModeChanged(EditorMode mode)
        {
            if (!gameObject.activeInHierarchy) return;

            if (mode != EditorMode.Compose)
            {
                Hide();
            }
        }

        private enum State
        {
            Create,
            Insert,
            Disabled
        }

    }

}
