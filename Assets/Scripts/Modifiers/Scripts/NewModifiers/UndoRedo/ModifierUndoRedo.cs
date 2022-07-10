using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Tools;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ModifierUndoRedo : TimelineUndoRedo<Data>
    {
        protected override TimelineManager<Data> GetManager()
            => NRDependencyInjector.Get<ModifierManager>();
    }
}
