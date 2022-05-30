using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifier;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public static class ModifierUtility
    {
        public static string GetDisplayName(ModifierHandler.ModifierType type)
            => type switch
            {
                ModifierHandler.ModifierType.Fader => "Fader",
                ModifierHandler.ModifierType.Particles => "Particles",
                ModifierHandler.ModifierType.Psychedelia => "Psychedelia",
                ModifierHandler.ModifierType.PsychedeliaUpdate => "Psy Update",
                ModifierHandler.ModifierType.AimAssist => "Aim Assist",
                ModifierHandler.ModifierType.Speed => "Speed",
                ModifierHandler.ModifierType.zOffset => "zOffset",
                ModifierHandler.ModifierType.ArenaBrightness => "Skybox Bright",
                ModifierHandler.ModifierType.ArenaChange => "Arena Change",
                ModifierHandler.ModifierType.ArenaPosition => "Arena Pos",
                ModifierHandler.ModifierType.ArenaRotation => "Skybox Rot",
                ModifierHandler.ModifierType.ArenaScale => "Arena Scale",
                ModifierHandler.ModifierType.ArenaSpin => "Arena Rot",
                ModifierHandler.ModifierType.AutoLighting => "Auto Light",
                ModifierHandler.ModifierType.ColorChange => "Color Change",
                ModifierHandler.ModifierType.ColorSwap => "Color Swap",
                ModifierHandler.ModifierType.ColorUpdate => "Color Update",
                ModifierHandler.ModifierType.HiddenTelegraphs => "Hidden Teles",
                ModifierHandler.ModifierType.InvisibleGuns => "Invis Guns",
                ModifierHandler.ModifierType.OverlaySetter => "Overlay Setter",
                ModifierHandler.ModifierType.SkyboxColor => "Skybox Color",
                ModifierHandler.ModifierType.SkyboxLimiter => "Skybox Limiter",
                ModifierHandler.ModifierType.TextPopup => "Text Popup",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Modifier type not implemented!"),
            };
    }
}
