using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI.Components
{
    public interface ITabbable
    {
        bool IsFocused { get; set; }
        int Index { get; set; }
        void Focus();
    }
}
