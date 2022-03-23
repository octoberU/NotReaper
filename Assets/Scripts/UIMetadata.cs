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

        public Components.NRButton generateDiff;
        public Components.NRButton loadThisDiff;
        public Components.NRButton deleteDiff;

        private int selectedDiff;
        private int diffPotentiallyGoingDelete = -1;

        public GameObject warningDeleteWindow;

        public TMP_Dropdown pitchDropdown;

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
            canvas.worldCamera = NRDependencyInjector.Get<Timeline>().menuCamera;
        }

        public void UpdateUIValues()
        {
            if (!Timeline.audicaLoaded) return;

            if (Timeline.desc.title != null)
            {
                titleField.text = Timeline.desc.title;
            }
            if (Timeline.desc.artist != null) artistField.text = Timeline.desc.artist;
            if (Timeline.desc.author != null) mapperField.text = Timeline.desc.author;
            if (Timeline.desc.moggSong != null) moggSongVolume.value = Timeline.audicaFile.mainMoggSong.volume.l;

            ChangeSelectedDifficulty(difficultyManager.loadedIndex);
            LoadCurrentDifficultyName(difficultyManager.loadedIndex);
            SetDifficultyIcons(difficultyManager.loadedIndex);


            float rating = DifficultyCalculator.GetRating(new Audica(Timeline.audicaFile.filepath), difficultyManager.loadedIndex);
            rating = (float)Math.Round(rating, 2);
            difficultyRating.text = rating.ToString();
            // Song end pitch event
            switch (Timeline.desc.songEndEvent)
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
            StartCoroutine(
                    GetAlbumArt($"file://" + Path.Combine(Application.dataPath, ".cache", "song.png")));

        }

        public void ApplyValues()
        {
            if (Timeline.desc == null) return;
            if (Timeline.audicaFile == null) return;
            if (String.IsNullOrEmpty(titleField.text)) return;

            Timeline.desc.title = titleField.text;
            Timeline.desc.artist = artistField.text;
            Timeline.desc.author = mapperField.text;
            if (String.IsNullOrEmpty(artText.text))
            {
                Timeline.desc.albumArt = "song.png";
            }
            Timeline.audicaFile.mainMoggSong.SetVolume(moggSongVolume.value, false);
        }

        public void TryCopyCuesToOther()
        {
            selectDiffWindow.SetActive(true);

            switch (difficultyManager.loadedIndex)
            {
                case 0:
                    selectDiffWindow.GetComponent<UIDifficulty>().DifficultyComingFrom("expert");
                    break;

                case 1:
                    selectDiffWindow.GetComponent<UIDifficulty>().DifficultyComingFrom("advanced");
                    break;

                case 2:
                    selectDiffWindow.GetComponent<UIDifficulty>().DifficultyComingFrom("standard");
                    break;

                case 3:
                    selectDiffWindow.GetComponent<UIDifficulty>().DifficultyComingFrom("beginner");
                    break;
            }
        }
        //Called when a user selects a new difficulty on the song info panel
        public void ChangeSelectedDifficulty(int index)
        {
            if (index == -1) return;

            selectedDiff = index;

            if (difficultyManager.loadedIndex == index)
            {
                loadThisDiff.interactable = false;
            }
            else
            {
                loadThisDiff.interactable = true;
            }

            if (difficultyManager.DifficultyExists(index))
            {
                generateDiff.interactable = false;
                deleteDiff.interactable = true;
                //deleteDiff.GetComponent<Image>().color = new Color(0.8039216f, 0.8039216f, 0.8039216f);
            }
            else
            {
                generateDiff.interactable = true;
                loadThisDiff.interactable = false;
                deleteDiff.interactable = false;
                //deleteDiff.GetComponent<Image>().color = new Color(0.8301887f, 0.8301887f, 0.8301887f, 0.2f);

            }
        }

        public void SetDifficultyName()
        {
            if (Timeline.desc == null) return;

            int difficultyIndex = difficultyManager.loadedIndex;

            if (difficultyIndex == -1) return;

            switch (difficultyIndex)
            {
                //expert
                case 0:
                    Timeline.desc.customExpert = DifficultyName.text;
                    break;

                //Advanced
                case 1:
                    Timeline.desc.customAdvanced = DifficultyName.text;
                    break;

                //Moderate
                case 2:
                    Timeline.desc.customModerate = DifficultyName.text;
                    break;

                //Beginner
                case 3:
                    Timeline.desc.customBeginner = DifficultyName.text;
                    break;

            }
        }

        public void LoadCurrentDifficultyName(int difficultyIndex)
        {
            if (Timeline.desc == null) return;

            if (difficultyIndex == -1) return;

            switch (difficultyIndex)
            {
                //expert
                case 0:
                    DifficultyName.text = Timeline.desc.customExpert;
                    break;

                //Advanced
                case 1:
                    DifficultyName.text = Timeline.desc.customAdvanced;
                    break;

                //Moderate
                case 2:
                    DifficultyName.text = Timeline.desc.customModerate;
                    break;

                //Beginner
                case 3:
                    DifficultyName.text = Timeline.desc.customBeginner;
                    break;

            }
        }

        public void SetDifficultyIcons(int difficultyIndex)
        {
            expertDiffDisplay.sprite = difficultyManager.DifficultyExists(0) ? expertDiffSprite : noExpertDiffSprite;
            advancedDiffDisplay.sprite = difficultyManager.DifficultyExists(1) ? advancedDiffSprite : noAdvancedDiffSprite;
            standardDiffDisplay.sprite = difficultyManager.DifficultyExists(2) ? standardDiffSprite : noStandardDiffSprite;
            beginnerDiffDisplay.sprite = difficultyManager.DifficultyExists(3) ? beginnerDiffSprite : noBeginnerDiffSprite;

            switch (difficultyIndex)
            {
                //expert
                case 0:
                    expertDiffGlow.SetActive(true);
                    advancedDiffGlow.SetActive(false);
                    standardDiffGlow.SetActive(false);
                    beginnerDiffGlow.SetActive(false);
                    break;

                //Advanced
                case 1:
                    expertDiffGlow.SetActive(false);
                    advancedDiffGlow.SetActive(true);
                    standardDiffGlow.SetActive(false);
                    beginnerDiffGlow.SetActive(false);
                    break;

                //Standard
                case 2:
                    expertDiffGlow.SetActive(false);
                    advancedDiffGlow.SetActive(false);
                    standardDiffGlow.SetActive(true);
                    beginnerDiffGlow.SetActive(false);
                    break;

                //Beginner
                case 3:
                    expertDiffGlow.SetActive(false);
                    advancedDiffGlow.SetActive(false);
                    standardDiffGlow.SetActive(false);
                    beginnerDiffGlow.SetActive(true);
                    break;

            }

            expertDissolve.isDissolving = false;
            advancedDissolve.isDissolving = false;
            standardDissolve.isDissolving = false;
            beginnerDissolve.isDissolving = false;

        }

        public void ChangeEndPitch()
        {
            switch (pitchDropdown.value)
            {
                case 0:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_C";
                    break;

                case 1:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_C#";
                    break;

                case 2:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_D";
                    break;

                case 3:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_D#";
                    break;

                case 4:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_E";
                    break;

                case 5:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_F";
                    break;

                case 6:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_F#";
                    break;

                case 7:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_G";
                    break;

                case 8:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_G#";
                    break;

                case 9:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_A";
                    break;

                case 10:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_A#";
                    break;

                case 11:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_B";
                    break;

                case 12:
                    Timeline.desc.songEndEvent = "event:/song_end/song_end_nopitch";
                    break;
            }
        }

        public void TryDeleteDifficulty()
        {
            string diffName = "";
            if (selectedDiff == 0) diffName = "expert";
            if (selectedDiff == 1) diffName = "advanced";
            if (selectedDiff == 2) diffName = "standard";
            if (selectedDiff == 3) diffName = "beginner";
            diffPotentiallyGoingDelete = selectedDiff;
            warningDeleteWindow.GetComponentInChildren<TextMeshProUGUI>().text = String.Format("WARNING: This will remove ALL cues in {0}. Are you SURE you want to do this?", diffName);
            warningDeleteWindow.SetActive(true);
        }

        //After the confirmation message
        public void ActuallyDeleteDifficulty()
        {
            warningDeleteWindow.SetActive(false);
            difficultyManager.RemoveDifficulty(diffPotentiallyGoingDelete);
            if (diffPotentiallyGoingDelete == 0) expertDissolve.isDissolving = true;
            if (diffPotentiallyGoingDelete == 1) advancedDissolve.isDissolving = true;
            if (diffPotentiallyGoingDelete == 2) standardDissolve.isDissolving = true;
            if (diffPotentiallyGoingDelete == 3) beginnerDissolve.isDissolving = true;
            //UpdateUIValues();
        }

        public void GenerateDifficulty()
        {
            difficultyManager.GenerateDifficulty(selectedDiff);
            difficultyManager.LoadDifficulty(selectedDiff, true);
            UpdateUIValues();
        }

        public void LoadThisDiff()
        {
            difficultyManager.LoadDifficulty(selectedDiff, true);
            UpdateUIValues();
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
                UnityEngine.Debug.Log(request.error);
            }
            else
            {
                Texture2D tex = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width / 2, tex.height / 2));
                AlbumArtImg.GetComponent<Image>().overrideSprite = sprite;
                AlbumArtImg.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
                artText.text = "";
                Timeline.desc.albumArt = "song.png";
            }
            yield break;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        public override void Show()
        {
            if (!Timeline.audicaLoaded) return;
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
                //ApplyValues();
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
            Timeline.instance.ReplaceSongAudio();
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
            EditorState.SelectMode(Models.EditorMode.Compose);
        }

        public void ExportAsCues()
        {
            string diff;
            switch (DifficultyManager.I.loadedIndex)
            {
                case 0:
                    diff = "Expert";
                    break;
                case 1:
                    diff = "Advanced";
                    break;
                case 2:
                    diff = "Standard";
                    break;
                default:
                    diff = "Easy";
                    break;


            }
            string fileName = Path.GetFileName(Timeline.audicaFile.filepath)?.Replace(".audica", "");
            fileName = fileName + "_NRExport-" + diff + ".cues";

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


            //Ensure all chains are generated
            List<TargetData> nonGeneratedNotes = new List<TargetData>();
            foreach (Target note in Timeline.instance.notes)
            {
                if (note.data.behavior == TargetBehavior.Legacy_Pathbuilder && note.data.legacyPathbuilderData.createdNotes == false)
                {
                    nonGeneratedNotes.Add(note.data);
                }
            }

            foreach (var data in nonGeneratedNotes)
            {
                ChainBuilder.GenerateChainNotes(data);
            }
            /*
            CueFile export = new CueFile();
            export.cues = new List<Cue>();
            export.NRCueData = new NRCueData();

            foreach (Target target in Timeline.orderedNotes) {

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
            Timeline.instance.SortOrderedList();
            foreach (Target t in Timeline.orderedNotes)
            {
                if (t.data.behavior == TargetBehavior.Legacy_Pathbuilder) continue;
                cues.Add(t.ToCue());
            }

            if (Timeline.audicaFile.desc.bakedzOffset)
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
            Hide();
        }
    }

}