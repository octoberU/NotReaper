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
    public class Data
    {

        public ModifierType type;
        public int startTick;
        public int endTick;
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

    public struct Timeframe
    {
        public ulong Start { get; }
        public ulong End { get; }

        public Timeframe(QNT_Timestamp start, QNT_Timestamp end)
        {
            Start = start.tick; 
            End = end.tick;
        }

        public Timeframe(int start, int end)
        {
            Start = (ulong)start;
            End = (ulong)end;
        }

        public bool Contains(QNT_Timestamp time) => Contains(time.tick);
        public bool Contains(ulong time) => time == Start || time == End || (time > Start && time < End);

        public bool GenerousContains(QNT_Timestamp time) => Contains(time.tick);

        public bool GenerousContains(ulong time)
        {
            ulong end = End;

            if (End - Start < 128)
                end = Start + 128;
            
            
            return time == Start || time == end || (time > Start && time < end);
        }
        
        public bool Contains(Timeframe other) 
            => (other.Start <= Start && other.End >= Start) || (other.Start <= End && other.End >= End) || (other.Start >= Start && other.End <= End);

        public bool Contains(Bounds bounds)
            => Contains(new Timeframe((int)QNT_Duration.FromBeatTime(bounds.min.x).tick, (int)QNT_Duration.FromBeatTime(bounds.max.x).tick));
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
            ModifierType.HiddenTelegraphs => "Hidden Telegraphs",
            ModifierType.InvisibleGuns => "Invisible Guns",
            ModifierType.OverlaySetter => "Overlay Setter",
            ModifierType.Particles => "Particles",
            ModifierType.Psychedelia => "Psychedelia",
            ModifierType.PsychedeliaUpdate => "Psychedelia Update",
            ModifierType.AutoLighting => "Auto Lightshow",
            ModifierType.SkyboxColor => "Skybox Color",
            ModifierType.ArenaBrightness => "Skybox Brightness",
            ModifierType.Fader => "Skybox Fader",
            ModifierType.SkyboxLimiter => "Skybox Brightness Limiter",
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
