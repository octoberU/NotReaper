using System.Reflection;
using NUnit.Framework;
using TargetPreview.Targets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace TargetPreview.Editor.Tests.TargetPreviewLogicTests
{
    public class RequireMockSceneTest : RequireMockFileTest
    {
        [SetUp]
        public void Setup() =>
            EditorSceneManager.OpenScene("Assets/Scenes/TargetPreviewMain.unity");
        
    }
}