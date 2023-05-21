using System.Linq;
using NUnit.Framework;
using TargetPreview.Scripts.Targets;
using TargetPreview.Scripts.Targets.Extensions;
using UnityEngine;
using UnityEngine.TestTools;

namespace TargetPreview.Editor.Tests.TargetPreviewLogicTests.ManagerTests
{
    public class CueManagerTests : ManagerTestBase<CueManager>
    {
        [SetUp]
        public void SetUp()
        {
            LoadTestCues();
        }
        
        [Test]
        public void Test_cues_are_ordered()
        {
            var ordered = Manager
                .TargetCues
                .OrderBy(x => x.timeMs)
                .ThenBy(x => (int)x.behavior)
                .ThenBy(x => (int)x.handType);
            
            Assert.IsTrue(ordered.SequenceEqual(Manager.TargetCues), "Cues weren't ordered correctly");
        }
        
        [Test]
        public void Test_active_cues()
        {
            bool activeCuesAlwaysEmpty = 
                OnTimeStep(10000, 15, 
                    () => Manager.ActiveCues.Any())
                    .All(x => false);
            
            Assert.IsFalse(activeCuesAlwaysEmpty, "Active cues were never assigned when stepping through time");
        }
        

        public bool[] OnTimeStep(float time, int iterations, System.Func<bool> testPerStep)
        {
            var output = new bool[iterations];
            for (var i = 0; i < output.Length; i++)
            {
                Manager.OnTimeUpdated(time * i);
                output[i] = testPerStep();
            }

            return output;
        }

        internal void LoadTestCues() =>
            Manager.TargetCues = GetTestAudica().expert.cues.AsTargetCues();
    }
}