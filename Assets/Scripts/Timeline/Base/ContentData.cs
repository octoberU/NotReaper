using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper
{
    [Serializable]
    public abstract class ContentData
    {
        public int startTick;
        public int endTick;
        public int typeIndex;
        public ContentData Clone()
        {
            var clone = CloneData();
            clone.startTick = startTick;
            clone.endTick = endTick;
            clone.typeIndex = typeIndex;
            return clone;
        }

        protected abstract ContentData CloneData();
    }
}
