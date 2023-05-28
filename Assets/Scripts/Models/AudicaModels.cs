using System;
using System.Collections.Generic;
using UnityEngine;
using NAudio.Midi;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.UI;
using NotReaper.Modifier;
using Newtonsoft.Json;

namespace NotReaper.Models
{
    public struct TempoChange
    {
        public QNT_Timestamp time;
        public UInt64 microsecondsPerQuarterNote;
        public TimeSignature timeSignature;
        public bool ExplicitSignature;
        public float secondsFromStart;
    }

    //KEEP IN MIND, almost all the song references are to the moggsong, not the mogg.
    // moggSusatinSongRight references the name of the mogg, the actual file is stored on the AudicaFile object.
    [Serializable]
    public class SongDesc
    {
        public string songID;

        public string moggSong = "";
        public string moggMainSong = "";

        [JsonIgnore]
        public string cachedMainSong
        {
            get
            {
                return $"{this.songID}";
            }
        }

        public string title = "";
        public string artist = "";

        public string midiFile = "";

        public string albumArt = "";

        public string fusionSpatialized = "fusion/guns/default/drums_default_spatial.fusion";
        public string fusionUnspatialized = "fusion/guns/default/drums_default_sub.fusion";

        public string targetDrums = "";

        public string sustainSongRight = "";
        public string moggSustainSongRight = "";

        [JsonIgnore]
        public string cachedSustainSongRight
        {
            get
            {
                return $"{this.songID}_sustain_r";
            }

        }

        public string sustainSongLeft = "";
        public string moggSustainSongLeft = "";

        [JsonIgnore]
        public string cachedSustainSongLeft
        {
            get
            {
                return $"{this.songID}_sustain_l";
            }
        }

        public string fxSong = "";
        public string moggFxSong = "";

        [JsonIgnore]
        public string cachedFxSong
        {
            get
            {
                return $"{this.songID}_extra";
            }
        }

        public float tempo = 120.0f;
        public string songEndEvent = "";
        public float prerollSeconds = 0;
        public bool useMidiForCues = false;
        public bool hidden = false;
        public string author = "";
        public int offset = 0;
        public double previewStartSeconds = 0.0d;
        [JsonIgnore]
        public List<TempoChange> tempoList;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<BookmarkData> bookmarks = new List<BookmarkData>();

