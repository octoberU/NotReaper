using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NotReaper;
using NotReaper.Models;
using System.IO;
using System;
using NotReaper.IO;
using NotReaper.Targets;

public class UISustainHandler : MonoBehaviour
{
    public static UISustainHandler Instance = null;
    public static bool PendingDelete { get; set; } = false;
    public DisplaySliderCombo volumeSlider;
    public Slider SustainVol;
    //public AudioSource SustainL;
    //public AudioSource SustainR;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI loadSustainButtonTextLeft;
    public TextMeshProUGUI loadSustainButtonTextRight;
    public GameObject deleteSustainButtonLeft;
    public GameObject deleteSustainButtonRight;
    public Image leftButtonPanel;
    public Image rightButtonPanel;
    public MoggSong sustainSongLeft { get; set; }
    public MoggSong sustainSongRight { get; set; }

    //public bool isPlayingSustains = false;

    public Color defaultColor;
    public Color loadedColor;

    public static SustainTrack LoadedTracks { get; set; } = SustainTrack.None;

   //private readonly Target target;

    private void Awake()
    {
        if(Instance is null)
        {
            Instance = this;
            volumeSlider.OnValueChanged += UpdateVolume;
        }
        else
        {
            Debug.LogWarning("Trying to create second UISustianHandler instance.");
            return;
        }
        
    }

    

   /* private void Update()
    {


        if (!target.isPlayingSustains == true)
        {
            SustainL.mute = true;
            SustainR.mute = true;
        }
        else
        {
            SustainL.mute = false;
            SustainR.mute = false;
        }
    } */



    public void UpdateVolume(float value)
    {
        switch (LoadedTracks)
        {
            case SustainTrack.Both:
                sustainSongLeft.SetVolume(value, true);
                sustainSongRight.SetVolume(value, true);
                break;
            case SustainTrack.Left:
                sustainSongLeft.SetVolume(value, true);
                break;
            case SustainTrack.Right:
                sustainSongLeft.SetVolume(value, true);
                break;
            default:
                break;
        }
        
    }

    public void LoadSustainTrack(SustainTrack track)
    {
        FillSustainDescData(track);
        //FillSustainDescData();
        if (!Timeline.instance.ReplaceAudio(Timeline.LoadType.Sustain, track))
        {
            Debug.Log("No valid path selected");
            FillSustainDescData(track, true);
            return;
        }
        UpdateLoadedSustains(track, false);
        if(track == SustainTrack.Left) sustainSongLeft.SetVolume(0f, true);
        else if(track  == SustainTrack.Right) sustainSongRight.SetVolume(0f, true);
        UpdateSustainUI();
        Timeline.instance.Export();
    }

    private void UpdateLoadedSustains(SustainTrack loadedNew, bool delete)
    {
        if (delete)
        {
            switch (LoadedTracks)
            {
                case SustainTrack.Left:
                    if (loadedNew == SustainTrack.Left)
                    {
                        LoadedTracks = SustainTrack.None;
                    }
                    break;
                case SustainTrack.Right:
                    if (loadedNew == SustainTrack.Right)
                    {
                        LoadedTracks = SustainTrack.None;
                    }
                    break;
                case SustainTrack.Both:
                    if(loadedNew == SustainTrack.Left)
                    {
                        LoadedTracks = SustainTrack.Right;
                    }
                    else if(loadedNew == SustainTrack.Right)
                    {
                        LoadedTracks = SustainTrack.Left;
                    }
                    break;
            }
        }
        else
        {
            switch (LoadedTracks)
            {
                case SustainTrack.Left:
                    if (loadedNew == SustainTrack.Right)
                    {
                        LoadedTracks = SustainTrack.Both;
                    }
                    break;
                case SustainTrack.Right:
                    if (loadedNew == SustainTrack.Left)
                    {
                        LoadedTracks = SustainTrack.Both;
                    }
                    break;
                case SustainTrack.None:
                    LoadedTracks = loadedNew;
                    break;
            }
        }
        
    }

    public void DeleteSustainTrack(SustainTrack track)
    {
        PendingDelete = true;
        UpdateLoadedSustains(track, true);
        FillSustainDescData(track, true);
        Timeline.instance.Export();
        UpdateSustainUI();
        if(track == SustainTrack.Left) Timeline.audicaFile.usesLeftSustain = false;
        else if(track == SustainTrack.Right) Timeline.audicaFile.usesRightSustain = false;
        PendingDelete = false;
    }

