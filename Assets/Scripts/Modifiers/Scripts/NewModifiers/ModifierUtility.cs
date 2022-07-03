using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifier;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public static class ModifierUtility
    {
        private static Camera cam;

        static ModifierUtility()
        {
            cam = CameraProvider.timeline;
        }
        
        public static string GetDisplayName(ModifierType type)
            => type switch
            {
                ModifierType.Fader => "Fader",
                ModifierType.Particles => "Particles",
                ModifierType.Psychedelia => "Psychedelia",
                ModifierType.PsychedeliaUpdate => "Psy Update",
                ModifierType.AimAssist => "Aim Assist",
                ModifierType.Speed => "Speed",
                ModifierType.zOffset => "zOffset",
                ModifierType.ArenaBrightness => "Skybox Bright",
                ModifierType.ArenaChange => "Arena Change",
                ModifierType.ArenaPosition => "Arena Pos",
                ModifierType.ArenaRotation => "Skybox Rot",
                ModifierType.ArenaScale => "Arena Scale",
                ModifierType.ArenaSpin => "Arena Rot",
                ModifierType.AutoLighting => "Auto Light",
                ModifierType.ColorChange => "Color Change",
                ModifierType.ColorSwap => "Color Swap",
                ModifierType.ColorUpdate => "Color Update",
                ModifierType.HiddenTelegraphs => "Hidden Teles",
                ModifierType.InvisibleGuns => "Invis Guns",
                ModifierType.OverlaySetter => "Overlay Setter",
                ModifierType.SkyboxColor => "Skybox Color",
                ModifierType.SkyboxLimiter => "Skybox Limiter",
                ModifierType.TextPopup => "Text Popup",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Modifier type not implemented!"),
            };

        public static bool SupportsEndTime(ModifierType type, bool option1, bool option2)
            => type switch
            {
                ModifierType.Fader => true,
                ModifierType.Particles => true,
                ModifierType.PsychedeliaUpdate => false,
                ModifierType.AimAssist => true,
                ModifierType.Speed => true,
                ModifierType.zOffset => false,
                ModifierType.ArenaBrightness => option1 || option2,
                ModifierType.ArenaChange => false,
                ModifierType.ColorChange => true,
                ModifierType.ColorSwap => true,
                ModifierType.ColorUpdate => false,
                ModifierType.HiddenTelegraphs => true,
                ModifierType.InvisibleGuns => true,
                ModifierType.OverlaySetter => false,
                ModifierType.Psychedelia => true,
                ModifierType.AutoLighting => true,
                ModifierType.SkyboxColor => true,
                ModifierType.SkyboxLimiter => false,
                ModifierType.ArenaRotation => option1 || option2,
                ModifierType.TextPopup => true,
                ModifierType.ArenaPosition => true,
                ModifierType.ArenaSpin => true,
                ModifierType.ArenaScale => true,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Type not supported")
            };

        public static TrackContent GetModifierUnderMouse()
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray);
            foreach (var hit in hits)
            {
                if (hit.transform.TryGetComponent(out TrackContent trackContent))
                {
                    return trackContent;
                }
            }

            return null;
        }
    }
}
