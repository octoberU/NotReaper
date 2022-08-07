using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.SustainTimeline
{
    public enum Pitch
    {
        C,
        CSharp,
        D,
        DSharp,
        E,
        F,
        FSharp,
        G,
        GSharp,
        A,
        ASharp,
        B
    }

    public static class PitchExtensions
    {
        public static string ToDisplayName(this Pitch pitch) =>
            pitch switch
            {
                Pitch.C => "C",
                Pitch.CSharp => "C#",
                Pitch.D => "D",
                Pitch.DSharp => "D#",
                Pitch.E => "E",
                Pitch.F => "F",
                Pitch.FSharp => "F#",
                Pitch.G => "G",
                Pitch.GSharp => "G#",
                Pitch.A => "A",
                Pitch.ASharp => "A#",
                Pitch.B => "B",
                _ => throw new ArgumentOutOfRangeException(nameof(pitch), pitch, "Pitch not recognized!")
            };
    }
}
