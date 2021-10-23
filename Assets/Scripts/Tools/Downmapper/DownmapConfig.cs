using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using NotReaper.Managers;
using System.IO;
using Newtonsoft.Json;
public class DownmapConfig : MonoBehaviour
{
    public static DownmapConfig Instance = null;

    public DownmapPrefrences Preferences;
    private string configPath;
    #region Private Methods
    private void Awake()
    {
        if (Instance is null) Instance = this;
        else
        {
            Debug.LogWarning("DownmapConfig already exists.");
            return;
        }
        configPath = Path.Combine(Application.persistentDataPath, "downmapConfig_");

    }
    private void SetAdvancedDefaults()
    {
        var streams = new StreamsConfig(true, true, 120, 3);
        var slots = new SlotsConfig(true, false, 960, 480);
        var sustains = new SustainsConfig(true, 480, 0);
        var chains = new ChainsConfig(true, false, false, 0, 480, 0);
        var melees = new MeleesConfig(false, false, 0, 0);
        var singleTargetSpacing = new SingleTargetSpacingConfig(true, 5f, 4f, 2f, 1f);
        var doubles = new DoublesConfig(true, false, 4f, 0);

        Preferences = new DownmapPrefrences(streams, slots, sustains, chains, melees, singleTargetSpacing, doubles);
    }
    private void SetStandardDefaults()
    {
        var streams = new StreamsConfig(true, true, 240, 2);
        var slots = new SlotsConfig(true, true, 0, 0);
        var sustains = new SustainsConfig(true, 0, 960);
        var chains = new ChainsConfig(true, true, false, 960, 960, 960);
        var melees = new MeleesConfig(true, false, 960, 960);
        var singleTargetSpacing = new SingleTargetSpacingConfig(true, 3f, 2f, 2f, 1f);
        var doubles = new DoublesConfig(true, true, 3f, 960);

        Preferences = new DownmapPrefrences(streams, slots, sustains, chains, melees, singleTargetSpacing, doubles);
    }
    private void SetBeginnerDefaults()
    {
        var streams = new StreamsConfig(true, false, 480, 2);
        var slots = new SlotsConfig(true, true, 0, 0);
        var sustains = new SustainsConfig(true, 240, 480);
        var chains = new ChainsConfig(true, false, true, 0, 0, 0);
        var melees = new MeleesConfig(true, true, 0, 0);
        var singleTargetSpacing = new SingleTargetSpacingConfig(true, 3f, 1.5f, 1.5f, 1.5f);
        var doubles = new DoublesConfig(true, true, 2f, 960);

        Preferences = new DownmapPrefrences(streams, slots, sustains, chains, melees, singleTargetSpacing, doubles);
    }
    #endregion
    #region Public Methods
    public bool SetDefaultValues(int difficulty)
    {
        switch (difficulty)
        {
            case 1:
                SetAdvancedDefaults();
                break;
            case 2:
                SetStandardDefaults();
                break;
            case 3:
                SetBeginnerDefaults();
                break;
            default:
                return false;
        }
        return true;
    }
    public void SaveCustomValues(int difficultyIndex)
    {
        if (difficultyIndex == 0) return;
        string path = configPath + $"{difficultyIndex}.json";
        string json = JsonConvert.SerializeObject(Preferences, Formatting.Indented);
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
    }
    public bool LoadCustomValues(int difficultyIndex)
    {
        if (difficultyIndex <= 0) return false;
        string path = configPath + $"{difficultyIndex}.json";
        if (!File.Exists(path))
        {
            SetDefaultValues(difficultyIndex);
            return false;
        }
        else
        {
            using (StreamReader sr = new StreamReader(path))
            {
                var json = sr.ReadToEnd();
                Preferences = JsonConvert.DeserializeObject<DownmapPrefrences>(json);
            }
            return true;
        }

    }
    #endregion
    #region Config Classes
    public class DownmapPrefrences
    {
        public StreamsConfig Streams;
        public SlotsConfig Slots;
        public SustainsConfig Sustains;
        public ChainsConfig Chains;
        public MeleesConfig Melees;
        public SingleTargetSpacingConfig SingleTargetSpacing;
        public DoublesConfig Doubles;

