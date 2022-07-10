using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Modifier;
using NotReaper.Timing;
using TMPro;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ModifierTrack : Track
    {
        protected override string TypeToDisplayName(int type) => ((ModifierType)type).ToDisplayName();
    }
}