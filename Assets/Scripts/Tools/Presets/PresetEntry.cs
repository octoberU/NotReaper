using System;
using NotReaper.Notifications;
using NotReaper.Targets;
using NotReaper.Tools.ChainBuilder;
using NotReaper.UserInput;
using System.Collections;
using System.Collections.Generic;
using NotReaper.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Tools.Presets
{
    public class PresetEntry : MonoBehaviour
    {
        [SerializeField] private NRInputField title;
        [SerializeField] private Image image;

        private PresetData _preset;

        internal PresetData preset
        {
            get
            {
                return _preset;
            }
            set
            {
                _preset = value;
                Initialize();
            }
        }

        private Timeline timeline;
        private MappingInput mapping;
        private PresetUI ui;

        private void Start()
        {
            timeline = NRDependencyInjector.Get<Timeline>();
            mapping = NRDependencyInjector.Get<MappingInput>();
            ui = NRDependencyInjector.Get<PresetUI>();
        }

        private void Initialize()
        {
            title.text = preset.presetName;
            image.overrideSprite = preset.thumbnail;
            //image.sprite = preset.thumbnail;
        }

        public void OnClick()
        {
            CopyPreset();
        }

        public void OnNameFocusChange(bool focus)
        {
            if (!focus && (string.IsNullOrEmpty(title.text) || title.text.Length < 3))
            {
                title.text = preset.presetName;
            }
            else if(!string.Equals(title.text, preset.presetName, StringComparison.InvariantCultureIgnoreCase))
            {
                var oldName = preset.presetName;
                preset.presetName = title.text;
                ui.SavePreset(preset, oldName);
            }
        }

        public void OnNameEndEdit(string newName)
        {

            if (string.IsNullOrEmpty(newName) || newName.Length < 3)
            {
                title.text = preset.presetName;
            }
            else if(!string.Equals(newName, preset.presetName, StringComparison.InvariantCultureIgnoreCase))
            {
                var oldName = preset.presetName;
                preset.presetName = newName;
                ui.SavePreset(preset, oldName);
            }
        }

        private void CopyPreset()
        {
            List<TargetData> copyData = new();
            foreach(var target in preset.targets)
            {
                var data = EditorTargets.ConvertCueToTargetData(target.cue);
                data.pathbuilderData = target.pathbuilderData;
                data.legacyPathbuilderData = target.legacyPathbuilderData;
                data.isPathbuilderTarget = target.isPathbuilderTarget;
                copyData.Add(data);
            }
            EditorTargets.CopyTargets(copyData);
            NotificationCenter.SendNotification("Preset copied!", NotificationType.Success);
        }

        public void OnDeleteClicked()
        {
            ui.OnDelete(this);
        }
    }
}

