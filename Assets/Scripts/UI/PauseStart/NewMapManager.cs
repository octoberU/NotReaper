using DG.Tweening;
using NotReaper;
using NotReaper.IO;
using NotReaper.Models;
using NotReaper.Timing;
using NotReaper.Tools;
using NotReaper.UI;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using NotReaper.UI.Components;
using NotReaper.Genres;
namespace NotReaper.UI
{
    public class NewMapManager : MonoBehaviour
    {
        #region Metadata
        [Header("Metadata")]
        [SerializeField] private TextMeshProUGUI loadAudioText;
        [Space]
        [SerializeField] private TextMeshProUGUI loadTempoText;
        [Space]
        [SerializeField] private NRIconInputField songNameInput;
        [SerializeField] private NRIconInputField artistNameInput;
        [SerializeField] private NRIconInputField mapperNameInput;
        [SerializeField] private NRDropdown keyDropdown;
        #endregion

        #region Difficulty
        [Space, Header("Difficulty")]
        [Header("Expert")]
        [SerializeField] private Image expertDisplay;
        [SerializeField] private Sprite expertSprite;
        [SerializeField] private Sprite noExpertSprite;
        [SerializeField] private TextMeshProUGUI expertText;
        [Space, Header("Advanced")]
        [SerializeField] private Image advancedDisplay;
        [SerializeField] private Sprite advancedSprite;
        [SerializeField] private Sprite noAdvancedSprite;
        [SerializeField] private TextMeshProUGUI advancedText;
        [Space, Header("Standard")]
        [SerializeField] private Image standardDisplay;
        [SerializeField] private Sprite standardSprite;
        [SerializeField] private Sprite noStandardSprite;
        [SerializeField] private TextMeshProUGUI standardText;
        [Space, Header("Beginner")]
        [SerializeField] private Image beginnerDisplay;
        [SerializeField] private Sprite beginnerSprite;
        [SerializeField] private Sprite noBeginnerSprite;
        [SerializeField] private TextMeshProUGUI beginnerText;
        #endregion

        #region Album Art
        [Space, Header("Album Art")]
        [SerializeField] private Image albumArt;
        [SerializeField] private TextMeshProUGUI albumArtText;
        #endregion

        #region Genre and BPM
        [Space, Header("Genre and BPM")]
        [SerializeField] private GenrePicker genrePicker;
        [SerializeField] private NRIconInputField bpmInput;
        [SerializeField] private NRInputField numeratorInput;
        [SerializeField] private NRInputField denominatorInput;
        #endregion

        #region Overlay
        [SerializeField] private CanvasGroup loadingOverlay;
        #endregion

        #region Fields
        Process ffmpeg = new Process();
        private bool isMp3;
        private string loadedSong = "";
        private string loadedMidi = "";
        private string loadedArt = "";
        private float moggSongVolume = -5;
        private float defaultBpm = 150;
        private int defaultNumerator = 4;
        private int defaultDenominator = 4;
        private string songName = "";
        private string artistName = "";
        private string mapperName = "";
        private string genre = "";
        private List<string> tags = new();
        private string songEndEvent;
        private AudioClip audioFile;
        private Color expertColor = new Color(0.74118f, 0.15686f, 1.00000f);
        private Color advancedColor = new Color(0.91765f, 0.65098f, 0.05490f);
        private Color standardColor = new Color(0.16078f, 0.86275f, 0.93725f);
        private Color beginnerColor = new Color(0.28235f, 0.87059f, 0.10980f);
        private Color disabledColor = new Color(0.8301887f, 0.8301887f, 0.8301887f, 0.8039216f);
        private int selectedDifficulty;
        [NRInject] private Timeline timeline;
        [NRInject] private NewMapView view;
        private TrimAudio trimAudio = new TrimAudio();
        #endregion
        
        
        private void ResetUIValues()
        {
            loadAudioText.SetText("no audio loaded");
            loadTempoText.SetText("no tempo loaded");
            songNameInput.text = "";
            artistNameInput.text = "";
            keyDropdown.SetValueWithoutNotify(0);
            albumArt.sprite = null;
            albumArtText.SetText("no image loaded");
            genrePicker.ResetUI();
            bpmInput.text = "";
            denominatorInput.text = "";
            numeratorInput.text = "";
            view.GoBack();
        }
        

