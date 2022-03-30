using NotReaper.ObjectPooling;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifier
{
    public class ModifierPool : ObjectPool<Modifier>
    {

        public override void Return(Modifier modifier)
        {
            modifier.Reset();
            base.Return(modifier);
        }
    }

}
