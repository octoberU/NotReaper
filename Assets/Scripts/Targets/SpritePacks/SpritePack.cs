using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper
{
    [CreateAssetMenu(menuName = "Targets/Sprite Pack", fileName = "SpritePack")]
    public class SpritePack : ScriptableObject
    {
        public Sprite target;
        public Sprite ring;
        public Sprite telegraph;
        public Texture noise;
        public float bloom;
    }
}