        private DifficultyUI difficultyUI;

        private class DifficultyUI
        {
            public Difficulty expert;
            public Difficulty advanced;
            public Difficulty standard;
            public Difficulty beginner;

            private List<Difficulty> difficulties = new List<Difficulty>();

            public DifficultyUI(Difficulty expert, Difficulty advanced, Difficulty standard, Difficulty beginner)
            {
                this.expert = expert;
                this.advanced = advanced;
                this.standard = standard;
                this.beginner = beginner;

                difficulties.Add(expert);
                difficulties.Add(advanced);
                difficulties.Add(standard);
                difficulties.Add(beginner);
            }

            public Difficulty GetDifficulty(int diff)
            {
                return diff == 0 ? expert : diff == 1 ? advanced : diff == 2 ? standard : beginner;
            }

            public void SetAllDisabled()
            {
                foreach (var diff in difficulties) diff.SetDisabled();
            }

            public class Difficulty
            {
                public int index;
                public Image display;
                public Color color;
                public Color disabledColor;
                public Sprite sprite;
                public Sprite disabledSprite;
                public TextMeshProUGUI text;

                public Difficulty(int index, Image display, Color color, Color disabledColor, Sprite sprite, Sprite disabledSprite, TextMeshProUGUI text)
                {
                    this.index = index;
                    this.display = display;
                    this.color = color;
                    this.disabledColor = disabledColor;
                    this.sprite = sprite;
                    this.disabledSprite = disabledSprite;
                    this.text = text;
                }

                public void SetDisabled()
                {
                    display.sprite = disabledSprite;
                    text.color = disabledColor;
                }
                public void SetEnabled()
                {
                    display.sprite = sprite;
                    text.color = color;
                }
            }
        }

        private void Start()
        {
            DifficultyUI.Difficulty expert = new DifficultyUI.Difficulty(0, expertDisplay, expertColor, disabledColor, expertSprite, noExpertSprite, expertText);
            DifficultyUI.Difficulty advanced = new DifficultyUI.Difficulty(1, advancedDisplay, advancedColor, disabledColor, advancedSprite, noAdvancedSprite, advancedText);
            DifficultyUI.Difficulty standard = new DifficultyUI.Difficulty(2, standardDisplay, standardColor, disabledColor, standardSprite, noStandardSprite, standardText);
            DifficultyUI.Difficulty beginner = new DifficultyUI.Difficulty(3, beginnerDisplay, beginnerColor, disabledColor, beginnerSprite, noBeginnerSprite, beginnerText);
            difficultyUI = new DifficultyUI(expert, advanced, standard, beginner);

            string ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg.exe");

            if ((Application.platform == RuntimePlatform.LinuxEditor) || (Application.platform == RuntimePlatform.LinuxPlayer))
                ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg");

            if ((Application.platform == RuntimePlatform.OSXEditor) || (Application.platform == RuntimePlatform.OSXPlayer))
                ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpegOSX");

            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ffmpeg.StartInfo.FileName = ffmpegPath;

            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.WorkingDirectory = Path.Combine(Application.streamingAssetsPath, "FFMPEG");

        }

        public void UpdateUIVales()
        {
            if (NRSettings.config.savedMapperName != null)
            {
                mapperNameInput.text = NRSettings.config.savedMapperName;
            }
        }

        private Coroutine loadAudioRoutine = null;
        public void SelectAudio()
        {
            if (loadAudioRoutine != null)
            {
                StopCoroutine(loadAudioRoutine);
            }
            loadAudioRoutine = StartCoroutine(DoLoadAudio());
        }

