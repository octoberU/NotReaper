using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.UI;
using NotReaper.Timing;
using SFB;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NotReaper.Managers;
using NotReaper.Notifications;
using UnityEngine.InputSystem;
using NotReaper.UserInput;
using NotReaper.UI.Components;

namespace NotReaper.ReviewSystem
{
    public class ReviewManager : NRInput<ReviewKeybinds>
    {
        public static ReviewManager Instance { get; private set; } = null;
        public static bool IsOpen = false;
        #region References
        [SerializeField] private ReviewOverlay overlay;
        [SerializeField] private CanvasGroup windowCanvas;
        [SerializeField] private TMP_InputField commentField;
        [SerializeField] private NRInputField authorField;
        [SerializeField] private GameObject makeSuggestionButton;
        [SerializeField] private GameObject showSuggestionButton;
        [SerializeField] private TextMeshProUGUI authorText;
        [SerializeField] private NRDropdown commentTypeDrop;
        [SerializeField] private TextMeshProUGUI commentTypeText;
        [SerializeField] private TextMeshProUGUI modeText;
        [SerializeField] private GameObject writeModeButtonsPanel;
        [SerializeField] private GameObject readModeButtonsPanel;
        [SerializeField] private NRButton checkCommentButton;
        [SerializeField] private GameObject writeSidePanel;
        [SerializeField] private NRButton toggleCommentsButton;
        [Space]
        [SerializeField] private GameObject commentListPanel;
        [SerializeField] private CommentEntry commentEntryPrefab;
        [SerializeField] private RectTransform commentListContent;
        [SerializeField] private ScrollRect scroller;
        #endregion

        [NRInject] private ReviewExtraPanels extraPanels;
        private ReviewContainer loadedContainer = new ReviewContainer();
        private ReviewComment currentComment = new ReviewComment();
        public ReviewMode SelectedMode { get; set; } = ReviewMode.Read;

        private List<CommentEntry> commentEntries = new List<CommentEntry>();
        private Vector2 lastOpenPosition = Vector2.zero;
        private float lastScrollPosition = 1f;

        [NRInject] private Timeline timeline;
        [NRInject] private UIModeSelect modeSelect;

        private GameObject editSuggestionPanel;
        private GameObject selectCuesPanel;
        private GameObject viewSuggestionPanel;

        protected override void Awake()
        {
            if (Instance is null) Instance = this;
            else
            {
                Debug.LogWarning("ReviewWindow already exists.");
                return;
            }
            base.Awake();
            //ShowWindow(false);
        }

        bool init = false;
        private void Start()
        {
            editSuggestionPanel = extraPanels.makeSuggestionPanel;
            selectCuesPanel = extraPanels.selectCuesPanel;
            viewSuggestionPanel = extraPanels.viewSuggestionPanel;

            SetMode(ReviewMode.Read);
            makeSuggestionButton.SetActive(false);
            showSuggestionButton.SetActive(false);
            string exportFolder = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "reviews");
            if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
            gameObject.SetActive(false);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            EditorState.SetIsInUI(true);
            gameObject.SetActive(true);
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            EditorState.SetIsInUI(false);
            gameObject.SetActive(false);
        }

        public void ShowWindow(bool show)
        {
            IsOpen = show;
            //if (!show && init) lastOpenPosition = overlay.transform.localPosition;
            //overlay.transform.localPosition = show ? lastOpenPosition : new Vector2(-4300f, 0f);
            if (!init) init = true;
            //overlay.SetActive(show);
            if (!show)
            {
                modeSelect.EnableButtons(true);
                lastScrollPosition = scroller.verticalNormalizedPosition;
                overlay.Hide();
                OnDeactivated();
                
            }
            else
            {
                overlay.Show();
                if (isShowingSuggestion)
                {
                    ShowSuggestion();
                }
                
                OnActivated();
                editSuggestionPanel.SetActive(false);
                selectCuesPanel.SetActive(false);
                StartCoroutine(UpdateScroller(lastScrollPosition));
            }
        }

        public void ToggleWindow()
        {
            ShowWindow(!IsOpen);
        }
        
        public void NextComment()
        {
            int nextIndex = loadedContainer.comments.IndexOf(currentComment) + 1;
            SelectComment(nextIndex);            
        }

        public void PreviousComment()
        {
            int nextIndex = loadedContainer.comments.IndexOf(currentComment) - 1;
            SelectComment(nextIndex);
        }

