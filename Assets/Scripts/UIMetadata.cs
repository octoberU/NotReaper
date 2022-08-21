using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DG.Tweening;
using NotReaper.Managers;
using TMPro;
using SFB;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using DifficultyCalculation;
using AudicaTools;
using NotReaper.Tools.ErrorChecker;
using NotReaper.Downmap;
using NotReaper.UI.Components;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Modifier;
using Newtonsoft.Json;
using NotReaper.Notifications;
using NotReaper.Tools.ChainBuilder;
using Difficulty = NotReaper.Models.Difficulty;

namespace NotReaper.UI
{

    public class UIMetadata : NRMenu
    {

        public static UIMetadata Instance = null;
        [NRInject] private DifficultyManager difficultyManager;

        public CanvasGroup window;

        public List<Image> inputBoxLines = new List<Image>();
        public List<Image> inputBoxLinesCover = new List<Image>();


        public NRIconInputField titleField;
        public NRIconInputField artistField;
        public NRIconInputField mapperField;
        public TextMeshProUGUI difficultyRating;
        public Slider moggSongVolume;


        public GameObject selectDiffWindow;
        private UIDifficulty uiDifficulty;

        public NRButton generateDiff;
        public NRButton loadThisDiff;
        public NRButtonPrompt deleteDiff;

        private Difficulty selectedDiff;
        private Difficulty diffPotentiallyGoingDelete = Difficulty.None;

        public GameObject warningDeleteWindow;

        public NRDropdown pitchDropdown;

        public Image AlbumArtImg;
        public TextMeshProUGUI artText;
        public NRInputField DifficultyName;
        [Space]
        [Header("Icons")]
        public Image beginnerDiffDisplay;
        public Image standardDiffDisplay;
        public Image advancedDiffDisplay;
        public Image expertDiffDisplay;
        public Sprite beginnerDiffSprite;
        public Sprite standardDiffSprite;
        public Sprite advancedDiffSprite;
        public Sprite expertDiffSprite;
        public Sprite noBeginnerDiffSprite;
        public Sprite noStandardDiffSprite;
        public Sprite noAdvancedDiffSprite;
        public Sprite noExpertDiffSprite;
        public GameObject beginnerDiffGlow;
        public GameObject standardDiffGlow;
        public GameObject advancedDiffGlow;
        public GameObject expertDiffGlow;

        [NRInject] private ErrorChecker errorChecker;
        [NRInject] private DownmapUIManager downmapper;

        public DissolveController expertDissolve;
        public DissolveController advancedDissolve;
        public DissolveController standardDissolve;
        public DissolveController beginnerDissolve;

        public NRInputField mapVersionInput;

        public void Start()
        {
            if (Instance is null) Instance = this;
            else
            {
                UnityEngine.Debug.Log("Trying to create second UIMetadata instance.");
                return;
            }

            var t = transform;
            var position = t.localPosition;
            t.localPosition = new Vector3(0, position.y, position.z);
            window.alpha = 0f;
            gameObject.SetActive(false);
            Canvas canvas = gameObject.GetComponent<Canvas>();
            uiDifficulty = selectDiffWindow.GetComponent<UIDifficulty>();
            canvas.worldCamera = CameraProvider.menu;
            DifficultyManager.onDifficultyLoaded += (Difficulty _) => UpdateUIValues();
        }

