using System.Linq;
using AudicaTools;
using NUnit.Framework;
using TargetPreview.Scripts.Targets;
using TargetPreview.Scripts.Targets.Extensions;
using TargetPreview.Targets;
using UnityEngine;


public class TargetPreviewConversionTests : RequireMockFileTest
{
    [Test]
    public void Test_cues_to_targetcues_conversion()
    {
        audica.expert.cues
            .TakeLast(10)
            .ToList()
            .ForEach(cue =>
            {
                TargetCue targetCue = (TargetCue)cue;
                Assert.AreEqual(targetCue.timeMs, TempoData.TickToMilliseconds(cue.tick, audica.tempoData), "Time in ms hasn't been converted."); 
                Assert.AreEqual(targetCue.tick, cue.tick, "Tick hasn't been converted.");
                Assert.AreEqual(new Cue.GridOffset(){x = targetCue.xOffset, y = targetCue.yOffset}, cue.gridOffset, "Grid offset hasn't been converted.");
                Assert.AreEqual(targetCue.behavior.ToString(), cue.behavior.ToString(), "Target behavior hasn't been converted.");
                Assert.AreEqual(targetCue.handType.ToString(), cue.handType.ToString(), "Target hand type hasn't been converted.");
            });
    }
    
    [Test]
    public void Test_chain_conversion()
    {
        audica.expert.cues
            .AsTargetCues()
            .Where(cue => cue.behavior == TargetBehavior.ChainStart)
            .ToList()
            .ForEach(chainStart =>
            {
                var chainNodes = chainStart.children;
                Assert.IsTrue(chainNodes.Any(), "Chain start doesn't have any child nodes after conversion.");
                if(chainNodes.Length > 1) Assert.IsTrue(chainNodes.First().timeMs < chainNodes.Last().timeMs, "Chain start children aren't in order.");
                Assert.IsTrue(chainStart.timeEndMs == chainNodes.Last().timeEndMs, "Chain start end time isn't the same as the last child's time.");
            });
    }

    [Test]
    public void Test_cues_are_sorted()
    {
       var convertedCues = audica.expert.cues
            .AsTargetCues();

       var sortedCues = 
           convertedCues
           .OrderBy(x => x.timeMs)
           .ThenBy(x => (int)x.behavior)
           .ThenBy(x => (int)x.handType);

       Assert.IsTrue(convertedCues.SequenceEqual(sortedCues));
    }
}