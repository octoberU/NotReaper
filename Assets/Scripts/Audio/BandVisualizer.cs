using NotReaper;
using NotReaper.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BandVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSyncScale bandPrefab;
    [SerializeField] private Transform parent;
    private List<AudioSyncScale> bands = new List<AudioSyncScale>();

    [Space, Header("Settings")]
    [SerializeField] private float bias = .4f;
    [SerializeField] private float timeStep = 0f;
    [SerializeField] private float timeToBeat = .15f;
    [SerializeField] private float restSmoothTime = 3f;
    [SerializeField] private Vector3 restScale = new Vector3(1f, 0f, 1f);
    [SerializeField] private Vector3 beatScale = new Vector3(1f, 2f, 1f);
    [SerializeField] private float colorAlpha = .3f;
    private Color[] colors = new Color[64];



    private void Start()
    {
        NRSettings.OnLoad(Setup);     
    }

    private void Setup()
    {
        for (int i = 0; i < 64; i++)
        {
            var band = Instantiate(bandPrefab);
            band.band = i;
            band.bias = bias;
            band.timeStep = timeStep;
            band.timeToBeat = timeToBeat;
            band.restSmoothTime = restSmoothTime;
            band.restScale = restScale;
            band.beatScale = beatScale;
            band.sync = AudioSyncer.SyncMode.BufferedBand64;
            band.transform.parent = parent;
            band.gameObject.name = $"Band {i + 1}";
            band.transform.localScale = restScale;
            band.SetColor(Color.Lerp(NRSettings.config.leftColor, NRSettings.config.rightColor, i / 64f), colorAlpha);
            bands.Add(band);
        }
    }
}