        public bool bakedzOffset = false;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.ComponentModel.DefaultValue("")]
        public string customExpert;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.ComponentModel.DefaultValue("")]
        public string customAdvanced;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.ComponentModel.DefaultValue("")]
        public string customModerate;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.ComponentModel.DefaultValue("")]
        public string customBeginner;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string genre = "";
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> tags = new();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int version = 1;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool testplay = false;
    }

    public class SafeDesc
    {

        public string songID;
        public string moggSong = "song.moggsong";
        public string title;
        public string artist;
        public string midiFile = "song.mid";
        public string albumArt = "song.png";
        public string fusionSpatialized = "fusion/guns/default/drums_default_spatial.fusion";
        public string fusionUnspatialized = "fusion/guns/default/drums_default_sub.fusion";
        public string sustainSongRight = "song_sustain_r.moggsong";
        public string sustainSongLeft = "song_sustain_l.moggsong";
        public string fxSong = "song_extras.moggsong";
        public int tempo = 128; // bpm
        public string songEndEvent = "event:/song_end/song_end_C#";
        public float prerollSeconds = 0.0f;
        public bool useMidiForCues = false;
        public bool hidden = false;
        public int offset = 0; // offset
        public string mapper; // author

    }

    public class DiffsList
    {
        public CueFile expert = new CueFile();
        public CueFile advanced = new CueFile();
        public CueFile moderate = new CueFile();
        public CueFile beginner = new CueFile();

    }

    [Serializable]
    public class NRCueData
    {

        //1 - Initial version
        public uint Version = 1;

        public List<Cue> pathBuilderNoteCues = new List<Cue>();
        public List<LegacyPathbuilderData> pathBuilderNoteData = new List<LegacyPathbuilderData>();
        public List<PathbuilderData> newPathbuilderData = new List<PathbuilderData>();
        public List<Cue> newPathbuilderCues = new List<Cue>();
        //public List<RepeaterSection> repeaterSections = new List<RepeaterSection>();
        public List<Repeaters.RepeaterSection> newRepeaterSections = new List<Repeaters.RepeaterSection>();
        public List<ModifierHandler> modifiers = new List<ModifierHandler>(); //TODO: is this needed?
    }

    [Serializable]
    public class CueFile
    {
        public List<Cue> cues = null;
        public NRCueData NRCueData = null;
    }


    public class ErrorLogEntry
    {
        public QNT_Timestamp time;
        public readonly string errorDesc;

        public List<Target> affectedTargets = new List<Target>();

        public ErrorLogEntry(QNT_Timestamp time, string v)
        {
            this.time = time;
            this.errorDesc = v;
        }
    }

    public class AudicaFile
    {
        public SongDesc desc = new SongDesc();
        public SafeDesc safeDesc = new SafeDesc();
        public AudioClip song;
        public DiffsList diffs = new DiffsList();
        public ModifierList modifiers = new ModifierList();
        public AudioClip song_extras;
        public AudioClip song_sustain_l;
        public AudioClip song_sustain_r;
        public MoggSong mainMoggSong;
        public MidiFile song_mid;
        public string filepath;
        public bool usesLeftSustain = false;
        public bool usesRightSustain = false;

    }

    [Serializable]
    public class ModifierList
    {
        public List<Modifiers.ModifierDTO> modifiers = new List<Modifiers.ModifierDTO>();
        public List<TrackManager.TrackOrder> trackArrangement = new();
    }

    public static class CuesDifficulty
    {
        public static string expert = "expert";
        public static string advanced = "advanced";
        public static string standard = "standard";
        public static string easy = "easy";
    }


    public enum Difficulty { None = -1, Expert = 0, Advanced = 1, Standard = 2, Beginner = 3 }
    public enum TargetHandType { Either = 0, Right = 1, Left = 2, None = 3 }
    public enum TargetBehavior { Standard = 0, Vertical = 1, Horizontal = 2, Sustain = 3, ChainStart = 4, ChainNode = 5, Melee = 6, Mine = 7, None = 8, Legacy_Pathbuilder = 101 }
    public enum InternalTargetVelocity { Kick = 20, Snare = 127, Percussion = 60, ChainStart = 1, Chain = 2, Melee = 3, Mine = 4, Silent = 999 }
    public enum EditorTool { DragSelect, ChainBuilder, ModifierCreator, SpacingSnapper, Pathbuilder, None } //Standard, Vertical, Horizontal, Sustain, ChainStart, ChainNode, Melee, Mine, 
    public enum EditorMode { Compose, Metadata, Settings, Timing };
    public enum TargetHitsound { Standard = 0, Snare = 1, Percussion = 2, ChainStart = 3, ChainNode = 4, Melee = 5, Silent = 6, Mine = 7, Metronome = 8 }
    public enum SnappingMode { None, Grid, Melee, DetailGrid }

    static class TargetBehaviorExtensions
    {
        public static bool IsMeleeOrMine(this TargetBehavior behavior)
            => behavior is TargetBehavior.Mine or TargetBehavior.Melee;

        public static bool IsMelee(this TargetBehavior behavior)
            => behavior is TargetBehavior.Melee;

        public static bool IsMine(this TargetBehavior behavior)
            => behavior is TargetBehavior.Mine;

        public static bool IsChain(this TargetBehavior behavior)
            => behavior is TargetBehavior.ChainStart or TargetBehavior.ChainNode;

        public static bool IsChainStart(this TargetBehavior behavior)
            => behavior is TargetBehavior.ChainStart or TargetBehavior.Legacy_Pathbuilder;
    }

    static class TargetHitsoundExtensinos
    {
        public static InternalTargetVelocity ToInternalVelocty(this TargetHitsound hitsound) =>
            hitsound switch
            {
                TargetHitsound.Standard => InternalTargetVelocity.Kick,
                TargetHitsound.Snare => InternalTargetVelocity.Snare,
                TargetHitsound.Percussion => InternalTargetVelocity.Percussion,
                TargetHitsound.Melee => InternalTargetVelocity.Melee,
                TargetHitsound.Mine => InternalTargetVelocity.Mine,
                TargetHitsound.ChainStart => InternalTargetVelocity.ChainStart,
                TargetHitsound.ChainNode => InternalTargetVelocity.Chain,
                TargetHitsound.Silent => InternalTargetVelocity.Silent,
                _ => InternalTargetVelocity.Kick

            };

        public static TimelineHitsound ToTimelineHitsound(this InternalTargetVelocity hitsound) =>
            hitsound switch
            {
                InternalTargetVelocity.Kick => TimelineHitsound.Standard,
                InternalTargetVelocity.Snare => TimelineHitsound.Snare,
                InternalTargetVelocity.Percussion => TimelineHitsound.Percussion,
                InternalTargetVelocity.ChainStart => TimelineHitsound.ChainStart,
                InternalTargetVelocity.Chain => TimelineHitsound.ChainNode,
                InternalTargetVelocity.Melee => TimelineHitsound.Melee,
                InternalTargetVelocity.Mine => TimelineHitsound.Mine,
                InternalTargetVelocity.Silent => TimelineHitsound.Silent,
                _ => TimelineHitsound.Standard
            };

        public static TimelineHitsound ToTimelineHitsound(this InternalTargetVelocity hitsound, bool isMelee) =>
            hitsound switch
            {
                InternalTargetVelocity.Kick => TimelineHitsound.Standard,
                InternalTargetVelocity.Snare when isMelee => TimelineHitsound.SnareMelee,
                InternalTargetVelocity.Snare when !isMelee => TimelineHitsound.Snare,
                InternalTargetVelocity.Percussion => TimelineHitsound.Percussion,
                InternalTargetVelocity.ChainStart => TimelineHitsound.ChainStart,
                InternalTargetVelocity.Chain => TimelineHitsound.ChainNode,
                InternalTargetVelocity.Melee when isMelee => TimelineHitsound.StandardMelee,
                InternalTargetVelocity.Melee when !isMelee => TimelineHitsound.Melee,
                InternalTargetVelocity.Mine => TimelineHitsound.Mine,
                InternalTargetVelocity.Silent => TimelineHitsound.Silent,
                _ => TimelineHitsound.Standard
            };

        public static TargetHitsound ToTargetHitsound(this InternalTargetVelocity velocity) =>
            velocity switch
            {
                InternalTargetVelocity.Kick => TargetHitsound.Standard,
                InternalTargetVelocity.Snare => TargetHitsound.Snare,
                InternalTargetVelocity.Percussion => TargetHitsound.Percussion,
                InternalTargetVelocity.Melee => TargetHitsound.Melee,
                InternalTargetVelocity.Mine => TargetHitsound.Mine,
                InternalTargetVelocity.ChainStart => TargetHitsound.ChainStart,
                InternalTargetVelocity.Chain => TargetHitsound.ChainNode,
                InternalTargetVelocity.Silent => TargetHitsound.Silent,
                _ => TargetHitsound.Standard
            };
    }

    static class DifficultyExtensions
    {
        public static bool IsExpert(this Difficulty diff)
            => diff == Difficulty.Expert;

        public static bool IsAdvanced(this Difficulty diff)
            => diff == Difficulty.Advanced;

        public static bool IsStandard(this Difficulty diff)
            => diff == Difficulty.Standard;

        public static bool IsBeginner(this Difficulty diff)
            => diff == Difficulty.Beginner;
    }

    [Serializable]
    public class Cue
    {
        public int tick;
        public int tickLength;
        public int pitch;
        public InternalTargetVelocity velocity = InternalTargetVelocity.Kick;
        public GridOffset gridOffset = new GridOffset { x = 0, y = 0 };
        public float zOffset = 0;
        public TargetHandType handType = TargetHandType.Right;
        public TargetBehavior behavior = TargetBehavior.Standard;

        [Serializable]
        public struct GridOffset
        {
            public double x;
            public double y;
        }
    }

    public static class CueExtensions
    {
        public static float GetMsTime(this Cue cue) => QNT_Timestamp.TickToMilliseconds(cue.tick);
        public static float GetEndMsTime(this Cue cue) => QNT_Timestamp.TickToMilliseconds(cue.tick + cue.tickLength);
    }



}