using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
namespace NotReaper.Models
{
    public class MoggSong
    {
        public MoggVol volume;
        public string moggPath;
        public MoggVol pan;
        public string[] moggString;

        public MoggSong(MemoryStream ms, bool isSustain = false)
        {
            StreamReader reader = new StreamReader(ms);
            moggString = Encoding.UTF8.GetString(ms.ToArray()).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in moggString)
            {
                if (line.Contains("(vols")) this.volume = GetMoggVolFromLine(line, isSustain);
                if (line.Contains("(pans")) this.pan = GetMoggVolFromLine(line, isSustain);
            }
        }
        public MoggSong() { }
        public MoggVol GetMoggVolFromLine(string line, bool isSustain)
        {
            try
            {
                var split = line.Split(new char[] { '(', ')' });
                string[] values;
                if (isSustain)
                {
                    return new MoggVol(float.Parse(split[2], new CultureInfo("en-US")), float.Parse(split[2], new CultureInfo("en-US")));
                }
                if (split[2].Contains("    ")) values = split[2].Split(new string[] { "    " }, StringSplitOptions.None);
                else if (split[2].Contains("   ")) values = split[2].Split(new string[] { "   " }, StringSplitOptions.None);
                else if (split[2].Contains("  ")) values = split[2].Split(new string[] { "  " }, StringSplitOptions.None);
                else values = split[2].Split(new string[] { " " }, StringSplitOptions.None);

                return new MoggVol(float.Parse(values[0], new CultureInfo("en-US")), float.Parse(values[1], new CultureInfo("en-US")));
                
            }
            catch (Exception)
            {
                UnityEngine.Debug.LogWarning("Moggsong is invalid. Using defaults instead");
                if (line.Contains("(vols")) return new MoggVol(0f, 0f);
                else return new MoggVol(-1, 1);

            }
        }

        public string ExportToText(bool isSustain)
        {
            if (moggString == null) return "";

            string[] exportString = moggString;
            if (exportString.Length == 0) return "";
            int volIndex = 0;
            int panIndex = 0;
            for (int i = 0; i < exportString.Length; i++)
            {
                exportString[i].Replace("\n", "");
                if (exportString[i].Contains("(vols")) volIndex = i;
                if (exportString[i].Contains("(pans")) panIndex = i;
            }
            if (isSustain)
            {
                exportString[volIndex] = $"(vols ({volume.l.ToString("n2", new CultureInfo("en-US"))}))";
                exportString[panIndex] = $"(pans ({pan.l.ToString("n2", new CultureInfo("en-US"))}))";
            }
            else
            {
                exportString[volIndex] = $"(vols ({volume.l.ToString("n2", new CultureInfo("en-US"))}   {volume.r.ToString("n2", new CultureInfo("en-US"))}))";
                exportString[panIndex] = $"(pans ({pan.l.ToString("n2", new CultureInfo("en-US"))}   {pan.r.ToString("n2", new CultureInfo("en-US"))}))";
            }
            
            return string.Join(Environment.NewLine, exportString);
        }

        public void SetVolume(float value, bool isSustain)
        {
            this.volume.l = this.volume.r = value;
            int val = isSustain ? 0 : 1;
            this.pan.l = val * -1;
            this.pan.r = val;
        }
    }

    public struct MoggVol
    {
        public float l;
        public float r;

        public MoggVol(float l, float r)
        {
            this.l = l;
            this.r = r;
        }
    }
}
