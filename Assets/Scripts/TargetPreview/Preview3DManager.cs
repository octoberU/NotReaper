using System;
using NotReaper.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TargetPreview.Display;
using TargetPreview.Targets;
using TargetPreview.Math;
using NotReaper.Timing;
using System.Linq;
using TargetPreview.ScriptableObjects;
using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using NotReaper.UI.Volume;
using NotReaper.UI.Components;
using NotReaper.Tools;
using NotReaper.Audio;
using NotReaper.Managers;
using NotReaper.UI.Particles;
using NotReaper.UI;
using NotReaper.Modifiers.Preview;
using TargetPreview.Scripts;
using TargetPreview.Scripts.Targets;
using TargetPreview.Scripts.Targets.Extensions;
using NotReaper.Models;
using NotReaper.Modifiers;
using TargetBehavior = NotReaper.Models.TargetBehavior;
using TargetHandType = NotReaper.Models.TargetHandType;
namespace NotReaper.MapPreview
{
    public class Preview3DManager : NRMenu
    {
        #region References
        [Header("References")]
        [SerializeField] private GameObject camGO;
        [SerializeField] private GameObject dome;
        [SerializeField] internal VisualConfig config;
        [SerializeField] internal TargetPreview.ScriptableObjects.AssetContainer assets;
        [SerializeField] private CueDart[] cueDarts;
        [SerializeField] private List<Material> skyboxes;
        [Space, Header("Components")]
        [SerializeField] private ModifierPreview3D modifierPreview;
        [SerializeField] private CueManager cueManager;
        [SerializeField] private PreviewCameraController cameraController;
        [Space, Header("UI")]
        [SerializeField] private CanvasGroup canvas;
        [SerializeField] private Slider songProgress;
        [SerializeField] private TextMeshProUGUI songTime;
        [SerializeField] private TextMeshProUGUI songTick;
        [SerializeField] internal CanvasGroup volumeButton;
        [SerializeField] private NRDropdown skyboxSelector;
        [SerializeField] private NRToggle modifierToggle;
        [SerializeField] private NRToggle showGridToggle;
        [SerializeField] private Slider playbackSpeed;
        [SerializeField] private TextMeshProUGUI playbackSpeedText;
        [SerializeField] private RectTransform uiPanel;
        [SerializeField] private RectTransform visibilityButton;
        #endregion

        #region Members

        [NRInject] private VolumeOverlay volume;
        [NRInject] private ModifierPreviewer modifierPreviewer;
        [NRInject] private SidebarFunctions sidebar;
        [NRInject] private SoundEffects sounds;
        public bool IsActive { get; set; } = false;
        public bool HasActiveTargets => cueManager.ActiveCues.Count > 0;
        private bool isDraggingSlider;
        private Skybox skybox;
        private Camera cam;
        private InputAction mousePosition;

        private TelegraphPreset standardPreset;
        private TelegraphPreset sustainPreset;
        private TelegraphPreset angledPreset;

        #endregion

        #region Awake and Start
        protected override void Awake()
        {
            base.Awake();
            canvas.alpha = 0f;
            canvas.interactable = false;
            canvas.blocksRaycasts = false;
            skybox = camGO.GetComponent<Skybox>();
            playbackSpeed.onValueChanged.AddListener(OnPlaybackSpeedSliderValueChanged);
            songProgress.onValueChanged.AddListener(OnSliderValueChanged);
            standardPreset = assets.standardTelegraph;
            sustainPreset = assets.sustainTelegraph;
            angledPreset = assets.angleTelegraph;
            
            foreach(var dart in cueDarts)
                dart.gameObject.SetActive(false);
        }

        private void Start()
        {
            NRSettings.OnLoad(() =>
            {
                int index = NRSettings.config.skybox;
                SelectSkybox(index);
                skyboxSelector.SetValueWithoutNotify(index);
                skyboxSelector.onValueChanged.AddListener(SelectSkybox);
                showGridToggle.selected = NRSettings.config.showPreviewGrid;
            });
            mousePosition = KeybindManager.Global.MousePosition;
            modifierToggle.selected = false;
        }
        #endregion

