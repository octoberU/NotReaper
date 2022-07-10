using System.Collections;
using System.Collections.Generic;
using NotReaper.ObjectPooling;
using UnityEngine;

namespace NotReaper
{
    public class ContentPool : ObjectPool<Content>
    {
        public override void Return(Content content)
        {
            content.ResetState();
            base.Return(content);
        }
    }
}