        private IEnumerator DoLoadAudio()
        {
            var compatible = new[] { new ExtensionFilter("Compatible Audio Types", "mp3", "ogg", "flac") };
            //string[] paths = StandaloneFileBrowser.OpenFilePanel("Select music track", Path.Combine(Application.persistentDataPath), compatible, false);
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select music track", directory: PlayerPrefs.GetString("lastSong"), compatible, false);
            if (paths is null || paths.Length == 0) yield break;
            var filePath = paths[0];
            PlayerPrefs.SetString("lastSong", Path.GetDirectoryName(filePath));
            ShowOverlay();
            bool ffmpegFinished = false;
            var waitItem = new WaitUntil(() => ffmpegFinished);
            if (filePath != null)
            {
                // if user loads mp3 or flac instead of ogg, do the conversion first
                if (paths[0].EndsWith(".mp3") || paths[0].EndsWith(".flac"))
                {
                    UnityEngine.Debug.Log(String.Format("-y -i \"{0}\" -map 0:a \"{1}\"", paths[0], "converted.ogg"));
                    ffmpeg.StartInfo.Arguments =
                        String.Format("-y -i \"{0}\" -map 0:a \"{1}\"", paths[0], "converted.ogg");
                    ffmpeg.EnableRaisingEvents = true;
                    ffmpeg.StartInfo.CreateNoWindow = true;
                    ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    ffmpeg.Exited += (obj, args) => ffmpegFinished = true;
                    ffmpeg.Start();
                    //ffmpeg.WaitForExit();
                    filePath = "converted.ogg";

                    isMp3 = true;
                    yield return waitItem;
                    yield return StartCoroutine(
                        GetAudioClip($"file://" + Path.Combine(Application.streamingAssetsPath, "FFMPEG", filePath)));
                }

                else
                {
                    isMp3 = false;
                    yield return StartCoroutine(GetAudioClip(filePath));
                }

                loadAudioText.text = System.IO.Path.GetFileName(paths[0]);
                loadedSong = paths[0];
                GrabMetadata();
            }
            HideOverlay();
        }

        private void GrabMetadata()
        {
            string retMessage = string.Empty;
            ffmpeg.StartInfo.RedirectStandardError = true;
            ffmpeg.StartInfo.Arguments =
            String.Format("-i \"{0}\" -filter:a volumedetect -f null /dev/null", loadedSong);
            ffmpeg.Start();
            retMessage = ffmpeg.StandardError.ReadToEnd();
            ffmpeg.WaitForExit();
            ffmpeg.Close();

            var outputFile = Path.Combine(Application.streamingAssetsPath, "Ogg2Audica", "audioOutput.txt");
            File.Delete(outputFile);
            File.WriteAllText(outputFile, retMessage);

            string max_volume = string.Empty; // Pulling max_volume line
            string songNameMeta = string.Empty; // Pulling meta
            string artistMeta = string.Empty;
            string bpmMeta = string.Empty;
            foreach (var line in File.ReadLines(outputFile))
            {
                if (string.IsNullOrEmpty(max_volume) && line.ToLower().Contains("max_volume:"))
                {
                    max_volume = line;
                }
                if (string.IsNullOrEmpty(songNameMeta) && line.ToLower().Contains(" title"))
                {
                    songNameMeta = line;
                }
                if (string.IsNullOrEmpty(artistMeta) && line.ToLower().Contains(" artist"))
                {
                    artistMeta = line;
                }

                if (string.IsNullOrEmpty(bpmMeta) && line.ToLower().Contains("bpm"))
                {
                    bpmMeta = line;
                }
            }

            if (NRSettings.config.autoSongVolume)
            {
                string normalized_db = max_volume.Split(' ')[4]; // Grab only the number

                float.TryParse(normalized_db, out float foundVolume); // Crappy maths
                if (foundVolume > 0)
                {
                    foundVolume *= -1;
                }
                else if (foundVolume < 0)
                {
                    foundVolume = Mathf.Abs(foundVolume);
                }

                moggSongVolume = foundVolume - Mathf.Abs(moggSongVolume);
            }

            if (!string.IsNullOrEmpty(songNameMeta))
            {
                string songNameMetaOnly = songNameMeta.Split(':')[1];
                songNameMetaOnly = songNameMetaOnly.TrimStart(' ');
                songNameInput.text = songNameMetaOnly;
            }
            if (!string.IsNullOrEmpty(artistMeta))
            {
                string artistMetaOnly = artistMeta.Split(':')[1];
                artistMetaOnly = artistMetaOnly.TrimStart(' ');
                
                int lastSeparator = artistMetaOnly.LastIndexOf(';');
                if (lastSeparator == -1)
                    lastSeparator = artistMetaOnly.LastIndexOf('/');

                if (lastSeparator >= 0)
                {
                    artistMetaOnly = artistMetaOnly.Remove(lastSeparator, 1);
                    artistMetaOnly = artistMetaOnly.Insert(lastSeparator, " & ");
                }

                artistMetaOnly = artistMetaOnly.Replace(";", ", ");
                artistMetaOnly = artistMetaOnly.Replace("/", ", ");

                artistNameInput.text = artistMetaOnly;
            }

            if (!string.IsNullOrEmpty(bpmMeta))
            {
                string bpmMetaOnly = bpmMeta.Split(':')[1];
                bpmMetaOnly = bpmMetaOnly.TrimStart(' ');
                
                if (float.TryParse(bpmMetaOnly, out var bpmParse))
                    bpmInput.text = Mathf.RoundToInt(bpmParse).ToString();
            }
        }

