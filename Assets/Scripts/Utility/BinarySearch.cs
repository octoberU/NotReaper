using NotReaper.Timing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Utility
{
    public class BinarySearch
    {
        public static BinarySearchResult SearchBPMIndex(float seconds)
        {
            BinarySearchResult result;
            var tempoChanges = EditorTempo.TempoChanges;
            int min = 0;
            int max = tempoChanges.Count - 1;
            while (min <= max)
            {
                int mid = (min + max) / 2;
                float midCueTime = tempoChanges[mid].secondsFromStart;
                if (seconds == midCueTime)
                {
                    while (mid != 0 && tempoChanges[mid - 1].secondsFromStart == seconds)
                    {
                        --mid;
                    }

                    result.index = mid;
                    result.found = true;
                    return result;
                }
                else if (seconds < midCueTime)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            result.index = Math.Min(Math.Max(min, 0), max);
            result.found = false;
            return result;
        }

        public static BinarySearchResult SearchBPMIndex(QNT_Timestamp cueTime)
        {
            BinarySearchResult result;
            var tempoChanges = EditorTempo.TempoChanges;
            int min = 0;
            int max = tempoChanges.Count - 1;
            while (min <= max)
            {
                int mid = (min + max) / 2;
                QNT_Timestamp midCueTime = tempoChanges[mid].time;
                if (cueTime == midCueTime)
                {
                    while (mid != 0 && tempoChanges[mid - 1].time == cueTime)
                    {
                        --mid;
                    }

                    result.index = mid;
                    result.found = true;
                    return result;
                }
                else if (cueTime < midCueTime)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            result.index = Math.Min(Math.Max(min, 0), max);
            result.found = false;
            return result;
        }

        public static int GetCurrentBPMIndex(QNT_Timestamp t)
        {
            var res = SearchBPMIndex(t);

            if (res.index > 0 && EditorTempo.TempoChanges[0].time > t)
            {
                return res.index - 1;
            }

            return res.index;
        }
    }
}