        #region Preview
        public void LoadPreview()
        {
            camGO.SetActive(true);
            dome.SetActive(showGridToggle.selected);
            modifierToggle.selected = modifierPreviewer.isPlaying;
            Color.RGBToHSV(NRSettings.config.leftColor, out float h, out float s, out float v);
            s = 1f;
            config.leftHandColor = Color.HSVToRGB(h, s, v);
            Color.RGBToHSV(NRSettings.config.rightColor, out h, out s, out v);
            s = 1f;
            config.rightHandColor = Color.HSVToRGB(h, s, v);

            UpdateProgress();
            CameraProvider.TargetPreviewMode();


            List<Models.Cue> cues = new();
            cueManager.TargetCues = cues.AsTargetCues();
            foreach (var target in EditorNotes.OrderedNotes)
            {
                cues.Add(target.ToCue());
            }

            if (EditorFile.AudicaFile.desc.bakedzOffset)
            {
                cues = ZOffsetBaker.Instance.Bake(cues.ToList());
            }
            
            cueManager.TargetCues = cues.AsTargetCues();
            SetActiveCuesVisible(true);

            foreach (var dart in cueDarts)
            {
                dart.gameObject.SetActive(true);
            }
            
            StartCoroutine(DoPreview());
        }

        internal void ResetTelegraphs()
        {
            assets.standardTelegraph = standardPreset;
            assets.sustainTelegraph = sustainPreset;
            assets.angleTelegraph = angledPreset;
        }

        private IEnumerator DoPreview()
        {
            while (IsActive)
            {
                TimeController.SetTime(EditorTime.Time.ToMs());
                UpdateProgress();
                yield return null;
            }
        }
        private void StopPreview()
        {
            StopCoroutine(DoPreview());
            camGO.SetActive(false);
            dome.SetActive(false);
            
            foreach(var dart in cueDarts)
                dart.gameObject.SetActive(false);
            
            CameraProvider.ComposeMode();
            SetActiveCuesVisible(false);
        }

        private void SetActiveCuesVisible(bool visible)
        {
            foreach (var reference in cueManager.ActiveCues)
            {
                reference.target.gameObject.SetActive(visible);
            }
        }
        
        #endregion

        #region Base Class Overrides
        public override void Show()
        {
            GridParticles.StopEmitting();
            KeybindManager.onMouseDown += cameraController.MouseDown;
            //EditorState.OnEditorPaused += OnPlay;
            EditorAudio.onPlaybackToggled += OnPlay;
            canvas.DOFade(1f, .3f);
            canvas.blocksRaycasts = true;
            canvas.interactable = true;
            IsActive = true;
            cameraController.isActive = true;
            playbackSpeed.SetValueWithoutNotify(EditorAudio.PlaybackSpeed * 100f);
            playbackSpeedText.text = $"{ playbackSpeed.value }%";
            OnActivated();
            LoadPreview();
        }

        public override void Hide()
        {
            GridParticles.StopEmitting();
            KeybindManager.onMouseDown -= cameraController.MouseDown;
            //EditorState.OnEditorPaused -= OnPlay;
            EditorAudio.onPlaybackToggled -= OnPlay;
            NRSettings.SaveSettingsJson();
            canvas.DOFade(0f, .3f);
            canvas.blocksRaycasts = false;
            canvas.interactable = false;
            //sidebar.UpdatePlaybackSpeedSlider();
            StopPreview();
            IsActive = false;
            cameraController.isActive = false;
            OnDeactivated();
        }

