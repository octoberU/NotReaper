using NotReaper.ObjectPooling;
using System.Collections;
using System.Collections.Generic;
using TargetPreview.ScriptableObjects;
using UnityEngine;

namespace NotReaper.MapPreview
{
    public class LinePool : ObjectPool<LineConnector>
    {
        [SerializeField] private VisualConfig config;
        public override LineConnector Spawn()
        {
            var connector = base.Spawn();
            connector.Initialize(config.leftHandColor, config.rightHandColor);
            return connector;
        }
    }

}
