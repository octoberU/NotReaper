using NotReaper.Models;
using NotReaper.Modifier;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Audio
{
    public class EditorAudioPlayer : MonoBehaviour
    {
        private PrecisePlayback playback;
        private Relative_QNT offset = new(0);
        private float offsetSeconds = 0f;
        private const float JumpDuration = .25f;
        private bool isJumping;
        private bool queuePlay;
        private void Start()
        {
            playback = NRDependencyInjector.Get<PrecisePlayback>();
            EditorTime.onTimeChanged += UpdateSustainAudio;
        }
        /// <summary>
        /// Toggles playback of loaded audio.
        /// </summary>
        /// <param name="metronome">True if the metronome should be started.</param>
        public void TogglePlay(bool metronome = false)
        {
            if (isJumping)
            {
                queuePlay = true;
                return;
            }
            if (EditorAudio.IsPlaying)
            {
                if(metronome)
                    playback.StartMetronome();

                StartCoroutine(Play());   
            }
        }

        /// <summary>
        /// Jumps to the percentage in the song.
        /// </summary>
        /// <param name="percent">The percentage to jump to.</param>
        /// <param name="forceJump">Set to true if you want to jump even while UI is active.</param>
        public void JumpToPercent(float percent, bool forceJump = false)
        {
            if (!EditorFile.IsAudioLoaded) return;
            if ((EditorState.Mode.Current != EditorMode.Compose || EditorState.IsInUI) && !forceJump) return;
            EditorTime.SetTime(QNT_Timestamp.ShiftTick(playback.song.Length * percent));
            playback.PlayPreview(EditorTime.Time, new((long)EditorBeatSnap.Duration.tick));
        }
        /// <summary>
        /// Jumps to the supplied beat.
        /// </summary>
        /// <param name="beats">The beat to jump to.</param>
        public void JumpToBeat(float beats)
        {
            if (ModifierHandler.activated || EditorState.Mode.Current != EditorMode.Compose || EditorState.IsInUI) return;
            float posX = beats;
            QNT_Timestamp newTime = new QNT_Timestamp(0) + QNT_Duration.FromBeatTime(posX * EditorScale.ScaleAmount);
            newTime = EditorTime.GetSnappedTime(newTime, EditorBeatSnap.BeatSnap);
            JumpToTime(newTime);
        }
        /// <summary>
        /// Jumps to the supplied time.
        /// </summary>
        /// <param name="time">The time to jump to.</param>
        public void JumpToTime(QNT_Timestamp time)
        {
            if (isJumping)
                return;

            isJumping = true;

            if (time.ToSeconds() > playback.song.Length)           
                time = QNT_Timestamp.ShiftTick(playback.song.Length);
            
            StartCoroutine(Jump(time.tick));
        }

        /// <summary>
        /// Scrubs the timeline.
        /// </summary>
        /// <param name="forward">True for scrubbing forward, false for backwards.</param>
        /// <param name="byTick">True for scrubbing by tick, false for scrubbing by <see cref="EditorBeatSnap.BeatSnap"/></param>
        public void Scrub(bool forward, bool byTick)
        {          
            Relative_QNT jumpDuration = new Relative_QNT(byTick ? 1 : (long)EditorBeatSnap.Duration.tick);
            jumpDuration.tick *= forward ? 1 : -1;
            EditorTime.SetTime(byTick ? EditorTime.Time + jumpDuration : EditorTime.GetSnappedTime(EditorTime.Time + jumpDuration, EditorBeatSnap.BeatSnap));
            if (!EditorAudio.IsPlaying)
            {
                playback.PlayPreview(EditorTime.Time + offset, jumpDuration);
            }
            else
            {
                playback.Play(EditorTime.Time + offset);
            }
        }

        private IEnumerator Play()
        {
            playback.Play(EditorTime.Time + offset);
            while (EditorAudio.IsPlaying)
            {
                if (!isJumping)
                {
                    EditorTime.SetTime(QNT_Timestamp.ShiftTick(playback.GetTime() - offsetSeconds));
                    if(EditorTime.Seconds >= playback.song.Length)
                    {
                        EditorTime.SetTime(new QNT_Timestamp((ulong)playback.song.Length));
                        //EditorState.SetPaused(true);
                        EditorAudio.TogglePlay();
                    }
                }

                yield return null;
            }
            playback.Stop();
            EditorTime.SetTime(EditorTime.SnappedTime);
        }

        private IEnumerator Jump(float targetTime)
        {
            //bool wasPlaying = EditorAudio.IsPlaying;

            //if (wasPlaying)
                //EditorAudio.TogglePlay();

            float start = EditorTime.Time.tick;
            float progress = 0f;
            while (EditorTime.Time.tick != targetTime)
            {
                progress += Time.deltaTime / JumpDuration;
                EditorTime.SetTime(Mathf.Lerp(start, targetTime, progress));
                yield return null;
            }

            playback.PlayPreview(EditorTime.Time, new((long)EditorBeatSnap.Duration.tick));

            //if (wasPlaying)
                //EditorAudio.TogglePlay();

            isJumping = false;

            /*if (queuePlay)
            {
                queuePlay = false;
                EditorAudio.TogglePlay();
            }*/

        }
        /// <summary>
        /// Adds an offset to the audio.
        /// </summary>
        /// <param name="offset">The offset to add.</param>
        public void SetOffset(Relative_QNT offset)
        {
            this.offset = offset;
            offsetSeconds = new QNT_Timestamp((ulong)offset.tick).ToSeconds();
        }

        private void UpdateSustainAudio(QNT_Timestamp time)
        {
            if (!EditorAudio.IsPlaying)
                return;

            foreach (var note in EditorNotes.LoadedNotes)
            {
                var data = note.data;
                if (data.behavior == TargetBehavior.Sustain)
                {
                    if ((data.time < time) && (data.time + data.beatLength > time))
                    {
                        if (!note.isPlayingSustains)
                        {
                            float panPos = (float)(data.x / 7.15f);
                            if (EditorFile.AudicaFile.usesLeftSustain && note.data.handType == TargetHandType.Left)
                            {
                                playback.leftSustainVolume = EditorAudio.SustainVolume;
                                if (playback.leftSustain != null) playback.leftSustain.pan = panPos;

                            }
                            else if (EditorFile.AudicaFile.usesRightSustain && note.data.handType == TargetHandType.Right)
                            {
                                playback.rightSustainVolume = EditorAudio.SustainVolume;
                                if (playback.rightSustain != null) playback.rightSustain.pan = panPos;
                            }
                            note.isPlayingSustains = true;
                        }
                    }
                    else
                    {
                        if (note.isPlayingSustains)
                        {
                            if (note.data.handType == TargetHandType.Left)
                            {
                                playback.leftSustainVolume = 0f;
                            }
                            else if (note.data.handType == TargetHandType.Right)
                            {
                                playback.rightSustainVolume = 0f;
                            }
                            note.isPlayingSustains = false;
                        }
                    }
                }
            }
        }
    }
}