        public void UpdateUIValues()
        {
            if (!gameObject.activeInHierarchy)
                return;
            
            if (!EditorFile.IsAudicaFileLoaded) return;

            if (EditorFile.SongDesc.title != null)
            {
                titleField.text = EditorFile.SongDesc.title;
            }
            if (EditorFile.SongDesc.artist != null) artistField.text = EditorFile.SongDesc.artist;
            if (EditorFile.SongDesc.author != null) mapperField.text = EditorFile.SongDesc.author;
            if (EditorFile.SongDesc.moggSong != null) moggSongVolume.SetValueWithoutNotify(EditorFile.AudicaFile.mainMoggSong.volume.l);
            mapVersionInput.text = EditorFile.SongDesc.version.ToString();
            ChangeSelectedDifficulty(difficultyManager.LoadedDifficulty);
            LoadCurrentDifficultyName(difficultyManager.LoadedDifficulty);
            SetDifficultyIcons(difficultyManager.LoadedDifficulty);

            float rating = DifficultyCalculator.GetRating(new Audica(EditorFile.AudicaFile.filepath), (int)difficultyManager.LoadedDifficulty);
            rating = (float)Math.Round(rating, 2);
            difficultyRating.text = rating.ToString("F");
            // Song end pitch event
            switch (EditorFile.SongDesc.songEndEvent)
            {
                case "event:/song_end/song_end_C":
                    pitchDropdown.value = 0;
                    break;

                case "event:/song_end/song_end_C#":
                    pitchDropdown.value = 1;
                    break;

                case "event:/song_end/song_end_D":
                    pitchDropdown.value = 2;
                    break;

                case "event:/song_end/song_end_D#":
                    pitchDropdown.value = 3;
                    break;

                case "event:/song_end/song_end_E":
                    pitchDropdown.value = 4;
                    break;

                case "event:/song_end/song_end_F":
                    pitchDropdown.value = 5;
                    break;

                case "event:/song_end/song_end_F#":
                    pitchDropdown.value = 6;
                    break;

                case "event:/song_end/song_end_G":
                    pitchDropdown.value = 7;
                    break;

                case "event:/song_end/song_end_G#":
                    pitchDropdown.value = 8;
                    break;

                case "event:/song_end/song_end_A":
                    pitchDropdown.value = 9;
                    break;

                case "event:/song_end/song_end_A#":
                    pitchDropdown.value = 10;
                    break;

                case "event:/song_end/song_end_B":
                    pitchDropdown.value = 11;
                    break;

                case "event:/song_end/song_end_nopitch":
                    pitchDropdown.value = 12;
                    break;
            }
            pitchDropdown.startIndex = pitchDropdown.value;

            StartCoroutine(
                    GetAlbumArt($"file://" + Path.Combine(Application.dataPath, ".cache", "song.png")));
            

        }

        public void ApplyValues()
        {
            if (EditorFile.SongDesc == null) return;
            if (EditorFile.AudicaFile == null) return;
            if (String.IsNullOrEmpty(titleField.text)) return;
            EditorFile.SongDesc.title = titleField.text;
            EditorFile.SongDesc.artist = artistField.text;
            EditorFile.SongDesc.author = mapperField.text;

            int.TryParse(mapVersionInput.text, out int version);
            if (version == 0) version = 1;
            EditorFile.SongDesc.version = version;
            if (String.IsNullOrEmpty(artText.text))
            {
                EditorFile.SongDesc.albumArt = "song.png";
            }
            EditorFile.AudicaFile.mainMoggSong.SetVolume(moggSongVolume.value, false);
        }

        private void SetTestplayTag()
        {
            var title = EditorFile.SongDesc.title;
            string tag = "[WIP]";
            bool containsTag = title.Contains(tag);
            bool isTestplay = EditorFile.SongDesc.testplay;
            if (isTestplay && !containsTag)
            {
                if (title.Length < 5 || title.Substring(0, 5) != tag)
                    title = tag + title;
            }
            else if (!isTestplay && containsTag)
            {
                title = title.Replace(tag, "");
            }

            EditorFile.SongDesc.title = title;
        }

        public void CreateTestplay()
        {
            if (string.IsNullOrEmpty(titleField.text) || EditorIO.IsSaving)
                return;


            EditorFile.SongDesc.testplay = true;
            SetTestplayTag();
            EditorIO.SaveMap(new System.Action(() => { OnTestplayCreated(); }));
        }

        private void OnTestplayCreated()
        {
            EditorFile.SongDesc.testplay = false;
            SetTestplayTag();

        }

        public void TryCopyCuesToOther()
        {
            selectDiffWindow.SetActive(true);
            uiDifficulty.DifficultyComingFrom(difficultyManager.LoadedDifficulty);
        }

