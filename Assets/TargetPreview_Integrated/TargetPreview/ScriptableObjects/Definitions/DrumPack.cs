using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TargetPreview.ScriptableObjects
{
    [CreateAssetMenu(menuName = "TargetPreview/Drum Pack")]
    public class DrumPack : ScriptableObject
    {
        [Header("Audio")]
        public AudioClip kick;
        public AudioClip snare;
        public AudioClip percussion;
        public AudioClip chainStart;
        public AudioClip chain;
        public AudioClip melee;
    }
}