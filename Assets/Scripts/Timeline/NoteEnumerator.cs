using NotReaper.Targets;
using NotReaper.Timing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper
{
    public class NoteEnumerator : IEnumerable<Target>
    {
        public NoteEnumerator(QNT_Timestamp start, QNT_Timestamp end)
        {
            this.start = start;
            this.end = end;
        }

        public QNT_Timestamp start;
        public QNT_Timestamp end;

        public bool startInclusive = true;
        public bool endInclusive = true;
        public bool reverse = false;

        public IEnumerator<Target> GetEnumerator()
        {
            if (EditorNotes.OrderedNotes.Count == 0)
            {
                yield break;
            }

            var result = TargetFinder.BinarySearchOrderedNotes(start);
            int index = result.index;

            //Invalid index? No iteration
            if (index >= EditorNotes.OrderedNotes.Count)
            {
                yield break;
            }

            //We didn't find an exact result, so search for the nearest
            if (!result.found)
            {

                //Go back until we find a note with a time less then the start
                while (index >= 0)
                {
                    QNT_Timestamp time = EditorNotes.OrderedNotes[index].data.time;
                    if (time < start)
                    {
                        break;
                    }

                    --index;
                }

                if (index < 0)
                {
                    index = 0;
                }

                //Increment up to and including start
                while (EditorNotes.OrderedNotes[index].data.time < start)
                {
                    ++index;
                }
            }

            //Invalid index? No iteration
            if (index >= EditorNotes.OrderedNotes.Count)
            {
                yield break;
            }

            //If we are not inclusive to starting time, then move up until we get a time after the start
            if (!startInclusive)
            {
                while (EditorNotes.OrderedNotes[index].data.time <= start)
                {
                    ++index;
                }
            }

            //Iterate over the valid notes
            if (!reverse)
            {
                for (int i = index; i < EditorNotes.OrderedNotes.Count; ++i)
                {
                    if (EditorNotes.OrderedNotes[i].data.time > end)
                    {
                        yield break;
                    }

                    if (!endInclusive && EditorNotes.OrderedNotes[i].data.time == end)
                    {
                        yield break;
                    }

                    yield return EditorNotes.OrderedNotes[i];
                }
            }
            else
            {
                int endIndex = index;

                while (endIndex < EditorNotes.OrderedNotes.Count && EditorNotes.OrderedNotes[endIndex].data.time < end)
                {
                    ++endIndex;
                }

                if (endInclusive)
                {
                    while (endIndex < EditorNotes.OrderedNotes.Count && EditorNotes.OrderedNotes[endIndex].data.time <= end)
                    {
                        ++endIndex;
                    }

                    --endIndex;
                }

                if (endIndex >= EditorNotes.OrderedNotes.Count)
                {
                    endIndex = EditorNotes.OrderedNotes.Count - 1;
                }

                for (int i = endIndex; i >= index; --i)
                {
                    yield return EditorNotes.OrderedNotes[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

    }

}