        public void SelectComment(int index)
        {
            if (index < 0 || index >= loadedContainer.comments.Count) return;
            EditorNotes.DeselectAllTargets();
            currentComment = loadedContainer.comments[index];
            if(currentComment.type != CommentType.General)
            {
                Cue firstCue = currentComment.selectedCues.FirstOrDefault();
                Cue lastCue = currentComment.selectedCues.LastOrDefault();
                EditorNotes.SelectTargets(SelectTargets(firstCue.tick, lastCue.tick).ToList());
            }

            //StartCoroutine(timeline.AnimateSetTime(new QNT_Timestamp((ulong)firstCue.tick)));
            EditorAudio.JumpToTime(new((ulong)currentComment.tick));

            FillData();
            foreach (CommentEntry ce in commentEntries) ce.Selected = false;
            commentEntries[index].Selected = true;
            checkCommentButton.SetText(currentComment.isChecked ? "Uncheck Comment" : "Check Comment");
            currentComment.entry.SetChecked(currentComment.isChecked);
            makeSuggestionButton.SetActive(currentComment.HasSelectedCues);
            showSuggestionButton.SetActive(currentComment.HasSuggestion);
        }

        public void DeselectComment()
        {
            if(currentComment.entry != null) currentComment.entry.Selected = false;
            currentComment = new ReviewComment();
            makeSuggestionButton.SetActive(false);
            showSuggestionButton.SetActive(false);
            EditorNotes.DeselectAllTargets();
        }

        public void FillData()
        {
            commentField.text = currentComment.description;
            switch (currentComment.type)
            {
                case CommentType.Positive:
                    commentTypeDrop.value = 1;
                    commentTypeText.text = @"<color=green>Positive";
                    break;
                case CommentType.Negative:
                    commentTypeDrop.value = 0;
                    commentTypeText.text = @"<color=orange>Negative";
                    break;
                case CommentType.Suggestion:
                    commentTypeDrop.value = 2;
                    commentTypeText.text = @"<color=lightblue>Suggestion";
                    break;
                case CommentType.General:
                    commentTypeDrop.value = 2;
                    commentTypeText.text = @"<color=lightblue>General";
                    break;
            }
        }

        public void RemoveComment()
        {
            if (loadedContainer.comments.Contains(currentComment))
            {
                EditorNotes.DeselectAllTargets();
                loadedContainer.comments.Remove(currentComment);
                RemoveCommentEntry(currentComment);
                //NotificationCenter.SendNotification($"Removed comment", NRNotifType.Success);
            }
            else NotificationCenter.SendNotification("Comment doesn't exist. Restart NotReaper.", NotificationType.Error);
        }

        /// <summary>
        /// Creates a review comment using selected notes and text fields.
        /// </summary>
        public void SaveComment()
        {
            if(EditorNotes.SelectedNotes.Count == 0 && commentField.text.Length == 0)
            {
                Export(false);
                return;
            }
            bool isCommentOnly = true;
            var selectedCues = new List<Cue>();
            if (EditorNotes.HasSelectedNotes)
            {
                foreach (Target target in EditorNotes.SelectedNotes)
                {
                    selectedCues.Add(target.ToCue());
                }
                selectedCues.Sort((c1, c2) => c1.tick.CompareTo(c2.tick));
                currentComment.selectedCues = selectedCues.ToArray();
                isCommentOnly = false;
                currentComment.tick = selectedCues.First().tick;
            }
            else
            {
                currentComment.tick = 0;
            }
            currentComment.description = commentField.text;
            currentComment.type = isCommentOnly ? CommentType.General : (CommentType)commentTypeDrop.value;
            if(loadedContainer == null)
            {
                loadedContainer = new();
            }
            if(!loadedContainer.comments.Contains(currentComment)) loadedContainer.comments.Add(currentComment);
            //if(loadedContainer.comments.Count > 1) loadedContainer.comments.Sort((c1, c2) => c1.selectedCues.First().tick.CompareTo(c2.selectedCues.First().tick));
            if(loadedContainer.comments.Count > 1) loadedContainer.comments.Sort((c1, c2) => c1.tick.CompareTo(c2.tick));
            if (isCommentOnly)
            {
                NotificationCenter.SendNotification($"Saved comment.", NotificationType.Success, false);
            }
            else
            {
                string targetPlural = selectedCues.Count == 1 ? "target" : "targets";
                NotificationCenter.SendNotification($"Saved comment for {selectedCues.Count} {targetPlural}", NotificationType.Success, false);
            }

            if (currentComment.entry is null) CreateCommentEntry(currentComment);
            else
            {
                currentComment.entry.UpdateEntry();
                SortEntries();
            }

            DeselectComment();
            FillData();
            Export(false);
        }

        public void NewComment()
        {
            DeselectComment();
            FillData();
        }

        public void CreateCommentEntry(ReviewComment comment)
        {
            CommentEntry entry = Instantiate(commentEntryPrefab, commentListContent);
            entry.SetData(comment);
            comment.entry = entry;
            entry.Index = loadedContainer.comments.IndexOf(comment);
            commentEntries.Add(entry);
            SortEntries();    
        }
        public void RemoveCommentEntry(ReviewComment comment)
        {
            commentEntries.Remove(comment.entry);
            Destroy(comment.entry.gameObject);
            DeselectComment();
            SortEntries();
            FillData();
        }

