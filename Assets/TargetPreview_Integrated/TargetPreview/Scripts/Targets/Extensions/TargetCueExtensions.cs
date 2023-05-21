using System.Collections.Generic;
using System.Linq;
using AudicaTools;

namespace TargetPreview.Scripts.Targets.Extensions
{
    public static class TargetCueExtensions
    {
        public static TargetCue[] AsTargetCues(this IEnumerable<Cue> cues)
        {
            var cuesSorted = 
                cues
                    .OrderBy(x => x.tick)
                    .ThenBy(x => (int)x.behavior)
                    .ThenBy(x => (int)x.handType)
                    .ToArray();
            
            List<TargetCue> output = new();
            Dictionary<Cue.HandType, List<TargetCue>> chainNodes = new()
            {
                { Cue.HandType.Left, new() },
                { Cue.HandType.Right, new() },
                { Cue.HandType.Either, new() },
                { Cue.HandType.None, new() },
            };
            
            for (var index = cuesSorted.Length - 1; index >= 0; index--)
            {
                var cue = cuesSorted[index];

                switch (cue.behavior)
                {
                    case Cue.Behavior.Chain:
                        chainNodes[cue.handType].Add(cue);
                        break;
                    case Cue.Behavior.ChainStart:
                        TargetCue targetCue = cue;
                        targetCue.children = chainNodes[cue.handType].OrderBy(x => x.timeMs).ToArray();
                        targetCue.timeEndMs = chainNodes.Any() ? chainNodes[cue.handType].First().timeEndMs : targetCue.timeEndMs;
                        output.Add(targetCue);
                        chainNodes[cue.handType].Clear();
                        break;
                    default:
                        output.Add(cue);
                        break;
                }
            }

            return output
                .OrderBy(x => x.tick)
                .ThenBy(x => (int)x.behavior)
                .ThenBy(x => (int)x.handType)
                .ToArray();
        }
    }
}