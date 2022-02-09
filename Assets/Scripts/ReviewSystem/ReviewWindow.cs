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
using Sirenix.Utilities;
using TMPro;
using System.Collections;
using NotReaper.Managers;
using NotReaper.Notifications;

namespace NotReaper.ReviewSystem
{
    //[RequireComponent(typeof(EditorInputDisabler))]
    public class ReviewWindow : MonoBehaviour
    {
        public static ReviewWindow Instance = null;
        public static bool IsOpen = false;
        #region References
        [SerializeField] private GameObject window;
        [SerializeField] private CanvasGroup windowCanvas;
        [SerializeField] private GameObject selectCuesPanel;
        [SerializeField] private GameObject editSuggestionPanel;
        [SerializeField] private GameObject viewSuggestionPanel;
        [SerializeField] private TMP_InputField commentField;
        [SerializeField] private TMP_InputField authorField;
        [SerializeField] private GameObject makeSuggestionButton;
        [SerializeField] private GameObject showSuggestionButton;
        [SerializeField] private TextMeshProUGUI authorText;
        [SerializeField] private TMP_Dropdown commentTypeDrop;
        [SerializeField] private TextMeshProUGUI commentTypeText;
        [SerializeField] private TextMeshProUGUI modeText;
        [SerializeField] private GameObject writeModeButtonsPanel;
        [SerializeField] private GameObject readModeButtonsPanel;
        [SerializeField] private TextMeshProUGUI checkCommentButtonText;
        [SerializeField] private GameObject writeSidePanel;
        [SerializeField] private List<GameObject> bottomBarButtons;
        [SerializeField] private TextMeshProUGUI toggleCommentsButtonText;
        [Space]
        [SerializeField] private GameObject commentListPanel;
        [SerializeField] private CommentEntry commentEntryPrefab;
        [SerializeField] private RectTransform commentListContent;
        [SerializeField] private ScrollRect scroller;
        #endregion

        private ReviewContainer loadedContainer = new ReviewContainer();
        private ReviewComment currentComment = new ReviewComment();
        public ReviewMode SelectedMode { get; set; } = ReviewMode.Read;

        private List<CommentEntry> commentEntries = new List<CommentEntry>();
        private Vector2 lastOpenPosition = Vector2.zero;
        private float lastScrollPosition = 1f;
        private void Awake()
        {
            if (Instance is null) Instance = this;
            else
            {
                Debug.LogWarning("ReviewWindow already exists.");
                return;
            }
        }

        bool init = false;
        private void Start()
        {
            SetMode(ReviewMode.Read);
            ShowWindow(false);
            makeSuggestionButton.SetActive(false);
            showSuggestionButton.SetActive(false);
            string exportFolder = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "reviews");
            if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
        }
        public void ShowWindow(bool show)
        {
            IsOpen = show;
            if (!show && init) lastOpenPosition = window.transform.localPosition;
            window.transform.localPosition = show ? lastOpenPosition : new Vector2(-4300f, 0f);
            if (!init) init = true;
            window.SetActive(show);
            if (!show)
            {
                foreach (GameObject go in bottomBarButtons) go.SetActive(true);
                lastScrollPosition = scroller.verticalNormalizedPosition;
            }
            else
            {
                if (isShowingSuggestion)
                {
                    ShowSuggestion();
                }
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
            Timeline.instance.DeselectAllTargets();
            currentComment = loadedContainer.comments[index];

            Cue firstCue = currentComment.selectedCues.FirstOrDefault();
            Cue lastCue = currentComment.selectedCues.LastOrDefault();

            foreach (Target target in SelectTargets(firstCue.tick, lastCue.tick))
                Timeline.instance.SelectTarget(target);

            StartCoroutine(Timeline.instance.AnimateSetTime(new QNT_Timestamp((ulong)firstCue.tick)));

            FillData();
            foreach (CommentEntry ce in commentEntries) ce.IsSelected = false;
            commentEntries[index].IsSelected = true;
            checkCommentButtonText.text = currentComment.isChecked ? "Uncheck Comment" : "Check Comment";
            currentComment.entry.SetChecked(currentComment.isChecked);
            makeSuggestionButton.SetActive(currentComment.HasSelectedCues);
            showSuggestionButton.SetActive(currentComment.HasSuggestion);
        }

        public void DeselectComment()
        {
            if(currentComment.entry != null) currentComment.entry.IsSelected = false;
            currentComment = new ReviewComment();
            makeSuggestionButton.SetActive(false);
            showSuggestionButton.SetActive(false);
            Timeline.instance.DeselectAllTargets();
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
            }
        }