        public DownmapPrefrences(StreamsConfig streams, SlotsConfig slots, SustainsConfig sustains, ChainsConfig chains, MeleesConfig melees, SingleTargetSpacingConfig singleTargetSpacing, DoublesConfig doubles)
        {
            Streams = streams;
            Slots = slots;
            Sustains = sustains;
            Chains = chains;
            Melees = melees;
            SingleTargetSpacing = singleTargetSpacing;
            Doubles = doubles;
        }
    }
    public class StreamsConfig
    {
        public bool enabled;
        public bool stream2Chain;
        public ulong maxStreamSpeed;
        public int maxConsecutiveTargets;

        public StreamsConfig(bool enabled, bool stream2Chain, ulong maxStreamSpeed, int maxConsecutiveTargets)
        {
            this.enabled = enabled;
            this.stream2Chain = stream2Chain;
            this.maxStreamSpeed = maxStreamSpeed;
            this.maxConsecutiveTargets = maxConsecutiveTargets;
        }
    }

    public class SlotsConfig
    {
        public bool enabled;
        public bool convert;
        public ulong leadinHorizontal;
        public ulong leadinVertical;

        public SlotsConfig(bool enabled, bool convert, ulong leadinHorizontal, ulong leadinVertical)
        {
            this.enabled = enabled;
            this.convert = convert;
            this.leadinHorizontal = leadinHorizontal;
            this.leadinVertical = leadinVertical;
        }
    }

    public class SustainsConfig
    {
        public bool enabled;
        public ulong leadinTime;
        public ulong pauseAfter;

        public SustainsConfig(bool enabled, ulong leadinTime, ulong pauseAfter)
        {
            this.enabled = enabled;
            this.leadinTime = leadinTime;
            this.pauseAfter = pauseAfter;
        }
    }

    public class ChainsConfig
    {
        public bool enabled;
        public bool isolate;
        public bool convert;
        public ulong leadinTime;
        public ulong pauseSameHand;
        public ulong pauseOtherHand;

        public ChainsConfig(bool enabled, bool isolate, bool convert, ulong leadinTime, ulong pauseSameHand, ulong pauseOtherHand)
        {
            this.enabled = enabled;
            this.isolate = isolate;
            this.convert = convert;
            this.leadinTime = leadinTime;
            this.pauseSameHand = pauseSameHand;
            this.pauseOtherHand = pauseOtherHand;
        }
    }

    public class MeleesConfig
    {
        public bool enabled;
        public bool deleteAll;
        public ulong leadinTime;
        public ulong pauseTime;

        public MeleesConfig(bool enabled, bool deleteAll, ulong leadinTime, ulong pauseTime)
        {
            this.enabled = enabled;
            this.deleteAll = deleteAll;
            this.leadinTime = leadinTime;
            this.pauseTime = pauseTime;
        }
    }

    public class SingleTargetSpacingConfig
    {
        public bool enabled;
        public float halfNote;
        public float quarterNote;
        public float eighthNote;
        public float sixteenthNote;

        public SingleTargetSpacingConfig(bool enabled, float halfNote, float quarterNote, float eighthNote, float sixteenthNote)
        {
            this.enabled = enabled;
            this.halfNote = halfNote;
            this.quarterNote = quarterNote;
            this.eighthNote = eighthNote;
            this.sixteenthNote = sixteenthNote;
        }
    }

    public class DoublesConfig
    {
        public bool enabled;
        public bool uncross;
        public float maxDistance;
        public ulong leadinTime;

        public DoublesConfig(bool enabled, bool uncross, float maxDistance, ulong leadinTime)
        {
            this.enabled = enabled;
            this.uncross = uncross;
            this.maxDistance = maxDistance;
            this.leadinTime = leadinTime;
        }
    }
    #endregion
}