        //Called when a user selects a new difficulty on the song info panel
        public void ChangeSelectedDifficulty(int difficulty)
            => ChangeSelectedDifficulty((Difficulty)difficulty);
        public void ChangeSelectedDifficulty(Difficulty difficulty)
        {
            if (difficulty == Difficulty.None)
                return;

            selectedDiff = difficulty;

            if (difficultyManager.LoadedDifficulty == difficulty)
            {
                loadThisDiff.interactable = false;
            }
            else
            {
                loadThisDiff.interactable = true;
            }

            if (difficultyManager.DifficultyExists(difficulty))
            {
                generateDiff.interactable = false;
                if(difficultyManager.LoadedDifficulty != difficulty)
                {
                    deleteDiff.interactable = true;
                    deleteDiff.SetPromptText($"do you really want to delete {difficulty}?");
                }
                else
                {
                    deleteDiff.interactable = false;
                }
            }
            else
            {
                generateDiff.interactable = true;
                loadThisDiff.interactable = false;
                deleteDiff.interactable = false;
            }
        }

        public void SetDifficultyName()
        {
            if (EditorFile.SongDesc == null) return;

            switch (difficultyManager.LoadedDifficulty)
            {
                case Difficulty.Expert:
                    EditorFile.SongDesc.customExpert = DifficultyName.text;
                    break;
                case Difficulty.Advanced:
                    EditorFile.SongDesc.customAdvanced = DifficultyName.text;
                    break;
                case Difficulty.Standard:
                    EditorFile.SongDesc.customModerate = DifficultyName.text;
                    break;
                case Difficulty.Beginner:
                    EditorFile.SongDesc.customBeginner = DifficultyName.text;
                    break;
                default:
                    break;
            }
        }

        public void LoadCurrentDifficultyName(Difficulty difficulty)
        {
            if (EditorFile.SongDesc == null) return;

            switch (difficulty)
            {
                case Difficulty.Expert:
                    DifficultyName.text = EditorFile.SongDesc.customExpert;
                    break;
                case Difficulty.Advanced:
                    DifficultyName.text = EditorFile.SongDesc.customAdvanced;
                    break;
                case Difficulty.Standard:
                    DifficultyName.text = EditorFile.SongDesc.customModerate;
                    break;
                case Difficulty.Beginner:
                    DifficultyName.text = EditorFile.SongDesc.customBeginner;
                    break;
                default:
                    break;
            }
        }

        public void SetDifficultyIcons(Difficulty difficulty)
        {
            expertDiffDisplay.sprite = difficultyManager.DifficultyExists(Difficulty.Expert) ? expertDiffSprite : noExpertDiffSprite;
            advancedDiffDisplay.sprite = difficultyManager.DifficultyExists(Difficulty.Advanced) ? advancedDiffSprite : noAdvancedDiffSprite;
            standardDiffDisplay.sprite = difficultyManager.DifficultyExists(Difficulty.Standard) ? standardDiffSprite : noStandardDiffSprite;
            beginnerDiffDisplay.sprite = difficultyManager.DifficultyExists(Difficulty.Beginner) ? beginnerDiffSprite : noBeginnerDiffSprite;

            expertDiffGlow.SetActive(difficulty == Difficulty.Expert);
            advancedDiffGlow.SetActive(difficulty == Difficulty.Advanced);
            standardDiffGlow.SetActive(difficulty == Difficulty.Standard);
            beginnerDiffGlow.SetActive(difficulty == Difficulty.Beginner);

            expertDissolve.isDissolving = false;
            advancedDissolve.isDissolving = false;
            standardDissolve.isDissolving = false;
            beginnerDissolve.isDissolving = false;

        }

        public static string GetEndPitchEvent(int index)
            => index switch
            {
                0 => "event:/song_end/song_end_C",
                1 => "event:/song_end/song_end_C#",
                2 => "event:/song_end/song_end_D",
                3 => "event:/song_end/song_end_D#",
                4 => "event:/song_end/song_end_E",
                5 => "event:/song_end/song_end_F",
                6 => "event:/song_end/song_end_F#",
                7 => "event:/song_end/song_end_G",
                8 => "event:/song_end/song_end_G#",
                9 => "event:/song_end/song_end_A",
                10 => "event:/song_end/song_end_A#",
                11 => "event:/song_end/song_end_B",
                12 => "event:/song_end/song_end_nopitch",
                _ => ""
            };