        public override void ShowHelp()
        {
            if (EditorAudio.IsPlaying)
                EditorAudio.TogglePlay();

            NRHelp.Instance.ShowPreviewer();
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
            => Hide();
        #endregion

        #region Utility

        internal Target GetPreviewTarget(Targets.Target target)
        {
            return cueManager.ActiveCues.FirstOrDefault(c =>
                    c.cue.timeMs == target.ToCue().GetMsTime() && c.cue.behavior == ConvertToPreviewBehavior(target.data.behavior) && 
                    c.cue.handType == ConvertToPreviewHandType(target.data.handType)).target;
            //spawner.GetPreviewTarget(target);
        }
        internal void UpdateTargetVisuals()
        {
            foreach (var reference in cueManager.ActiveCues)
            {
                reference.target.UpdateVisuals(reference.target.TargetData);
            }
            
            foreach(var cueDart in cueDarts)
                cueDart.UpdateColor();
        }
        #endregion

        #region UI Callbacks

        private void OnPlaybackSpeedSliderValueChanged(float value)
        {
            value *= .01f;
            playbackSpeedText.text = $"{ playbackSpeed.value }%";
            EditorAudio.SetPlaybackSpeed(value);
        }

        private bool wasPaused;
        public void OnSliderDragEnd()
        {
            if (wasPaused)
            {
                wasPaused = false;
                EditorAudio.TogglePlay();
            }
        }

        private void OnSliderValueChanged(float value)
        {
            if (EditorAudio.IsPlaying)
            {
                EditorAudio.TogglePlay();
                wasPaused = true;
            }
            EditorAudio.ForceJumpToPercent(value);
            UpdateText();
        }
        private void UpdateText()
        {
            float timestamp = EditorTime.Seconds;
            int minutes = Mathf.FloorToInt(timestamp / 60f);
            int seconds = Mathf.FloorToInt(timestamp % 60f);
            string strTargetMinutes = minutes < 10 ? "0" : "";
            strTargetMinutes += minutes;
            string strTargetSeconds = seconds < 10 ? "0" : "";
            strTargetSeconds += seconds;
            songTime.text = $"{strTargetMinutes}:{strTargetSeconds}";
            songTick.text = EditorTime.Time.ToString();
        }

        public void SelectSkybox(int index)
        {
            skybox.material = skyboxes[index];
            modifierPreview.SkyboxMaterial = skyboxes[index];
            NRSettings.config.skybox = index;
            NRSettings.SaveSettingsJson();
        }

        public void ToggleGrid()
        {
            NRSettings.config.showPreviewGrid = showGridToggle.selected;
            dome.SetActive(showGridToggle.selected);
            NRSettings.SaveSettingsJson();
        }

        public void ToggleModifiers()
        {
            if (!modifierToggle.selected)
            {
                modifierPreviewer.StopPreview();
            }
            else if (EditorAudio.IsPlaying && modifierToggle.selected && !modifierPreviewer.isPlaying)
            {
                modifierPreviewer.StartPreview();
                //modifierPreviewer.UpdateModifierList(EditorTime.Time.tick);
            }
        }

        private void OnPlay(bool isPlaying)
        {
            if (!isPlaying) return;

            if (modifierToggle.selected && !modifierPreviewer.isPlaying)
            {
                //modifierPreviewer.UpdateModifierList(EditorTime.Time.tick);
                modifierPreviewer.StartPreview();
            }
        }

        private void UpdateProgress()
        {
            if (isDraggingSlider) return;
            songProgress.SetValueWithoutNotify(EditorAudio.SongPercentage);
            UpdateText();
        }

        public void OpenVolumeOverlay()
        {
            volume.Show();
        }

        private bool isVisible = true;
        private bool isPlayingAnimation;
        public void ToggleUIVisibility()
        {
            if (isPlayingAnimation) return;
            isPlayingAnimation = true;
            isVisible = !isVisible;
            Vector2 size = uiPanel.sizeDelta;
            var animation = DOTween.Sequence();
            if (isVisible)
            {
                size.y = 60f;
                animation.Append(uiPanel.DOSizeDelta(size, .3f).SetEase(Ease.OutBack));
                animation.Append(visibilityButton.DORotate(new Vector3(0f, 0f, 180f), .15f));
                sounds.PlaySound(SoundEffects.Sound.Open);
            }
            else
            {             
                size.y = 10f;
                animation.Append(uiPanel.DOSizeDelta(size, .3f).SetEase(Ease.InBack));
                animation.Append(visibilityButton.DORotate(Vector3.zero, .15f));
                sounds.PlaySound(SoundEffects.Sound.Close);
            }
            animation.OnComplete(() => isPlayingAnimation = false);
            animation.Play();
        }
        
                
        public static TargetPreview.Scripts.Targets.TargetCue ConvertToTargetCue(Cue cue)
            => new (tick: cue.tick, tickLength: cue.tickLength, pitch: cue.pitch, velocity: (int)cue.velocity,
                xOffset: (float)cue.gridOffset.x, yOffset: (float)cue.gridOffset.y, zOffset: cue.zOffset * 10f,
                handType: (TargetPreview.Targets.TargetHandType)cue.handType, behavior: (TargetPreview.Targets.TargetBehavior)cue.behavior,
                timeMs: cue.GetMsTime(), cue.GetEndMsTime(), new TargetCue[0]);

        private TargetPreview.Targets.TargetBehavior ConvertToPreviewBehavior(Models.TargetBehavior behavior) =>
            behavior switch
            {
                TargetBehavior.Standard => TargetPreview.Targets.TargetBehavior.Standard,
                TargetBehavior.Vertical => TargetPreview.Targets.TargetBehavior.Vertical,
                TargetBehavior.Horizontal => TargetPreview.Targets.TargetBehavior.Horizontal,
                TargetBehavior.Sustain => TargetPreview.Targets.TargetBehavior.Hold,
                TargetBehavior.ChainStart => TargetPreview.Targets.TargetBehavior.ChainStart,
                TargetBehavior.ChainNode => TargetPreview.Targets.TargetBehavior.Chain,
                TargetBehavior.Melee => TargetPreview.Targets.TargetBehavior.Melee,
                TargetBehavior.Mine => TargetPreview.Targets.TargetBehavior.Dodge,
                TargetBehavior.None => TargetPreview.Targets.TargetBehavior.Standard,
                _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior, null)
            };

