using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class RecentAudicaFiles : MonoBehaviour
{
    public static List<string> AudicaPaths { get; private set; }
    private static string recentsFilePath;

    public delegate void OnRecentsLoaded();
    public static event OnRecentsLoaded onRecentsLoaded;

    private void Awake()
        => recentsFilePath = Path.Combine(Application.persistentDataPath, "RecentDirs.json");

    private void Start() => LoadRecents();

    public static void AddRecentDir(string dir)
    {
        if (AudicaPaths.Contains(dir)) AudicaPaths.Remove(dir);
        AudicaPaths.Insert(0, dir);
        if (AudicaPaths.Count > 6) AudicaPaths = AudicaPaths.GetRange(0, 6);
        SaveRecents();
    }

    public static void SaveRecents()
        => SaveRecentsAsync();

    private static async void SaveRecentsAsync()
    {
        string text = JsonConvert.SerializeObject(AudicaPaths);
        await File.WriteAllTextAsync(recentsFilePath, text);
        LoadRecentsAsync();
    }

    private static async void LoadRecentsAsync()
    {
        if (AudicaPaths != null)
        {
            onRecentsLoaded?.Invoke();
            return;
        }

        
        if (File.Exists(recentsFilePath))
        {
            try
            {
                string text = await File.ReadAllTextAsync(recentsFilePath);
                AudicaPaths = JsonConvert.DeserializeObject<List<string>>(text);
            }
            catch (Exception)
            {
                throw;
            }
        }
        else
        {
            AudicaPaths = new List<string>();
        }
        for (int i = AudicaPaths.Count - 1; i >= 0; i--)
        {
            if (!File.Exists(AudicaPaths[i]))
            {
                AudicaPaths.RemoveAt(i);
            }
        }

        onRecentsLoaded?.Invoke();
    }

    public static void LoadRecents()
        => LoadRecentsAsync();

    public static void ClearRecents()
    {
        AudicaPaths = new List<string>();
        SaveRecents();
    }

}