        public void SelectTempo()
        {
            var compatible = new[] { new ExtensionFilter("MIDI", "mid") };
            //string[] paths = StandaloneFileBrowser.OpenFilePanel("Select midi tempo map", Path.Combine(Application.persistentDataPath), compatible, false);
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select midi tempo map", PlayerPrefs.GetString("lastMidi"), compatible, false);
            if (paths is null || paths.Length == 0) return;
            var filePath = paths[0];
            PlayerPrefs.SetString("lastMidi", Path.GetDirectoryName(filePath));
            if (filePath != null)
            {
                loadTempoText.text = System.IO.Path.GetFileName(paths[0]);
                loadedMidi = paths[0];
            }
        }

        public void SelectAlbumArt()
        {
            var compatible = new[] { new ExtensionFilter("Compatible Image Types", "png", "jpeg", "jpg") };
            //string[] paths = StandaloneFileBrowser.OpenFilePanel("Select album art", Path.Combine(Application.persistentDataPath), compatible, false);
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select album art", PlayerPrefs.GetString("lastAlbumArt"), compatible, false);
            if (paths is null || paths.Length == 0) return;
            var filePath = paths[0];
            PlayerPrefs.SetString("lastAlbumArt", Path.GetDirectoryName(filePath));
            if (filePath != null)
            {

                UnityEngine.Debug.Log(String.Format("-y -i \"{0}\" -vf scale=256:256 \"{1}\"", paths[0], "song.png"));
                ffmpeg.StartInfo.Arguments =
                    String.Format("-y -i \"{0}\" -vf scale=256:256 \"{1}\"", paths[0], "song.png");
                ffmpeg.Start();
                ffmpeg.WaitForExit();
                filePath = "song.png";


                StartCoroutine(
                   GetAlbumArt($"file://" + Path.Combine(Application.streamingAssetsPath, "FFMPEG", filePath)));

                albumArtText.text = "";
                loadedArt = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "song.png");

            }
        }

        public void SetDifficulty(int diff)
        {
            difficultyUI.SetAllDisabled();
            difficultyUI.GetDifficulty(diff).SetEnabled();
            selectedDifficulty = diff;

        }