        public void ChangeEndPitch() => EditorFile.SongDesc.songEndEvent = GetEndPitchEvent(pitchDropdown.value);

        public void TryDeleteDifficulty()
        {
            diffPotentiallyGoingDelete = selectedDiff;
            warningDeleteWindow.GetComponentInChildren<TextMeshProUGUI>().text = String.Format("WARNING: This will remove ALL cues in {0}. Are you SURE you want to do this?", selectedDiff.ToString());
            warningDeleteWindow.SetActive(true);
        }

        //After the confirmation message
        public void ActuallyDeleteDifficulty()
        {
            warningDeleteWindow.SetActive(false);
            diffPotentiallyGoingDelete = selectedDiff;
            difficultyManager.RemoveDifficulty(diffPotentiallyGoingDelete);
            if (diffPotentiallyGoingDelete == Difficulty.Expert) expertDissolve.isDissolving = true;
            if (diffPotentiallyGoingDelete == Difficulty.Advanced) advancedDissolve.isDissolving = true;
            if (diffPotentiallyGoingDelete == Difficulty.Standard) standardDissolve.isDissolving = true;
            if (diffPotentiallyGoingDelete == Difficulty.Beginner) beginnerDissolve.isDissolving = true;
            NotificationCenter.SendNotification($"Deleted {diffPotentiallyGoingDelete}", NotificationType.Success);
            deleteDiff.interactable = false;
        }

        public void GenerateDifficulty()
        {
            difficultyManager.GenerateDifficulty(selectedDiff);
            difficultyManager.LoadDifficulty(selectedDiff, true);
        }

        public void LoadThisDiff()
        {
            EditorAudio.StopPlayback();
            difficultyManager.LoadDifficulty(selectedDiff, true);
            EditorFile.SetIsAudicaLoaded(true);
        }

        public void SelectAlbumArtFile() // Album art
        {

            var compatible = new[] { new ExtensionFilter("Compatible Image Types", "png", "jpeg", "jpg") };
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select album art", Path.Combine(Application.persistentDataPath), compatible, false);
            var filePath = paths[0];

            if (filePath != null)
            {
                Process ffmpeg = new Process();
                string ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg.exe");

                if ((Application.platform == RuntimePlatform.LinuxEditor) || (Application.platform == RuntimePlatform.LinuxPlayer))
                    ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg");

                if ((Application.platform == RuntimePlatform.OSXEditor) || (Application.platform == RuntimePlatform.OSXPlayer))
                    ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpegOSX");

                ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                ffmpeg.StartInfo.FileName = ffmpegPath;

                ffmpeg.StartInfo.CreateNoWindow = true;
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.RedirectStandardOutput = true;
                ffmpeg.StartInfo.WorkingDirectory = Path.Combine(Application.streamingAssetsPath, "FFMPEG");
                UnityEngine.Debug.Log(String.Format("-y -i \"{0}\" -vf scale=256:256 \"{1}\"", paths[0], "song.png"));
                ffmpeg.StartInfo.Arguments =
                    String.Format("-y -i \"{0}\" -vf scale=256:256 \"{1}\"", paths[0], "song.png");
                ffmpeg.Start();
                ffmpeg.WaitForExit();
                filePath = "song.png";


                StartCoroutine(
                   GetAlbumArt($"file://" + Path.Combine(Application.streamingAssetsPath, "FFMPEG", "song.png")));

                artText.text = "";

                string cachedArt = Path.Combine(Application.dataPath, ".cache", "song.png");

                File.Delete(cachedArt);
                File.Copy(Path.Combine(Application.streamingAssetsPath, "FFMPEG", filePath), cachedArt);

            }
        }

