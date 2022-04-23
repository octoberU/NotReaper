using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Genres
{
    [Serializable]
    public class Genre
    {
        public string name = "";
        public List<string> subgenres = new();
    }
}