        private IEnumerator GetAudioClip(string uri)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    UnityEngine.Debug.Log(www.error);
                }
                else
                {
                    yield return audioFile = DownloadHandlerAudioClip.GetContent(www);
                    timeline.LoadTimingMode(audioFile);
                    ApplyValues();

                    yield break;
                }
            }
        }

        private IEnumerator GetAlbumArt(string filepath)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(filepath);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.Log(request.error);
            }
            else
            {
                Texture2D tex = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width / 2, tex.height / 2));
                albumArt.sprite = sprite;
                albumArt.color = new Color32(255, 255, 255, 255);
            }
            yield break;
        }

        public void ApplyValues()
        {

            if (EditorAudio.IsPlaying)
            {
                EditorAudio.TogglePlay();
            }


            mapperName = mapperNameInput.text;
            songName = songNameInput.text;
           
            artistName = artistNameInput.text;
            genre = genrePicker.GetGenre();
            tags = genrePicker.GetTags();
            //songEndEvent = KeyScraper.GetSongEndEvent(artistNameInput.text, songNameInput.text);
            songEndEvent = UIMetadata.GetEndPitchEvent(keyDropdown.value);
            float.TryParse(bpmInput.text, out float bpm);
            if (bpm != 0f) defaultBpm = bpm;

            int.TryParse(numeratorInput.text, out int numerator);
            if (numerator != 0) defaultNumerator = numerator;

            int.TryParse(denominatorInput.text, out int denominator);
            if (denominator != 0) defaultDenominator = denominator;

            //timeline.SetTimingModeStats(Constants.MicrosecondsPerQuarterNoteFromBPM(defaultBpm), 0);
            UnityEngine.Debug.Log("Commented out this timing mode stats thingy. Uncomment again if buggy");

            CheckAllUIFilled();

        }
        public void GenerateOgg()
        {
            ApplyValues();

            if (!CheckAllUIFilled())
                return;

            StartCoroutine(DoGenerateOgg());
        }

        private IEnumerator DoGenerateOgg()
        {
            /*ApplyValues();
            if (!CheckAllUIFilled())
            {
                yield break;
            }*/

            Difficulty difficulty = (Difficulty)selectedDifficulty;
            ShowOverlay();
            if (isMp3)
            {
                yield return StartCoroutine(trimAudio.SetAudioLength(loadedSong, Path.Combine(Application.streamingAssetsPath, "FFMPEG", "output.ogg"), 0, defaultBpm, true));
                yield return StartCoroutine(AudicaGenerator.Generate(Path.Combine(Application.streamingAssetsPath, "FFMPEG", "output.ogg"), moggSongVolume,
                     RemoveSpecialCharacters(songName + "-" + mapperName), songName, artistName, defaultBpm, songEndEvent, mapperName, 0, loadedMidi,
                     loadedArt, difficulty, genre, tags, OnGenerationDone));
            }
            else
            {
                yield return StartCoroutine(AudicaGenerator.Generate(loadedSong, moggSongVolume, RemoveSpecialCharacters(songName + "-" + mapperName),
                    songName, artistName, defaultBpm, songEndEvent, mapperName, 0, loadedMidi, loadedArt, difficulty, genre, tags, OnGenerationDone));
            }           
        }

        private void OnGenerationDone(string path)
        {
            //StartCoroutine(timeline.LoadAudicaFile(false, path, defaultBpm, defaultNumerator, defaultDenominator, OnLoaded));
            EditorIO.LoadNewAudicaFile(path, OnLoaded, defaultBpm, defaultNumerator, defaultDenominator);
        }

        private void OnLoaded(bool success)
        {
            HideOverlay();
            if (success)
            {
                view.ContinueToBPM();
            }
            defaultBpm = 150;
            defaultNumerator = 4;
            defaultDenominator = 4;

            ResetUIValues();
        }

        private void ShowOverlay()
        {
            loadingOverlay.gameObject.SetActive(true);
        }

        private void HideOverlay()
        {
            loadingOverlay.gameObject.SetActive(false);
        }

        private string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_' || c == '-')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        public bool CheckAllUIFilled()
        {
            var workFolder = Path.Combine(Application.streamingAssetsPath, "Ogg2Audica");
            if (loadedSong != "" && mapperName != "" && songName != "" && artistName != "")
            {
                if (loadedMidi != null && loadedMidi.Length > 0)
                {
                    return true;
                }
                else
                {
                    loadedMidi = Path.Combine(workFolder, "songtemplate.mid");
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }

}