        private void SortEntries()
        {
            commentEntries.Sort((c1, c2) => c1.StartTick.CompareTo(c2.StartTick));
            int index = 0;
            foreach (CommentEntry ce in commentEntries)
            {
                ce.Index = index;
                index++;
            }
        }
        public void Load()
        {
            string reviewDirectory = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "reviews");
            string path = StandaloneFileBrowser.OpenFilePanel("Select review file", reviewDirectory, "review", false).FirstOrDefault();
            if (File.Exists(path) && path.Contains(".review")) LoadContainer(path);
            else NotificationCenter.SendNotification($"Review file doesn't exist", NotificationType.Warning);
            StartCoroutine(UpdateScroller(1f));
        }

        private IEnumerator UpdateScroller(float newPos)
        {
            if (!commentListPanel.activeSelf) yield break;
            yield return new WaitForEndOfFrame();
            scroller.verticalNormalizedPosition = newPos;
        }

        public void SetMode(ReviewMode mode)
        {
            bool isReadMode = mode == ReviewMode.Read;
            commentTypeDrop.gameObject.SetActive(!isReadMode);
            commentTypeText.gameObject.SetActive(isReadMode);
            commentField.interactable = !isReadMode;
            writeModeButtonsPanel.SetActive(!isReadMode);
            readModeButtonsPanel.SetActive(isReadMode);
            writeSidePanel.gameObject.SetActive(!isReadMode);
            modeText.text = isReadMode ? "Read Mode" : "Write Mode";
            SelectedMode = mode;
        }

        public void SelectCues(bool selectMode)
        {
            selectCuesPanel.SetActive(selectMode);
            //window.SetActive(!selectMode);
            ShowWindow(!selectMode);
            EnableBottomBarButtons(!selectMode);
        }

        private void EnableBottomBarButtons(bool enable)
        {
            modeSelect.EnableButtons(enable);
        }

        public void ToggleMode()
        {
            SetMode(SelectedMode == ReviewMode.Read ? ReviewMode.Write : ReviewMode.Read);
        }

        void LoadContainer(string path)
        {
            if (File.Exists(path))
            {
                var container = ReviewContainer.Read(path);
                if (VerifyReview(container, out string error))
                {
                    foreach (CommentEntry entry in commentEntries) Destroy(entry.gameObject);
                    commentEntries.Clear();
                    currentComment = new ReviewComment();

                    loadedContainer = container;
                    if(loadedContainer.comments.Count > 1) loadedContainer.comments.Sort((c1, c2) => c1.tick.CompareTo(c2.tick));
                    NotificationCenter.SendNotification($"Loaded {loadedContainer.reviewAuthor}'s review", NotificationType.Success);
                    authorField.text = loadedContainer.reviewAuthor;
                    authorText.text = loadedContainer.reviewAuthor;
                    SetMode(ReviewMode.Read);

                    foreach (ReviewComment comment in loadedContainer.comments)
                    {
                        CreateCommentEntry(comment);
                    }
                    NextComment();
                }
                else NotificationCenter.SendNotification(error, NotificationType.Warning);

            }
            else loadedContainer = new ReviewContainer();
        }

        public void ClearContainer()
        {
            EditorNotes.DeselectAllTargets();
            foreach(var entry in commentEntries)
            {
                Destroy(entry.gameObject);
            }
            commentField.text = "";
            authorField.text = "";
            loadedContainer = new();
        }

        public void OnAuthorNameChanged()
        {
            loadedContainer.reviewAuthor = authorField.text;
            authorText.text = authorField.text;
        }

        public void Export(bool openFolder = true)
        {
            if(loadedContainer.comments is null || loadedContainer.comments.Count == 0)
            {
                NotificationCenter.SendNotification($"Review doesn't have any comments.", NotificationType.Error);
                return;
            }
            loadedContainer.Export();
            if(openFolder) OpenReviewFolder();
            NotificationCenter.SendNotification($"Saved review!", NotificationType.Success, false);
        }

        public void ToggleComments()
        {
            bool active = !commentListPanel.activeSelf;
            commentListPanel.SetActive(active);
            toggleCommentsButton.SetText(active ? "Hide Comments" : "Show Comments");
        }

        public void ToggleCommentChecked()
        {
            currentComment.isChecked = !currentComment.isChecked;
            currentComment.entry.SetChecked(currentComment.isChecked);
            checkCommentButton.SetText(currentComment.isChecked ? "Uncheck Comment" : "Check Comment");
        }

        private bool isEditingSuggestion = false;
        public void EditSuggestion()
        {
            if (!currentComment.HasSelectedCues) return;
            isEditingSuggestion = !isEditingSuggestion;
            editSuggestionPanel.SetActive(isEditingSuggestion);
            ShowWindow(!isEditingSuggestion);
            EnableBottomBarButtons(!isEditingSuggestion);
            if (isEditingSuggestion)
            {
                if (currentComment.HasSuggestion)
                {
                    SpawnTargets(false);
                }
            }
            else
            {
                if(EditorNotes.HasSelectedNotes)
                {
                    var selectedCues = new List<Cue>();
                    foreach (Target target in EditorNotes.SelectedNotes)
                    {
                        selectedCues.Add(target.ToCue());
                    }
                    selectedCues.Sort((c1, c2) => c1.tick.CompareTo(c2.tick));
                    currentComment.suggestionCues = selectedCues.ToArray();
                    currentComment.entry.EnableSuggestion(true);
                }
                else
                {
                    currentComment.suggestionCues = null;
                    currentComment.entry.EnableSuggestion(false);
                }
                SpawnTargets(true);
            }
        }
        private bool isShowingSuggestion = false;
        public void ShowSuggestion(bool applySuggestion = false)
        {
            if (!currentComment.HasSuggestion) return;
            isShowingSuggestion = !isShowingSuggestion;
            viewSuggestionPanel.SetActive(isShowingSuggestion);
            ShowWindow(!isShowingSuggestion);
            EnableBottomBarButtons(!isShowingSuggestion);
            if (isShowingSuggestion)
            {
                SpawnTargets(false);
            }
            else if (!applySuggestion)
            {              
                SpawnTargets(true);                          
            }
        }

        private void SpawnTargets(bool original)
        {
            List<Cue> targetsToSpawn = (original ? currentComment.selectedCues : currentComment.suggestionCues).ToList();
            List<Cue> targetsToSelect = (original ? currentComment.suggestionCues : currentComment.selectedCues).ToList();
            EditorNotes.DeselectAllTargets();
            EditorNotes.SelectTargets(SelectTargets(targetsToSelect.First().tick, targetsToSelect.Last().tick).ToList());
            EditorTargets.DeleteSelectedTargets();

            foreach(Cue cue in targetsToSpawn)           
                EditorNotes.SelectTarget(EditorTargets.AddTargetFromAction(cue));
        }

        void OpenReviewFolder()
        {
            string arguments = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "reviews");
            string fileName = "explorer.exe";

            System.Diagnostics.Process.Start(fileName, arguments);
        }

        public enum ReviewMode
        {
            Read,
            Write
        }

        private bool fade = false;

        private void TogglePlayback()
        {
            if (SelectedMode != ReviewMode.Read) return;
            
            fade = !fade;
            EditorAudio.TogglePlay();
            windowCanvas.alpha = fade ? .4f : 1f;
            windowCanvas.interactable = !fade;
            if (fade)
            {
                KeybindManager.EnableMap(KeybindManager.Map.Timeline);
            }
            else
            {
                KeybindManager.DisableMap(KeybindManager.Map.Timeline);
            }
                EditorState.SetIsInUI(!fade);
        }

        private void Scrub(InputAction.CallbackContext obj)
        {
            EditorAudio.ScrubTimeline(obj.ReadValue<float>() < 0, false);
        }

        private bool VerifyReview(ReviewContainer container, out string message)
        {
            bool correctID = container.songID == EditorFile.AudicaFile.desc.songID;
            bool correctDifficulty = container.difficulty == DifficultyManager.Instance.LoadedDifficulty || container.difficulty == Difficulty.None;
            if (!correctID) message = "Review was made for a different song.";
            else if (!correctDifficulty) message = $"Review was made for {container.difficulty}.";
            else message = "";
            return correctID && correctDifficulty;
        }

        internal void EnableScrubbing(bool enable)
        {
            if (enable && !actions.Review.Scrub.enabled)
                actions.Review.Scrub.Enable();
            else if (!enable && actions.Review.Scrub.enabled)
                actions.Review.Scrub.Disable();
        }


        /// <summary>
        /// Enumerates targets within a tick range.
        /// </summary>
        IEnumerable<Target> SelectTargets(int startTick, int endTick) =>
            from target in EditorNotes.OrderedNotes
            where target.data.time.tick >= (ulong)startTick &&
                  target.data.time.tick <= (ulong)endTick
            select target;

        protected override void RegisterCallbacks()
        {
            actions.Review.TogglePlayback.performed += _ => TogglePlayback();
            actions.Review.Scrub.performed += Scrub;
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            ShowWindow(false);
        }

        protected override void SetRebindConfiguration(ref RebindConfiguration options, ReviewKeybinds myKeybinds)
        {
            options.SetAssetTitle("Review System").SetRebindable(false);
        }
    }

}