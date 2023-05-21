using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TargetPreview.Display;
using TargetPreview.Scripts.Targets;
using UnityEditor.SceneManagement;

namespace TargetPreview.Editor.Tests.TargetPreviewLogicTests
{
    public class SceneTests : RequireMockSceneTest
    {
        [Test]
        public void Test_managers_are_created()
        {
            List<Type> managers = new()
            {
                typeof(CueManager),
                typeof(TargetManager),
                typeof(TargetConnectorManager),
                typeof(TargetPool),
            };
            
            managers.ForEach(manager =>
            {
                var instance = typeof(UnityEngine.Object)
                    .GetMethods()
                    .Where(x => x.Name == "FindObjectOfType")
                    .FirstOrDefault(x => x.IsGenericMethod)
                    .MakeGenericMethod(manager)
                    .Invoke(null, new object[] { });

                Assert.IsNotNull(instance, "Couldn't find manager of type " + manager.Name + " in scene " + EditorSceneManager.GetActiveScene().name);
            });
        }
    }
}