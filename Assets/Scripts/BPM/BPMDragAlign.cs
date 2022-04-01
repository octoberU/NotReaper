using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NotReaper;
using System;

namespace NotReaper.BpmAlign
{
    public class BPMDragAlign : MonoBehaviour
    {
        private Camera cam;

        private Transform waveform;
        private Vector2 startPosition;
        private Vector3 waveformPosition;
        private Vector3 originalWaveformPosition;
        private Vector3 startWaveformPosition;
        [NRInject] private PrecisePlayback playback;
        [NRInject] private Timeline timeline;
        [SerializeField] private AudioWaveformVisualizer visualizer;

        private void Start()
        {
            originalWaveformPosition = waveform.localPosition;
        }

        private void OnEnable()
        {
            waveform = timeline.waveformVisualizer.transform;
            cam = timeline.timelineCamera.GetComponent<Camera>();
            KeybindManager.onMouseDown += MouseDown;
            waveformPosition = waveform.position;
            startWaveformPosition = waveformPosition;
            //originalWaveformPosition = waveformPosition;
        }

        private void OnDisable()
        {
            KeybindManager.onMouseDown -= MouseDown;
        }

        private void MouseDown(bool down)
        {
            if (down) StartDrag();
            else EndDrag();
        }

        private void StartDrag()
        {
            RaycastHit2D hit = Physics2D.Raycast(GetMousePosition(), cam.transform.position - (Vector3)GetMousePosition(), .001f);

            if(hit.collider != null)
            {
                if(hit.collider.tag == "Timeline")
                {
                    startPosition = GetMousePosition();
                    StartCoroutine(Drag());
                }
            }           
        }

        public void ModifyAudio()
        {
            if (!EditorState.IsPaused) Timeline.Instance.TogglePlayback();
            if (startWaveformPosition == waveform.position) return;
            var shiftBy = QNT_Duration.FromBeatTime(Mathf.Abs(waveform.localPosition.x) * EditorScale.ScaleAmount);
            Relative_QNT time = new Relative_QNT((long)shiftBy.tick * -1);
            waveform.localPosition = originalWaveformPosition;
            EditorTime.SetTime(0);
            Timeline.Instance.SetBPMDragOffset(new QNT_Timestamp(0));
            var bpm = Mathf.Round(Constants.OneMinuteInMicroseconds / EditorTempo.TempoChanges[0].microsecondsPerQuarterNote);
            var numBeats = bpm > 120f ? 8f : 4f;
            time += new Relative_QNT((long)Math.Round(Constants.PulsesPerQuarterNote * numBeats));
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
                //Timeline.instance.JumpToXInstantly(waveform.position.x - (GetMousePosition().x - startPosition.x));
                
                yield return null;
            }
        }

        private void EndDrag()
        {
            StopAllCoroutines();
            waveformPosition = waveform.position;

            var shiftBy = QNT_Duration.FromBeatTime(Mathf.Abs(waveform.position.x) * EditorScale.ScaleAmount);
            var time = new QNT_Timestamp(shiftBy.tick);
            EditorTime.SetTime(time);
            Timeline.Instance.SetBPMDragOffset(time);
        }

        private Vector2 GetMousePosition()
        {
            return cam.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>());        
        }
    }

}
