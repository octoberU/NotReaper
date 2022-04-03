using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NotReaper;
using System;
using NotReaper.UserInput;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace NotReaper.BpmAlign
{
    public class BPMDragAlign : NRInput<BPMDragKeybinds>
    {
        private Camera timelineCam;

        private Transform waveform;
        private Vector2 startPosition;
        private Vector3 waveformPosition;
        private Vector3 originalWaveformPosition;
        private Vector3 startWaveformPosition;
        [NRInject] private PrecisePlayback playback;
        [NRInject] private Timeline timeline;
        [NRInject] private BPMDragView view;
        [SerializeField] private AudioWaveformVisualizer visualizer;
        private Relative_QNT lastAppliedBeatOffset;
        private float lastAppliedBeatOffsetTime;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            originalWaveformPosition = waveform.localPosition;
            timelineCam = CameraProvider.timeline;
        }

        private void OnEnable()
        {
            OnActivated();
            waveform = timeline.waveformVisualizer.transform;
            KeybindManager.onMouseDown += MouseDown;
            waveformPosition = waveform.position;
            startWaveformPosition = waveformPosition;
            //EnableKeybinds(false);
            
        }

        private void OnDisable()
        {
            KeybindManager.onMouseDown -= MouseDown;
            OnDeactivated();
            //EnableKeybinds(true);
        }

        private void EnableKeybinds(bool enable)
        {
            foreach (var action in view.GetEnabledActions())
            {
                if (enable)
                    KeybindManager.EnableKeybind(action.action.name);
                else
                    KeybindManager.DisableKeybind(action.action.name);
            }
            foreach(var map in view.GetEnabledMaps())
            {
                if (enable)
                    KeybindManager.EnableMap(map);
                else
                    KeybindManager.DisableMap(map);
            }
        }

        private void MouseDown(bool down)
        {
            if (down) StartDrag();
            else EndDrag();
        }

        private void StartDrag()
        {
            RaycastHit2D hit = Physics2D.Raycast(GetMousePosition(), timelineCam.transform.position - (Vector3)GetMousePosition(), .001f);

            if(hit.collider != null)
            {
                if(hit.collider.tag == "Timeline")
                {
                    if (EditorAudio.IsPlaying)
                    {
                        EditorAudio.TogglePlay();
                    }

                    startPosition = GetMousePosition();
                    StartCoroutine(Drag());
                }
            }           
        }

        public void ModifyAudio()
        {
            if (EditorAudio.IsPlaying) EditorAudio.TogglePlay();
            if (startWaveformPosition == waveform.position) return;
            var shiftBy = QNT_Duration.FromBeatTime(Mathf.Abs(waveform.localPosition.x) * EditorScale.ScaleAmount);
            Relative_QNT time = new Relative_QNT((long)shiftBy.tick * -1);
            waveform.localPosition = originalWaveformPosition;
            EditorTime.SetTime(0);
            PrecisePlayback.Instance.OffsetPlaybackTime(EditorTime.Time, new(0));
            EditorAudio.SetOffset(new(0));
            var bpm = Mathf.Round(Constants.OneMinuteInMicroseconds / EditorTempo.TempoChanges[0].microsecondsPerQuarterNote);
            var numBeats = bpm > 120f ? 8f : 4f;
            time += new Relative_QNT((long)Math.Round(Constants.PulsesPerQuarterNote * numBeats));
            lastAppliedBeatOffset = new(0);
            EditorAudioManager.Instance.RemoveOrAddTimeToAudio(time);
        }

        private IEnumerator Drag()
        {
            while (true)
            {
                Vector3 newPos = waveformPosition;
                newPos.x += GetMousePosition().x - startPosition.x;
                newPos.x = Mathf.Clamp(newPos.x, -50f, 0f);
                waveform.position = newPos;
                yield return null;
            }
        }

        private void EndDrag()
        {
            StopAllCoroutines();
            waveformPosition = waveform.position;

            var shiftBy = QNT_Duration.FromBeatTime(Mathf.Abs(waveform.position.x) * EditorScale.ScaleAmount);
            var time = new QNT_Duration(shiftBy.tick % Constants.QuarterNoteDuration.tick);
            var beatOffset = Mathf.FloorToInt((float)shiftBy.tick / Constants.QuarterNoteDuration.tick);
            Relative_QNT currentOffset = new((long)time.tick + (long)QNT_Duration.FromBeatTime(beatOffset).tick);
            var newTime = EditorTime.Time + (currentOffset - lastAppliedBeatOffset);
            PrecisePlayback.Instance.OffsetPlaybackTime(newTime, time);
            //Timeline.Instance.SetBeatOffset(new(beatOffset));
            lastAppliedBeatOffset = currentOffset;
            lastAppliedBeatOffsetTime = new QNT_Timestamp((ulong)currentOffset.tick).ToSeconds();
            EditorAudio.SetOffset(lastAppliedBeatOffset);
        }
        private Vector2 GetMousePosition()
        {
            return timelineCam.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>());        
        }

        protected override void RegisterCallbacks()
        {
            actions.DragAlign.Scrub.performed += (CallbackContext amount) 
                => EditorAudio.ScrubTimeline(amount.ReadValue<float>() < 0f, KeybindManager.Global.Modifier.IsCtrlDown());

            actions.DragAlign.TogglePlay.performed += _ 
                =>  EditorAudio.TogglePlay(KeybindManager.Global.Modifier.IsCtrlDown());
        }

        protected override void OnEscPressed(InputAction.CallbackContext context) { }

        protected override void SetRebindConfiguration(ref RebindConfiguration options, BPMDragKeybinds myKeybinds)
        {
            options.AddHiddenMaps(myKeybinds.DragAlign);
        }
    }

}
