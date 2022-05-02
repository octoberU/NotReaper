using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NAudio.Midi;
using Newtonsoft.Json;

namespace AudicaTools
{
    
    public class Audica : IEnumerable<Difficulty>, IEquatable<Audica>
    {
        public Difficulty beginner;
        public Difficulty moderate;
        public Difficulty advanced;
        public Difficulty expert;

        public MidiFile midi;
        public List<TempoData> tempoData = new List<TempoData>();

        public Description desc;

        public MoggSong moggSong;
        public MonoMoggSong moggSongSustainL;
        public MonoMoggSong moggSongSustainR;

        public Mogg song;
        public Mogg songSustainL;
        public Mogg songSustainR;

        public byte[] albumArt;

        public string fileName;

        public Audica(string filePath)
        {
            CheckPath(filePath);

            fileName = Path.GetFileNameWithoutExtension(filePath);
            using ZipArchive zip = ZipFile.OpenRead(filePath);
           
            string[] zipFileNames = zip.Entries.Select(entry => entry.Name).ToArray(); //Get file names once so that we don't have to loop over entries again

            this.desc = ReadJsonEntry<Description>(zip, "song.desc");
            this.expert = ReadJsonEntry<Difficulty>(zip, "expert.cues");
            this.advanced = ReadJsonEntry<Difficulty>(zip, "advanced.cues");
            this.moderate = ReadJsonEntry<Difficulty>(zip, "moderate.cues");
            this.beginner = ReadJsonEntry<Difficulty>(zip, "beginner.cues");

            this.moggSong = new MoggSong(zip.GetEntry(this.desc.moggSong)?.Open());
            if (this.desc.sustainSongLeft != "") this.moggSongSustainL = new MonoMoggSong(zip.GetEntry(this.desc.sustainSongLeft)?.Open());
            if (this.desc.sustainSongRight != "") this.moggSongSustainR = new MonoMoggSong(zip.GetEntry(this.desc.sustainSongRight)?.Open());

            if (this.moggSongSustainL != null) this.songSustainL = new Mogg(zip.GetEntry(moggSongSustainL.moggPath)?.Open());
            if (this.moggSongSustainR != null) this.songSustainR = new Mogg(zip.GetEntry(moggSongSustainR.moggPath)?.Open());

            ZipArchiveEntry songEntry = zip.GetEntry(moggSong.moggPath);
            if (songEntry != null) this.song = new Mogg(songEntry.Open());

            this.midi = new MidiFile(zip.GetEntry(desc.midiFile)?.Open(), true);
            this.tempoData = ReadTempoEvents(midi.Events);

            //this.midi = zip.GetEntry(desc.midiFile).Open();
            //this.moggSongSustainL = new MoggSong(zip.GetEntry(this.desc.sustainSongLeft).Open());
            //this.moggSongSustainR = new MoggSong(zip.GetEntry(this.desc.sustainSongRight).Open());

            if (zipFileNames.Contains("song.png")) albumArt = Utility.GetBytesFromStream(zip.GetEntry("song.png")?.Open());

            
        }
        
        public void SetAlbumArtFromPath(string path)
        {
            if (!File.Exists(path)) return;
            else albumArt = Utility.GetBytesFromStream(File.OpenRead(path));
        }

        public List<TempoData> ReadTempoEvents(MidiEventCollection events)
        {
            var tempList = new List<TempoData>();
            foreach (var eventList in events)
            {
                foreach (var e in eventList)
                {
                    if (e is TempoEvent)
                    {
                        TempoEvent tempo = (e as TempoEvent);
                        tempList.Add(new TempoData((int)tempo.AbsoluteTime, (ulong)tempo.MicrosecondsPerQuarterNote));
                    }
                }
            }
            return tempList;
        }

        private static void CheckPath(string filePath)
        {
            if (!File.Exists(filePath))
                throw new ArgumentException("Audica file path doesn't exist", filePath);
            else if (!filePath.Contains(".audica"))
                throw new ArgumentException("File path doesn't lead to an .audica file", filePath);
        }

        private static T ReadJsonEntry<T>(ZipArchive zip, string entryName)
        {
            if (zip.GetEntry(entryName) == null) return default(T);
            var descStream = zip.GetEntry(entryName)?
                .Open();

            if (descStream != null)
            {
                using (var reader = new StreamReader(descStream))
                {
                    string text = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(text);
                }
            }
            else return default(T);
        }

        public static AudicaMetadata GetMetadata(string filePath)
        {
            CheckPath(filePath);
            ZipArchive zip = ZipFile.OpenRead(filePath);
            var desc = ReadJsonEntry<Description>(zip, "song.desc");
            
            bool expert, advanced, moderate, beginner;
            expert = advanced = moderate = beginner = false;

            foreach (var entry in zip.Entries)
            {
                switch (entry.Name)
                {
                    case "expert.cues":
                        expert = true;
                        break;
                    case "advanced.cues":
                        advanced = true;
                        break;
                    case "moderate.cues":
                        moderate = true;
                        break;
                    case "beginner.cues":
                        beginner = true;
                        break;
                    default:
                        break;
                };
            }

            return new AudicaMetadata(desc, expert, advanced, moderate, beginner, new FileInfo(filePath));
        }

