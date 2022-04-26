using Newtonsoft.Json;
using NotReaper.Targets;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using UnityEngine;
using NotReaper.Models;
using System.Linq;
using System;

namespace NotReaper.Tools.Presets
{
    public class PresetManager : MonoBehaviour
    {
        private string presetDirectory;
        private string tempPath;

        [NRInject] private PresetUI ui;

        private void Awake()
        {
            presetDirectory = Path.Combine(Application.dataPath, @"../", "presets");
            tempPath = Path.Combine(Application.dataPath, @"../", "presets", "temp");
        }

        private void Start()
        {
            if (!Directory.Exists(presetDirectory))
            {
                Directory.CreateDirectory(presetDirectory);
            }
            StartCoroutine(LoadPresets());
        }

        private IEnumerator LoadPresets()
        {
            foreach(var file in Directory.GetFiles(presetDirectory))
            {
                var archive = ZipFile.Open(file, ZipArchiveMode.Read);
                PresetData preset = new();
                Texture2D thumbnail = new(1, 1);
                foreach(var entry in archive.Entries)
                {
                    if (entry.Name.Contains("preset"))
                    {
                        using StreamReader reader = new(entry.Open());
                        string json;
                        yield return json = reader.ReadToEnd();
                        var tempPreset = JsonUtility.FromJson<PresetData>(json);
                        List<PresetTarget> presetTargets = new();
                        foreach(var target in tempPreset.targets)
                        {
                            presetTargets.Add(new(target.cue, target.pathbuilderData, target.legacyPathbuilderData, target.isPathbuilderTarget));
                        }
                        preset = new(tempPreset.presetName, presetTargets);
                    }
                    else if (entry.Name.Contains("thumb"))
                    {
                        using MemoryStream memory = new();
                        entry.Open().CopyTo(memory);
                        yield return thumbnail.LoadImage(memory.ToArray());
                        preset.thumbnail = Sprite.Create(thumbnail, new Rect(0, 0, thumbnail.width, thumbnail.height), Vector2.zero);
                    }
                }
                ui.AddPreset(preset);
            }
            yield return null;
        }

        private IEnumerator DoSavePreset(PresetData preset, Action<PresetData> onComplete = null)
        {
            Directory.CreateDirectory(tempPath);
            string path = Path.Combine(presetDirectory, preset.presetName + ".preset");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            string tempThumb = Path.Combine(tempPath, "thumb.png");
            string tempPreset = Path.Combine(tempPath, "preset.json");
            Texture2D thumbnail;
            thumbnail = new Texture2D((int)(Screen.width * .75f), (int)(Screen.height * .6f), TextureFormat.RGBA32, false);
            yield return new WaitForEndOfFrame();
            float xOffset = (Screen.width - (Screen.width * .75f)) * .5f;
            float yOffset = (Screen.height - (Screen.height * .6f)) * .375f;
            //thumbnail.ReadPixels(new Rect(xOffset, yOffset, Screen.width * .75f, Screen.height * .6f), 0, 0, false);
            thumbnail.ReadPixels(new Rect(xOffset, yOffset, Screen.width * .75f, Screen.height * .6f), 0, 0, false);
            thumbnail.Apply();
            preset.thumbnail = Sprite.Create(thumbnail, new Rect(0, 0, thumbnail.width, thumbnail.height), Vector2.zero);
            var bytes = thumbnail.EncodeToPNG();
            var json = JsonUtility.ToJson(preset, true);
            yield return File.WriteAllBytesAsync(tempThumb, bytes);
            yield return File.WriteAllTextAsync(tempPreset, json);
            ZipFile.CreateFromDirectory(tempPath, path, System.IO.Compression.CompressionLevel.NoCompression, false);
            Directory.Delete(tempPath, true);
            onComplete?.Invoke(preset);
        }

        internal void DeletePreset(PresetData preset)
        {
            var path = Path.Combine(presetDirectory, preset.presetName + ".preset");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void SavePreset(string name, List<Target> targets, Action<PresetData> onComplete = null)
        {
            if (targets.Count == 0) return;
            StartCoroutine(DoSavePreset(new(name, targets), onComplete));
        }
    }

}
