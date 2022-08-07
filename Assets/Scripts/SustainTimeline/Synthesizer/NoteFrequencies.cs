using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Audio.Synthesizer
{
    public class NoteFrequencies
    {
        private const float BaseFrequency = 440; // A4
        private const int Octaves = 6;
        private const int SemitonesPerOctave = 12;
        public NoteFrequencies()
        {
            int totalNotes = Octaves * SemitonesPerOctave;
            float interval = 1f / 12f;
            int stepsFromRoot = -57; //we start at C0, which is 57 half tones away from A4
            for (int i = 0; i < totalNotes; i++)
            {
                Notes.Add((Pitch)i, BaseFrequency * Mathf.Pow(interval, stepsFromRoot));
                stepsFromRoot++;
            }
        }

        public Dictionary<Pitch, float> Notes { get; } = new();
    }

    public enum Pitch
    {
        C1,
        CSharp1,
        D1,
        DSharp1,
        E1,
        F1,
        FSharp1,
        G1,
        GSharp1,
        A1,
        ASharp1,
        B1,
        C2,
        CSharp2,
        D2,
        DSharp2,
        E2,
        F2,
        FSharp2,
        G2,
        GSharp2,
        A2,
        ASharp2,
        B2,
        C3,
        CSharp3,
        D3,
        DSharp3,
        E3,
        F3,
        FSharp3,
        G3,
        GSharp3,
        A3,
        ASharp3,
        B3,
        C4,
        CSharp4,
        D4,
        DSharp4,
        E4,
        F4,
        FSharp4,
        G4,
        GSharp4,
        A4,
        ASharp4,
        B4,
        C5,
        CSharp5,
        D5,
        DSharp5,
        E5,
        F5,
        FSharp5,
        G5,
        GSharp5,
        A5,
        ASharp5,
        B5,
    }
}
