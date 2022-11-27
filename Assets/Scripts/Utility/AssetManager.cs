using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper
{
    public class AssetManager : MonoBehaviour
    {
        [SerializeField] private AssetContainer assetContainer;

        private void Start() => assetContainer.CreatePresetProperties();
    }
}
