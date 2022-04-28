using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NotReaper;
using TMPro;
using UnityEngine;
using NotReaper.UserInput;
using UnityEngine.EventSystems;
using NotReaper.Timing;
using UnityEngine.InputSystem;
using NotReaper.UI.Components;
using System.IO;
using NotReaper.Models;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using NotReaper.MapIO;
namespace NotReaper.UI.Countin
{
    public class CountInWindow : NRMenu
    {
        public NRInputField lengthInput;
        [NRInject] private PrecisePlayback playback;
        private CanvasGroup canvas;
        public bool isActive = false;
        protected override void Awake()
        {
            base.Awake();
            canvas = GetComponent<CanvasGroup>();
        }

        void Start()
        {
            Vector3 defaultPos = Vector3.zero;
            lengthInput.text = "8";
            gameObject.GetComponent<RectTransform>().localPosition = defaultPos;
            canvas.alpha = 0.0f;
            gameObject.SetActive(false);
        }

        public override void Show()
        {
            isActive = true;
            OnActivated();
            canvas.DOFade(1.0f, 0.3f);
            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            isActive = false;
            canvas.DOFade(0.0f, 0.3f).OnComplete(() =>
            {
                OnDeactivated();
            });
        }

        public override void ShowHelp()
        {
            NRHelp.Instance.ShowCountin();
        }

        public void PreviewCountIn()
        {
            if (uint.TryParse(lengthInput.text, out uint beats))
            {
                PreviewCountIn(beats);
            }
        }

        public void GenerateCountIn()
        {
            if (uint.TryParse(lengthInput.text, out uint beats))
            {
                GenerateCountIn(beats);
            }
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }

        public void PreviewCountIn(uint beats)
        {
            if (EditorAudio.IsPlaying)
            {
                EditorAudio.TogglePlay();
            }

            EditorTime.SetTime(0);
            //SafeSetTime();

            TempoChange first = EditorTempo.TempoChanges[0];
            QNT_Duration timeSignatureDuration = new QNT_Duration(Constants.PulsesPerWholeNote / first.timeSignature.Denominator) * beats;
            playback.PlayClickTrack(new QNT_Timestamp(0) + timeSignatureDuration);

            if (!EditorAudio.IsPlaying)
            {
                EditorAudio.TogglePlay();
            }
        }

        public void GenerateCountIn(uint beats)
        {
            TempoChange first = EditorTempo.TempoChanges[0];
            QNT_Duration timeSignatureDuration = new QNT_Duration(Constants.PulsesPerWholeNote / first.timeSignature.Denominator) * beats;
            string appPath = Application.dataPath;
            string wavPath = $"{appPath}/.cache/" + "clickTrack.wav";
            string oggPath = $"{appPath}/.cache/" + "clickTrack.ogg";

            string moggName = "song_extras.mogg";
            string moggPath = $"{appPath}/.cache/" + moggName;
            SavWav.Save(wavPath, playback.GenerateClickTrack(new QNT_Timestamp(0) + timeSignatureDuration));

            //Convert wav to ogg
            if (!EditorAudioManager.Instance.ConvertWavToOgg(wavPath, oggPath))
            {
                return;
            }

            //Convert ogg to mogg
            EditorAudioManager.Instance.ConvertOggToMogg(oggPath, moggPath);

            //Add extra to zip archive
            using (var archive = ZipArchive.Open(EditorFile.AudicaFile.filepath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.ToString() == moggName)
                    {
                        archive.RemoveEntry(entry);
                    }
                }
                archive.AddEntry(moggName, moggPath);
                archive.SaveTo(EditorFile.AudicaFile.filepath + ".temp", SharpCompress.Common.CompressionType.None);
                archive.Dispose();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            File.Delete(EditorFile.AudicaFile.filepath);
            File.Move(EditorFile.AudicaFile.filepath + ".temp", EditorFile.AudicaFile.filepath);

            //Load the generated extra sounds
            StartCoroutine(EditorAudioManager.Instance.LoadExtraAudio($"file://{oggPath}"));
            Hide();
        }

        public void RemoveCountin()
        {
            string extrasName = "song_extras.mogg";
            string emptyExtras = Path.Combine(Application.streamingAssetsPath, "Ogg2Audica", "AudicaTemplate", extrasName);

            using var archive = ZipArchive.Open(EditorFile.AudicaFile.filepath);
            foreach(var entry in archive.Entries)
            {
                if(entry.ToString() == extrasName)
                {
                    archive.RemoveEntry(entry);
                }
            }
            archive.AddEntry(extrasName, emptyExtras);
            archive.SaveTo(EditorFile.AudicaFile.filepath + ".temp", SharpCompress.Common.CompressionType.None);
            archive.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var filename = EditorFile.AudicaFile.filepath;
            File.Delete(filename);
            File.Move(filename + ".temp", filename);

            MemoryStream tempMogg = new MemoryStream();
            File.OpenRead(emptyExtras).CopyTo(tempMogg);
            AudicaLoader.MoggToOgg(tempMogg.ToArray(), EditorFile.AudicaFile.desc.cachedFxSong);
            StartCoroutine(EditorAudioManager.Instance.LoadExtraAudio($"file://{Application.dataPath}/.cache/{EditorFile.AudicaFile.desc.cachedFxSong}.ogg"));
            Hide();
        }
    }
}
