using System.Collections;
using System.Collections.Generic;
using AudicaTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

public class TestFileLoading : RequireMockFileTest
{
    [Test]
    public void Test_Audica_not_null_after_loading() =>
        Assert.IsNotNull(audica, "Audica file loading failed");

    [Test]
    public void Test_Audica_contains_data()
    {
        Assert.IsTrue(audica.expert.cues.Count > 0, "Cues haven't been loaded correctly.");
        Assert.IsNull(audica.advanced, "Cues have been loaded for a difficulty that doesn't exist.");
    }

    [Test]
    public void Test_tempos_exist()
    {
        Assert.IsNotNull(audica.tempoData, "Tempos haven't been loaded correctly.");
        Assert.IsNotEmpty(audica.tempoData, "The audica file has no tempos.");
    }
}