using UnityEngine;

namespace TargetPreview.Editor.Tests.TargetPreviewLogicTests.ManagerTests
{
    public class ManagerTestBase<T> : RequireMockSceneTest where T : Object
    {
        public T Manager =>
            Object.FindObjectOfType<T>();
    }
}