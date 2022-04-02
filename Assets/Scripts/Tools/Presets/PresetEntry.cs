using NotReaper.Notifications;
using NotReaper.Targets;
using NotReaper.UserInput;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Tools.Presets
{
    public class PresetEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
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

        private void CopyPreset()
        {
            List<TargetData> copyData = new();
            foreach(var target in preset.targets)
            {
                var data = timeline.GetTargetDataForCue(target.cue);
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

