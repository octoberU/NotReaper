using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifier;
using NotReaper.Timing;
using UnityEditor;
using UnityEngine;

namespace NotReaper.Modifiers
{
    [Serializable]
    public class Data : ContentData
    {
        public ModifierType type;
        public float amount;
        public string value1;
        public string value2;
        public string xoffset;
        public string yoffset;
        public string zoffset;
        public bool option1;
        public bool option2;
        public bool independantBool;
        public float[] leftHandColor = { 0, 1, 1 };
        public float[] rightHandColor = { 0, 1, 1 };

        protected override ContentData CloneData() => 
            new Data
            {
                type = type,
                amount = amount,
                value1 = value1,
                value2 = value2,
                xoffset = xoffset,
                yoffset = yoffset,
                zoffset = zoffset,
                option1 = option1,
                option2 = option2,
                independantBool = independantBool,
                leftHandColor = leftHandColor,
                rightHandColor = rightHandColor
            };
    }

    [Serializable]
    public class ModifierDTO
    {
        public string type;
        public float startTick;
        public float endTick;
        public float amount;
        public float startPosX;
        public float endPosX;
        public float miniStartX;
        public float miniEndX;
        public string value1;
        public string value2;
        public string xoffset;
        public string yoffset;
        public string zoffset;
        public bool option1;
        public bool option2;

        public bool independantBool;
        public float[] leftHandColor;
        public float[] rightHandColor;

        public int typeTrackIndex;
    }

    public enum ModifierType
    {
        AimAssist = 0,
        ArenaChange = 1,
        ColorChange = 2,
        ColorSwap = 3,
        ColorUpdate = 4,
        HiddenTelegraphs = 5,
        InvisibleGuns = 6,
        OverlaySetter = 7,
        Particles = 8,
        Psychedelia = 9,
        PsychedeliaUpdate = 10,
        AutoLighting = 11,
        SkyboxColor = 12,
        ArenaBrightness = 13,
        Fader = 14,
        SkyboxLimiter = 15,
        ArenaRotation = 16,
        Speed = 17,
        TextPopup = 18,
        zOffset = 19,
        ArenaPosition = 20,
        ArenaSpin = 21,
        ArenaScale = 22,
    }

    public enum ColorPickerType
    {
        None,
        HSV,
        RGB
    }
    
    public static class ModifierTypeExtensions
    {
        public static string ToDisplayName(this ModifierType type) => type switch
        {
            ModifierType.AimAssist => "Aim Assist",
            ModifierType.ArenaChange => "Arena Change",
            ModifierType.ColorChange => "Color Change",
            ModifierType.ColorSwap => "Color Swap",
            ModifierType.ColorUpdate => "Color Update",
            ModifierType.HiddenTelegraphs => "Hidden Teles",
            ModifierType.InvisibleGuns => "Invisible Guns",
            ModifierType.OverlaySetter => "Overlay Setter",
            ModifierType.Particles => "Particles",
            ModifierType.Psychedelia => "Psychedelia",
            ModifierType.PsychedeliaUpdate => "Psy Update",
            ModifierType.AutoLighting => "Auto Lightshow",
            ModifierType.SkyboxColor => "Skybox Color",
            ModifierType.ArenaBrightness => "Skybox Brightness",
            ModifierType.Fader => "Skybox Fader",
            ModifierType.SkyboxLimiter => "Skybox Limiter",
            ModifierType.ArenaRotation => "Skybox Rotation",
            ModifierType.Speed => "Speed",
            ModifierType.TextPopup => "Text Popup",
            ModifierType.zOffset => "zOffset",
            ModifierType.ArenaPosition => "Arena Position",
            ModifierType.ArenaSpin => "Arena Spin",
            ModifierType.ArenaScale => "Arena Scale",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Type doesn't exists.")
        };

        public static bool IsUpdateModifier(this ModifierType type, out ModifierType baseModifier)
        {
            baseModifier = type.GetBaseModifier();
            return type is ModifierType.ColorUpdate or ModifierType.PsychedeliaUpdate;
        }

        private static ModifierType GetBaseModifier(this ModifierType type) => type switch
        {
            ModifierType.ColorUpdate => ModifierType.ColorChange,
            ModifierType.PsychedeliaUpdate => ModifierType.Psychedelia,
            _ => ModifierType.AimAssist
        };
    }
}