        public IEnumerator GetAlbumArt(string filepath)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(filepath);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                AlbumArtImg.GetComponent<Image>().overrideSprite = null;
                AlbumArtImg.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
                artText.text = "No Image loaded";
            }
            else
            {
                Texture2D tex = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width / 2, tex.height / 2));
                AlbumArtImg.GetComponent<Image>().overrideSprite = sprite;
                AlbumArtImg.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
                artText.text = "";
                EditorFile.SongDesc.albumArt = "song.png";
            }
            yield break;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        public override void Show()
        {
            if (!EditorFile.IsAudicaFileLoaded) return;
            OnActivated();

            //Set colors
            foreach (Image img in inputBoxLines)
            {
                img.color = NRSettings.config.leftColor;
            }

            foreach (Image img in inputBoxLinesCover)
            {
                img.color = NRSettings.config.rightColor;
            }
            window.gameObject.SetActive(true);
            UpdateUIValues();
            window.DOFade(1f, .3f);
        }
        public override void Hide()
        {
            window.DOFade(0f, .3f).OnComplete(() =>
            {
                window.gameObject.SetActive(false);
                OnDeactivated();
            });
        }

        public override void ShowHelp() { }

        public void OnSetPreviewClicked()
        {
            MiniTimeline.Instance.SetPreviewStartPointToCurrent();
        }

        public void OnErrorCheckClicked()
        {
            errorChecker.RunErrorCheck();
        }

        public void OnDownmapClicked()
        {
            downmapper.ShowWindow(true);
        }

        public void OnLoadAudioClicked()
        {
            EditorAudioManager.Instance.ReplaceSongAudio();
        }

        public void OnLoadSustainLeftClicked()
        {
            UISustainHandler.Instance.UpdateSustainTrackLeft(false);
        }

        public void OnDeleteSustainLeftClicked()
        {
            UISustainHandler.Instance.UpdateSustainTrackLeft(true);
        }

        public void OnLoadSustainRightClicked()
        {
            UISustainHandler.Instance.UpdateSustainTrackRight(false);
        }
        public void OnDeleteSustainRightClicked()
        {
            UISustainHandler.Instance.UpdateSustainTrackRight(true);
        }

        public void OnComposeClicked()
        {
            EditorState.SelectMode(EditorMode.Compose);
        }

        public void ExportAsCues()
        {
            string fileName = Path.GetFileName(EditorFile.AudicaFile.filepath)?.Replace(".audica", "");
            fileName = fileName + "_NRExport-" + difficultyManager.DifficultyString + ".cues";
            string path;



            if (!String.IsNullOrEmpty(NRSettings.config.cuesSavePath))
            {
                path = Path.Combine(NRSettings.config.cuesSavePath, fileName);
            }
            else
            {
                path = StandaloneFileBrowser.SaveFilePanel("Find community_maps/maps folder in Audica folder", Path.Combine(Application.dataPath, @"../"), fileName, "cues");
                if (String.IsNullOrEmpty(path)) return;

                NRSettings.config.cuesSavePath = Path.GetDirectoryName(path);
                NRSettings.SaveSettingsJson();
            }
            
            /*
            CueFile export = new CueFile();
            export.cues = new List<Cue>();
            export.NRCueData = new NRCueData();

            foreach (Target target in EditorData.OrderedNotes) {

               if (target.data.beatLength == 0) target.data.beatLength = Constants.SixteenthNoteDuration;

               if (target.data.behavior == TargetBehavior.Metronome) continue;

               var cue = NotePosCalc.ToCue(target, Timeline.offset);

               if(target.data.behavior == TargetBehavior.NR_Pathbuilder) {
                  export.NRCueData.pathBuilderNoteCues.Add(cue);
                  export.NRCueData.pathBuilderNoteData.Add(target.data.pathBuilderData);
                  continue;
               }

               export.cues.Add(cue);
            }
            */
            CueFile file = new CueFile();
            var cues = new List<Models.Cue>();
            file.NRCueData = new NRCueData();
            EditorNotes.SortOrderedNotes();
            foreach (Target t in EditorNotes.OrderedNotes)
            {
                if (t.data.behavior == TargetBehavior.Legacy_Pathbuilder) continue;
                cues.Add(t.ToCue());
            }

            if (EditorFile.AudicaFile.desc.bakedzOffset)
            {
                cues = ZOffsetBaker.Instance.Bake(cues);
            }

            file.cues = cues;
            var json = JsonConvert.SerializeObject(file, Formatting.Indented);
            File.WriteAllText(path, json);
            NotificationCenter.SendNotification("Cues exported!", NotificationType.Success);
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            OnComposeClicked();
        }
    }

}