        private TargetPreview.Targets.TargetHandType ConvertToPreviewHandType(Models.TargetHandType handType) =>
            handType switch
            {
                TargetHandType.Either => TargetPreview.Targets.TargetHandType.Either,
                TargetHandType.Right => TargetPreview.Targets.TargetHandType.Right,
                TargetHandType.Left => TargetPreview.Targets.TargetHandType.Left,
                TargetHandType.None => TargetPreview.Targets.TargetHandType.None,
                _ => throw new ArgumentOutOfRangeException(nameof(handType), handType, null)
            };

        #endregion

        public IEnumerable<Target> GetActivePreviewTargets()
        {
            List<Target> targets = new();
            foreach (var reference in cueManager.ActiveCues)
            {
                targets.Add(reference.target);
            }

            return targets;
        }
    }

    public static class TargetCueExtensions
    {
        public static TargetCue[] AsTargetCues(this IEnumerable<Models.Cue> cues)
        {

            var cuesSorted = 
                cues
                    .OrderBy(x => x.tick)
                    .ThenBy(x => (int)x.behavior)
                    .ThenBy(x => (int)x.handType)
                    .ToArray();
            
            List<TargetCue> output = new();
            Dictionary<Models.TargetHandType, List<TargetCue>> chainNodes = new()
            {
                { Models.TargetHandType.Left, new() },
                { Models.TargetHandType.Right, new() },
                { Models.TargetHandType.Either, new() },
                { Models.TargetHandType.None, new() },
            };
            
            for (var index = cuesSorted.Length - 1; index >= 0; index--)
            {
                var cue = cuesSorted[index];

                switch (cue.behavior)
                {
                    case Models.TargetBehavior.ChainNode:
                        chainNodes[cue.handType].Add(Preview3DManager.ConvertToTargetCue(cue));
                        break;
                    case Models.TargetBehavior.ChainStart:
                        TargetCue targetCue = Preview3DManager.ConvertToTargetCue(cue);
                        targetCue.children = chainNodes[cue.handType].OrderBy(x => x.timeMs).ToArray();
                        targetCue.timeEndMs = chainNodes.Any() && chainNodes[cue.handType].Any() ? chainNodes[cue.handType].First().timeEndMs : targetCue.timeEndMs;
                        output.Add(targetCue);
                        chainNodes[cue.handType].Clear();
                        break;
                    default:
                        output.Add(Preview3DManager.ConvertToTargetCue(cue));
                        break;
                }
            }

            return output
                .OrderBy(x => x.tick)
                .ThenBy(x => (int)x.behavior)
                .ThenBy(x => (int)x.handType)
                .ToArray();
        }
        
       
    }
   
}