        public void RemoveComment()
        {
            if (loadedContainer.comments.Contains(currentComment))
            {
                Timeline.instance.DeselectAllTargets();
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
            if(Timeline.instance.selectedNotes.Count == 0)
            {
                NotificationCenter.SendNotification("Couldn't save comment. No targets selected.", NotificationType.Warning);
                return;
            }
            var selectedCues = new List<Cue>();
            foreach (Target target in Timeline.instance.selectedNotes)
            {
                selectedCues.Add(target.ToCue());
            }
            selectedCues.Sort((c1, c2) => c1.tick.CompareTo(c2.tick));
            /*currentComment = new ReviewComment(selectedCues.ToArray(),
                commentField.text,
                (CommentType)commentTypeDrop.value);*/
            currentComment.selectedCues = selectedCues.ToArray();
            currentComment.description = commentField.text;
            currentComment.type = (CommentType)commentTypeDrop.value;
            
            if(!loadedContainer.comments.Contains(currentComment)) loadedContainer.comments.Add(currentComment);
            if(loadedContainer.comments.Count > 1) loadedContainer.comments.Sort((c1, c2) => c1.selectedCues.First().tick.CompareTo(c2.selectedCues.First().tick));
            string targetPlural = selectedCues.Count == 1 ? "target" : "targets";
            NotificationCenter.SendNotification($"Saved comment for {selectedCues.Count} {targetPlural}", NotificationType.Success);

            if (currentComment.entry is null) CreateCommentEntry(currentComment);
            else
            {
                currentComment.entry.UpdateEntry();
                SortEntries();
            }

            DeselectComment();
            FillData();
            Export(false);
            //StartCoroutine(UpdateScroller(0f));
        }

        public void NewComment()
        {
            DeselectComment();
            FillData();
        }

        public void CreateCommentEntry(ReviewComment comment)
        {
            if (!comment.HasSelectedCues) return;
            var entry = GameObject.Instantiate(commentEntryPrefab, commentListContent);
            entry.SetComment(comment);
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
            foreach (GameObject go in bottomBarButtons) go.SetActive(!selectMode);
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
                    if(loadedContainer.comments.Count > 1) loadedContainer.comments.Sort((c1, c2) => c1.selectedCues.First().tick.CompareTo(c2.selectedCues.First().tick));
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
            Timeline.instance.DeselectAllTargets();
            foreach(var entry in commentEntries)
            {
                Destroy(entry.gameObject);
            }
            commentField.text = "";
            authorField.text = "";
            loadedContainer = null;
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
            NotificationCenter.SendNotification($"Saved review!", NotificationType.Success);
        }

        public void ToggleComments()
        {
            bool active = !commentListPanel.activeSelf;
            commentListPanel.SetActive(active);
            toggleCommentsButtonText.text = active ? "Hide Comments" : "Show Comments";
        }

        public void ToggleCommentChecked()
        {
            if (!currentComment.HasSelectedCues) return;
            currentComment.isChecked = !currentComment.isChecked;
            currentComment.entry.SetChecked(currentComment.isChecked);
            checkCommentButtonText.text = currentComment.isChecked ? "Uncheck Comment" : "Check Comment";
        }

        private bool isEditingSuggestion = false;
        public void EditSuggestion()
        {
            if (!currentComment.HasSelectedCues) return;
            isEditingSuggestion = !isEditingSuggestion;
            editSuggestionPanel.SetActive(isEditingSuggestion);
            ShowWindow(!isEditingSuggestion);
            if (isEditingSuggestion)
            {
                if (currentComment.HasSuggestion)
                {
                    SpawnTargets(false);
                }
            }
            else
            {
                if(Timeline.instance.selectedNotes.Count > 0)
                {
                    var selectedCues = new List<Cue>();
                    foreach (Target target in Timeline.instance.selectedNotes)
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
            Timeline.instance.DeselectAllTargets();
            foreach(Target target in SelectTargets(targetsToSelect.First().tick, targetsToSelect.Last().tick))
            {
                Timeline.instance.SelectTarget(target);
            }
            Timeline.instance.DeleteTargets(Timeline.instance.selectedNotes);
            foreach(Cue cue in targetsToSpawn)
            {
                TargetData data = Timeline.instance.GetTargetDataForCue(cue);
                Target target = Timeline.instance.AddTargetFromAction(data);
                Timeline.instance.SelectTarget(target);
            }
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
        private void Update()
        {
            if (SelectedMode != ReviewMode.Read) return;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                fade = !fade;
                Timeline.instance.TogglePlayback();
                windowCanvas.alpha = fade ? .4f : 1f;
                windowCanvas.interactable = !fade;
            }
        }

        bool VerifyReview(ReviewContainer container, out string message)
        {
            bool correctID = container.songID == Timeline.audicaFile.desc.songID;
            bool correctDifficulty = container.difficulty == DifficultyManager.I.loadedIndex || container.difficulty == -1;
            if (!correctID) message = "Review was made for a different song.";
            else if (!correctDifficulty) message = $"Review was made for {DifficultyManager.I.GetDifficultyText(container.difficulty)}.";
            else message = "";
            return correctID && correctDifficulty;
        }


        /// <summary>
        /// Enumerates targets within a tick range.
        /// </summary>
        IEnumerable<Target> SelectTargets(int startTick, int endTick) =>
            from target in Timeline.orderedNotes
            where target.data.time.tick >= (ulong)startTick &&
                  target.data.time.tick <= (ulong)endTick
            select target;
    }

}