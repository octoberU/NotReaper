using System.IO;
using AudicaTools;
using NUnit.Framework;
using UnityEngine;

public class RequireMockFileTest
{
    protected Audica audica;
    
    [SetUp]
    public void Setup() =>
        audica = GetTestAudica();
    
    public Audica GetTestAudica()
    {
        string testAudicaPath = Path.Combine(Application.dataPath, "TargetPreview", "Editor", "Tests", "TestResources",
            "Mangekyou-octo.audica");
        string fullPath = Path.GetFullPath(testAudicaPath);

        return new Audica(fullPath);
    }
}