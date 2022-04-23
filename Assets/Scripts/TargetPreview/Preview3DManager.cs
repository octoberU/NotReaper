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
using NotReaper.Modifier;
using System;
using UnityEngine.EventSystems;
using NotReaper.Tools;
using NotReaper.Audio;
using NotReaper.UI.Particles;
using NotReaper.UI;

namespace NotReaper.MapPreview
{
    public class Preview3DManager : NRMenu
    {
        #region References
        [Header("References")]
        [SerializeField] private GameObject camGO;
        [SerializeField] private GameObject dome;
        [SerializeField] internal VisualConfig config;
        [SerializeField] internal AssetContainer assets;
        [SerializeField] private List<Material> skyboxes;
        [Space, Header("Components")]
        [SerializeField] private ModifierPreview3D modifierPreview;
        [SerializeField] private PreviewSpawner spawner;
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
            spawner.ClearSpawnedTargets();
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
                TargetManager.Time = EditorTime.Time.tick;
                UpdateProgress();
                foreach(var target in EditorNotes.OrderedNotes)
                {
                    var start = EditorTime.Time - Relative_QNT.FromBeatTime(10);
                    var end = EditorTime.Time + Relative_QNT.FromBeatTime(10);
                    if(target.data.time >= start && target.data.time <= end)
                    {
                        float zOffset = modifierPreview.zOffsets.ContainsKey(target) ?
                            modifierPreview.zOffsets[target] : 0f;
                        spawner.SpawnTarget(target, zOffset);
                    }
                    else
                    {
                        spawner.ReturnTarget(target);
                    }
                }
                yield return null;
            }
        }
        private void StopPreview()
        {
            StopCoroutine(DoPreview());
            camGO.SetActive(false);
            dome.SetActive(false);
            CameraProvider.ComposeMode();
            spawner.ClearSpawnedChainConnectors();
            spawner.ClearSpawnedDualines();
            spawner.ClearSpawnedTargets();
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
        {
            Hide();
        }
        #endregion

        #region Utility
        internal Target GetPreviewTarget(Targets.Target target) => spawner.GetPreviewTarget(target);
        internal void UpdateTargetVisuals()
        {
            foreach (var target in spawner.GetSpawnedPreviewTargets())
                target.UpdateVisuals(target.TargetData);
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
        #endregion
    }
}
