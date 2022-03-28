using NotReaper.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TargetPreview.Display;
using TargetPreview.Models;
using TargetPreview.Math;
using NotReaper.Timing;
using System.Linq;
using TargetPreview.ScriptableObjects;
using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using NotReaper.UI.Volume;

namespace NotReaper.MapPreview
{
    public class PreviewManager : NRMenu
    {
        [Header("Preview")]
        [SerializeField] private GameObject cam;
        [SerializeField] private GameObject dome;
        [SerializeField] private VisualConfig config;
        [SerializeField] private List<Material> skyboxes;
        [Space, Header("Menu")]
        [SerializeField] private CanvasGroup canvas;
        [SerializeField] private Slider songProgress;
        [SerializeField] private TextMeshProUGUI songTime;
        [SerializeField] internal CanvasGroup volumeButton;


        [NRInject] private TargetPool targetPool;
        [NRInject] private VolumeOverlay volume;
        private Dictionary<Targets.Target, Target> spawnedTargets = new();
        public bool isActive = false;
        private bool isDraggingSlider;

        protected override void Awake()
        {
            base.Awake();
            canvas.alpha = 0f;
            canvas.interactable = false;
            canvas.blocksRaycasts = false;
            songProgress.onValueChanged.AddListener(OnSliderValueChanged);
            cam.GetComponent<Skybox>().material = skyboxes[Random.Range(0, skyboxes.Count)];
        }

        public void LoadPreview()
        {
            cam.SetActive(true);
            dome.SetActive(true);
            config.leftHandColor = NRSettings.config.leftColor;
            config.rightHandColor = NRSettings.config.rightColor;
            UpdateProgress();
            CameraProvider.TargetPreviewMode();
            foreach(var target in spawnedTargets)
            {
                targetPool.Return(target.Value);
            }
            spawnedTargets.Clear();
            StartCoroutine(DoPreview());
        }

        private void UpdateProgress()
        {
            if (isDraggingSlider) return;
            songProgress.SetValueWithoutNotify(Timeline.instance.GetPercentagePlayed());
            UpdateText();
        }

        public void OpenVolumeOverlay()
        {
            volume.Show();
        }

        private void UpdateText()
        {
            float timestamp = Timeline.instance.TimestampToSeconds(Timeline.time);
            int minutes = Mathf.FloorToInt(timestamp / 60f);
            int seconds = Mathf.FloorToInt(timestamp % 60f);
            string strTargetMinutes = minutes < 10 ? "0" : "";
            strTargetMinutes += minutes;
            string strTargetSeconds = seconds < 10 ? "0" : "";
            strTargetSeconds += seconds;
            songTime.text = $"{strTargetMinutes}:{strTargetSeconds}";
        }

        private void StopPreview()
        {
            StopCoroutine(DoPreview());
            cam.SetActive(false);
            dome.SetActive(false);
            CameraProvider.ComposeMode();
            foreach(var target in spawnedTargets)
            {
                targetPool.Return(target.Value);
            }
            spawnedTargets.Clear();
        }

        private void SpawnTarget(Targets.Target target)
        {
            if (spawnedTargets.ContainsKey(target))
            {
                return;
            }

            var cue = target.ToCue();
            var position = TargetTransform.CalculateTargetTransform(cue.pitch, ((float)cue.gridOffset.x, (float)cue.gridOffset.y, cue.zOffset));
            TargetData data = new TargetData(ConvertBehavior(target.data.behavior), ConvertHandType(target.data.handType), (uint)target.data.time.tick, position);
            spawnedTargets.Add(target, targetPool.Take(data));
        }

        private void ReturnTarget(Targets.Target target)
        {
            if (!spawnedTargets.ContainsKey(target))
            {
                return;
            }
            targetPool.Return(spawnedTargets[target]);
            spawnedTargets.Remove(target);
        }

        private IEnumerator DoPreview()
        {
            while (isActive)
            {
                TargetManager.Time = Timeline.time.tick;
                UpdateProgress();
                foreach(var target in Timeline.orderedNotes)
                {
                    var start = Timeline.time - Relative_QNT.FromBeatTime(1);
                    var end = Timeline.time + Relative_QNT.FromBeatTime(4);
                    if(target.data.time >= start && target.data.time <= end)
                    {
                        SpawnTarget(target);
                    }
                    else
                    {
                        ReturnTarget(target);
                    }
                }
                yield return null;
            }
        }

        private float rotationSpeed = 1.5f;
        private Vector3 direction = Vector3.zero;
        private void LateUpdate()
        {
            if (isActive)
            {
                if(spawnedTargets.Count > 0)
                {
                    List<Vector3> positions = new();
                    foreach (var target in spawnedTargets)
                    {
                        positions.Add(target.Value.TargetData.transformData.position);
                    }
                    var averagePosition = positions.Aggregate(Vector3.zero, (acc, v) => acc + v) / positions.Count;
                    direction = averagePosition - cam.transform.position;
                    direction.Normalize();                    
                }

                if(direction != Vector3.zero)
                {
                    cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed);
                }
            }
        }

        private TargetBehavior ConvertBehavior(Models.TargetBehavior behavior) =>
            behavior switch
            {
                Models.TargetBehavior.Standard => TargetBehavior.Standard,
                Models.TargetBehavior.Sustain => TargetBehavior.Hold,
                Models.TargetBehavior.Vertical => TargetBehavior.Vertical,
                Models.TargetBehavior.Horizontal => TargetBehavior.Horizontal,
                Models.TargetBehavior.Melee => TargetBehavior.Melee,
                Models.TargetBehavior.Mine => TargetBehavior.Dodge,
                Models.TargetBehavior.Legacy_Pathbuilder => TargetBehavior.ChainStart,
                Models.TargetBehavior.ChainStart => TargetBehavior.ChainStart,
                Models.TargetBehavior.ChainNode => TargetBehavior.Chain,
                _ => TargetBehavior.Standard
            };

        private TargetHandType ConvertHandType(Models.TargetHandType hand) =>
            hand switch
            {
                Models.TargetHandType.Left => TargetHandType.Left,
                Models.TargetHandType.Right => TargetHandType.Right,
                Models.TargetHandType.Either => TargetHandType.Either,
                Models.TargetHandType.None => TargetHandType.None,
                _ => TargetHandType.Left
            };


        public override void Show()
        {
            canvas.DOFade(1f, .3f);
            canvas.blocksRaycasts = true;
            canvas.interactable = true;
            isActive = true;
            OnActivated();
            LoadPreview();

        }

        public override void Hide()
        {
            canvas.DOFade(0f, .3f);
            canvas.blocksRaycasts = false;
            canvas.interactable = false;
            StopPreview();
            isActive = false;
            OnDeactivated();
        }
        private bool wasPaused;
        public void OnSliderDragStart()
        {

        }
        public void OnSliderDragEnd()
        {
            if (wasPaused)
            {
                wasPaused = false;
                Timeline.instance.TogglePlayback();
            }
        }

        private void OnSliderValueChanged(float value)
        {
            if (!Timeline.instance.paused)
            {
                Timeline.instance.TogglePlayback();
                wasPaused = true;
            }
            Timeline.instance.JumpToPercent(value, true);
            UpdateText();
        }

        public override void ShowHelp()
        {
            throw new System.NotImplementedException();
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }
    }
}