        public void Export(string filePath)
        {
            using (FileStream zipFile = File.Open(filePath, FileMode.Create))
            {
                using (var zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Create, false))
                {
                    AddEntryFromStream(zipArchive, "song.desc", desc.GetMemoryStream());
                    
                    if(expert != null) AddEntryFromStream(zipArchive, "expert.cues", expert.GetMemoryStream());
                    if (advanced != null) AddEntryFromStream(zipArchive, "advanced.cues", advanced.GetMemoryStream());
                    if (moderate != null) AddEntryFromStream(zipArchive, "moderate.cues", moderate.GetMemoryStream());
                    if (beginner != null) AddEntryFromStream(zipArchive, "beginner.cues", beginner.GetMemoryStream());

                    AddEntryFromStream(zipArchive, moggSong.moggPath, song.GetMemoryStream());
                    if (this.songSustainL != null) AddEntryFromStream(zipArchive, moggSongSustainL.moggPath, songSustainL.GetMemoryStream());
                    if (this.songSustainR != null) AddEntryFromStream(zipArchive, moggSongSustainR.moggPath, songSustainR.GetMemoryStream());

                    AddEntryFromStream(zipArchive, desc.moggSong, moggSong.GetMemoryStream());

                    AddEntryFromStream(zipArchive, desc.midiFile, ExportTempoEvents());
                }

            }
        }
        private void AddEntryFromStream(ZipArchive zip, string entryName, MemoryStream ms)
        {
            var zipEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
            using (var zipEntryStream = zipEntry.Open())
            {
                ms.CopyTo(zipEntryStream);
            }
        }

        private void AddEntryFromStream(ZipArchive zip, string entryName, Stream stream)
        {
            var zipEntry = zip.CreateEntry(entryName);
            using (var zipEntryStream = zipEntry.Open())
            {
                stream.CopyTo(zipEntryStream);
            }
        }

        private MemoryStream ExportTempoEvents()
        {
            MidiEventCollection events = new MidiEventCollection(0, 480);
            foreach (var tempo in tempoData)
            {
                events.AddEvent(new TempoEvent((int)tempo.microsecondsPerQuarterNote, (long)tempo.tick), 0);
            }
            events.PrepareForExport();

            return Utility.ExportMidiToStream(events);
        }

        public string GetHashedSongID()
        {
            return fileName + "_" + GetHash();
        }

        private string GetHash()
        {
            string expertHash = CreateHashForDifficulty(expert);
            string advancedHash = CreateHashForDifficulty(advanced);
            string moderateHash = CreateHashForDifficulty(moderate);
            string beginnerHash = CreateHashForDifficulty(beginner);
            return Utility.CreateMD5(beginnerHash + moderateHash + advancedHash + expertHash);
        }

        private string CreateHashForDifficulty(Difficulty difficulty)
        {
            //A tempo descriptor is a semi-color separated list of tempos, if theres a single tempo then we only append the first tempo. Else we use "tempo;tick;" for each tempochange
            string tempoDescriptor = "";
            List<TempoData> tempTempoData = new List<TempoData>(tempoData);

            int lastTempo = 0;
            foreach (var tempoChange in tempoData) // Remove all bpm markers that don't change the current tempo(for eg. metronome resets)
            {
                int tempo = (int)Math.Round(TempoData.GetBPMFromMicrosecondsPerQuaterNote(tempoChange.microsecondsPerQuarterNote), MidpointRounding.AwayFromZero);
                if (tempo == lastTempo) tempTempoData.Remove(tempoChange);
                lastTempo = tempo;
            }

            if (tempoData[0].tick == 0 && tempTempoData.Count < 2) tempoDescriptor += ((int)Math.Round(TempoData.GetBPMFromMicrosecondsPerQuaterNote(tempoData[0].microsecondsPerQuarterNote), MidpointRounding.AwayFromZero)).ToString() + ";";
            else
            {
                foreach (var tempoChange in tempTempoData)
                {
                    int tempo = (int)Math.Round(TempoData.GetBPMFromMicrosecondsPerQuaterNote(tempoChange.microsecondsPerQuarterNote), MidpointRounding.AwayFromZero);
                    tempoDescriptor += tempo.ToString() + ";" + tempoChange.tick.ToString() + ";";

                }
                tempoDescriptor += ";"; //We add an extra semi-colon if there are multiple bpms
            }
            string scoreDataDescriptor = "500;750;750;1000;1000;1000;10;4;"; //ScoreData.GetDescriptor
            string scoringDescriptor = "0.500;2.500;2.000;100.000;100.000;0.975;0.250;0.600;"; //KataConfig.GetScoringDescriptor
            
            string cueDescriptors = "";
            if (difficulty != null)
            {
                foreach (var cue in difficulty.cues)
                {
                    cueDescriptors += cue.GetDescriptor();
                } 
            }
            return Utility.CreateMD5(tempoDescriptor + scoreDataDescriptor + scoringDescriptor + cueDescriptors);
        }

        public struct AudicaMetadata
        {
            public Description desc;
            public bool hasExpert;
            public bool hasAdvanced;
            public bool hasModerate;
            public bool hasBeginner;
            public FileInfo fileInfo;
            public string weakHash;

            public AudicaMetadata(Description desc, bool hasExpert, bool hasAdvanced, bool hasModerate,
                bool hasBeginner, FileInfo fileInfo)
            {
                this.desc = desc;
                this.hasExpert = hasExpert;
                this.hasAdvanced = hasAdvanced;
                this.hasModerate = hasModerate;
                this.hasBeginner = hasBeginner;
                this.fileInfo = fileInfo;
                this.weakHash = Utility.CreateMD5(desc.songID + fileInfo.Length);
            }
        }

        public IEnumerator<Difficulty> GetEnumerator()
        {
            foreach (var difficulty in new Difficulty[4] {beginner, moderate, advanced, expert})
                if (difficulty != null)
                    yield return difficulty;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Equals(Audica other) => GetHashedSongID() == other?.GetHashedSongID();
    }
    
}