    private void FillSustainDescData(SustainTrack track, bool clear = false)
    {
        if (clear)
        {
            if(track == SustainTrack.Left)
            {
                Timeline.audicaFile.desc.sustainSongLeft = "";
                Timeline.audicaFile.desc.moggSustainSongLeft = "";
            }
            else if(track == SustainTrack.Right)
            {
                Timeline.audicaFile.desc.sustainSongRight = "";
                Timeline.audicaFile.desc.moggSustainSongRight = "";
            }
        }
        else
        {
            if(track == SustainTrack.Left)
            {
                Timeline.audicaFile.desc.sustainSongLeft = "song_sustain_l.moggsong";
                Timeline.audicaFile.desc.moggSustainSongLeft = "song_sustain_l.mogg";
            }
            else if(track == SustainTrack.Right)
            {
                Timeline.audicaFile.desc.sustainSongRight = "song_sustain_r.moggsong";
                Timeline.audicaFile.desc.moggSustainSongRight = "song_sustain_r.mogg";
            }
        }
        
    }

    private void UpdateSustainUI()
    {
        switch (LoadedTracks)
        {
            case SustainTrack.None:
                statusText.text = "No Sustains Loaded";
                loadSustainButtonTextLeft.text = "Load L";
                loadSustainButtonTextRight.text = "Load R";
                volumeSlider.gameObject.SetActive(false);
                deleteSustainButtonLeft.SetActive(false);
                deleteSustainButtonRight.SetActive(false);
                leftButtonPanel.color = defaultColor;
                rightButtonPanel.color = defaultColor;

                break;
            case SustainTrack.Left:
                statusText.text = "Left Sustain Loaded";
                loadSustainButtonTextLeft.text = "Replace L";
                loadSustainButtonTextRight.text = "Load R";
                volumeSlider.gameObject.SetActive(true);
                deleteSustainButtonLeft.SetActive(true);
                deleteSustainButtonRight.SetActive(false);
                leftButtonPanel.color = loadedColor;
                rightButtonPanel.color = defaultColor;
                break;
            case SustainTrack.Right:
                statusText.text = "Right Sustain Loaded";
                loadSustainButtonTextLeft.text = "Load L";
                loadSustainButtonTextRight.text = "Replace R";
                volumeSlider.gameObject.SetActive(true);
                deleteSustainButtonLeft.SetActive(false);
                deleteSustainButtonRight.SetActive(true);
                leftButtonPanel.color = defaultColor;
                rightButtonPanel.color = loadedColor;
                break;
            case SustainTrack.Both:
                statusText.text = "Both Sustains Loaded";
                loadSustainButtonTextLeft.text = "Replace L";
                loadSustainButtonTextRight.text = "Replace R";
                volumeSlider.gameObject.SetActive(true);
                deleteSustainButtonLeft.SetActive(true);
                deleteSustainButtonRight.SetActive(true);
                leftButtonPanel.color = loadedColor;
                rightButtonPanel.color = loadedColor;
                break;
            default:
                break;
        }
    }

    public void LoadVolume(bool hasLeftSustain, bool hasRightSustain)
    {
        if (hasLeftSustain && hasRightSustain) LoadedTracks = SustainTrack.Both;
        else if (hasLeftSustain) LoadedTracks = SustainTrack.Left;
        else if (hasRightSustain) LoadedTracks = SustainTrack.Right;
        else LoadedTracks = SustainTrack.None;
        UpdateSustainUI();
        switch (LoadedTracks)
        {
            case SustainTrack.Left:
            case SustainTrack.Both:
                Debug.Log("Vol: " + sustainSongLeft.volume.l.ToString());
                volumeSlider.value = sustainSongLeft.volume.l;
                break;
            case SustainTrack.Right:
                volumeSlider.value = sustainSongRight.volume.r;
                break;
            default:
                break;
        }
        
        /*
        if(hasLeftSustain && hasRightSustain)
        statusText.text = "Sustains loaded";
        if (!hasLeftSustain)
        {
            loadSustainButtonTextLeft.text = "Load";
            statusText.text = "Right Sustain loaded";

        }
        if (!hasRightSustain)
        {
            loadSustainButtonTextLeft.text = "Load";
            statusText.text = "No Sustains loaded";
            volumeSlider.gameObject.SetActive(false);
            deleteSustainButtonLeft.SetActive(false);
            return;
        }
        loadSustainButtonTextLeft.text = "Replace";
        statusText.text = "Sustains loaded";
        volumeSlider.gameObject.SetActive(true);
        deleteSustainButtonLeft.SetActive(true);
        volumeSlider.value = sustainSongLeft.volume.l;
        */
    }

    public void UpdateSustainTrackLeft(bool delete)
    {
        if (delete) DeleteSustainTrack(SustainTrack.Left);
        else LoadSustainTrack(SustainTrack.Left);
    }

    public void UpdateSustainTrackRight(bool delete)
    {
        if (delete) DeleteSustainTrack(SustainTrack.Right);
        else LoadSustainTrack(SustainTrack.Right);
    }

    public enum SustainTrack
    {
        None,
        Left,
        Right,
        Both
    }

    

}
