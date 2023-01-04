using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using NotReaper.Models;

namespace NotReaper.Tools
{
    static class KeyScraper
    {

        public static Dictionary<string, int> pitchEventDict = new Dictionary<string, int>
        {
            {"A", 9},
            {"A#", 10},
            {"B\u266D", 10},
            {"B", 11},
            {"C", 0},
            {"C#", 1},
            {"D\u266D", 1},
            {"D", 2},
            {"D#", 3},
            {"E\u266D", 3},
            {"E", 4},
            {"F", 5},
            {"F#", 6},
            {"G\u266D", 6},
            {"G", 7},
            {"G#", 8},
            {"A\u266D", 8},
        };

        static int scrapeCount = 0;
        static string content = "";

        public static int GetSongEndEvent(string artist, string songName)
        {
            artist = Regex.Replace(artist, @"\s+\(.+\)", "", RegexOptions.IgnoreCase); //Remove anything within parenthesees
            artist = Regex.Replace(artist, @"\s+\(?Feat\..+", "", RegexOptions.IgnoreCase); //Remove feat.
            songName = Regex.Replace(songName, @"\s+\(.+\)", "", RegexOptions.IgnoreCase); //Remove anything within parenthesees, such as (TV Size) or (Cut ver.)

            string key = GetKey($@"{artist} {songName}");

            if (key == "") return 1;
            else
            {
                string pitch = key.Split(' ')[0];

                if (pitchEventDict.ContainsKey(pitch)) return pitchEventDict[pitch];
                else return 1;
            }
        }

        public static string GetKey(string search)
        {
            content = "";
            if (scrapeCount >= 14) return "";
            try
            {
                WebClient client = new WebClient();
                string query = HttpUtility.UrlEncode(search);

                scrapeCount++;
                content = client.DownloadString($@"https://songdata.io/search?query={query}");
            }
            catch
            {
                return "";
            }

            Match match = Regex.Match(content, @">.{1,2} (minor|major)", RegexOptions.IgnoreCase);
            if (!match.Success) return "";
            return match.Value.Replace(">", "");

        }

        public static string GetBPM()
        {
            Match match = Regex.Match(content, @"table_bpm\u0022>...", RegexOptions.IgnoreCase);
            if (!match.Success) return "";
            return Regex.Match(match.Value, @"\d+\.*\d+").Value;
        }

        public static string GetArt()
        {
            Match match = Regex.Match(content, @"https:\/\/i.scdn.co\/image\/+\w*", RegexOptions.IgnoreCase);
            if (!match.Success) return "";
            return match.Value.Replace("src=", "").Replace("\u0022", "");
        }

    }
}
