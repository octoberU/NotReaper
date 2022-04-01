using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NotReaper.Timing;
using System.Collections.Generic;
using NotReaper.Models;
using NotReaper;
using NotReaper.Utility;

public class AudioWaveformVisualizer : MonoBehaviour
{
    public GameObject waveformSegmentInstance;
    public int sortingOrder = 0;
    public bool isSongAudio = true;

    const uint NumQuarterNotesSampled = 4;

    const UInt64 texturePerTickDuration = NumQuarterNotesSampled;
    const UInt64 PixelsPerQuarterNote = 128;
    const UInt64 SecondsPerTexture = 16;

    public bool visible = true;

    public delegate void OnWaveformGenerated(List<GameObject> segments);
    public event OnWaveformGenerated onWaveformGenerated;

    private List<GameObject> segments = new List<GameObject>();

    private int activeGenerations = 0;

    struct GenerationSections
    {
        public float start;
        public float end;
    };

    public List<GameObject> GetSegments()
    {
        return segments;
    }

    public void SetWaveformVisible(bool visible)
    {
        foreach (Renderer r in gameObject.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = visible;
        }
        this.visible = visible;
    }

    public void ToggleWaveform()
    {
        SetWaveformVisible(!visible);
    }

    public void ClearWaveform()
    {
        for (int i = segments.Count - 1; i >= 0; i--)
        {
            Destroy(segments[i]);
        }
        segments.Clear();
    }


    public void GenerateWaveform(ClipData aud, Timeline timeline)
    {
        StopAllCoroutines();
        segments.Clear();
        activeGenerations = 0;
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        if (!EditorTempo.HasTempoChanges())
        {
            return;
        }

        //Ensure all timestamps are different, otherwise texture generation is very crashy
        var tempoChanges = EditorTempo.TempoChanges;
        QNT_Timestamp lastTime = tempoChanges[0].time;
        for (int i = 1; i < tempoChanges.Count; ++i)
        {
            if (lastTime == tempoChanges[i].time)
            {
                return;
            }

            lastTime = tempoChanges[i].time;
        }
        int nextTempoChange = 1;

        List<GenerationSections> sections = new List<GenerationSections>();
        for (float t = 0; t < aud.Length;)
        {
            float endOfSection = t + SecondsPerTexture;
            if (endOfSection > aud.Length)
            {
                endOfSection = aud.Length;
            }

            if (nextTempoChange < tempoChanges.Count)
            {
                float nextTempoChangeSec = tempoChanges[nextTempoChange].time.ToSeconds();

                if (endOfSection >= nextTempoChangeSec)
                {
                    endOfSection = nextTempoChangeSec;
                    ++nextTempoChange;
                }
            }

            GenerationSections sec;
            sec.start = t;
            sec.end = endOfSection;
            sections.Add(sec);

            t = endOfSection;
        }
        int index = 1;
        foreach (GenerationSections gen in sections)
        {
            int sampleStart = (int)(gen.start * aud.frequency * aud.channels);
            int sampleEnd = (int)(gen.end * aud.frequency * aud.channels);

            QNT_Timestamp startTick = QNT_Timestamp.ShiftTick(gen.start);
            UInt64 microsecondsPerQuarterNote = EditorTempo.TempoChanges[EditorTempo.GetCurrentBPMIndex(startTick)].microsecondsPerQuarterNote;

            float beatTime = Conversion.ToQNT(gen.end - gen.start, microsecondsPerQuarterNote).ToBeatTime();
            StartCoroutine(PaintWaveformSpectrum(aud.samples, sampleStart, sampleEnd - sampleStart, (int)(beatTime * PixelsPerQuarterNote), 64, isSongAudio ? NRSettings.config.waveformColor : NRSettings.config.sustainWaveformColor,
                delegate (Texture2D tex)
                {
                    GameObject obj = GameObject.Instantiate(waveformSegmentInstance, new Vector3(0, 0, 0), Quaternion.identity, gameObject.transform);
                    obj.name = "Segment " + index;
                    index++;
                    QNT_Timestamp start = QNT_Timestamp.ShiftTick(gen.start);
                    QNT_Timestamp end = QNT_Timestamp.ShiftTick(gen.end);

                    obj.GetComponent<MeshFilter>().mesh = CreateMesh(start.ToBeatTime(), new QNT_Duration((UInt64)(end.tick - start.tick)).ToBeatTime(), 1);
                    obj.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", tex);
                    obj.GetComponent<MeshRenderer>().enabled = false;
                    obj.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
                    obj.GetComponent<Transform>().localPosition = new Vector3(0, isSongAudio ? -.5f : -.25f, 0);
                    obj.GetComponent<Transform>().localScale = new Vector3(1f, isSongAudio ? 1f : .5f, 1f);
                    segments.Add(obj);
                    SetWaveformVisible(visible);
                    activeGenerations -= 1;
                    if(activeGenerations == 0)
                    {
                        onWaveformGenerated?.Invoke(segments);
                    }
                }
            ));
        }
    }

    public Mesh CreateMesh(float startX, float width, float height)
    {
        var mesh = new Mesh();

        var vertices = new Vector3[4]
        {
            new Vector3(startX, 0, 0),
            new Vector3(startX + width, 0, 0),
            new Vector3(startX, height, 0),
            new Vector3(startX + width, height, 0)
        };
        mesh.vertices = vertices;

        var tris = new int[6]
        {
            // lower left triangle
            0, 2, 1,
            // upper right triangle
            2, 3, 1
        };
        mesh.triangles = tris;

        var normals = new Vector3[4]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        mesh.normals = normals;

        var uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.uv = uv;

        return mesh;
    }

    public delegate void TextureCallback(Texture2D data);

    IEnumerator PaintWaveformSpectrum(float[] samples, int sampleStart, int sampleSection, int width, int height, Color col, TextureCallback cb)
    {
        if (width <= 0)
        {
            yield break;
        }
        
        activeGenerations += 1;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        //Set all to clear
        Color32 resetColor = new Color32(255, 255, 255, 0);
        Color32[] resetColorArray = tex.GetPixels32();
        for (int i = 0; i < resetColorArray.Length; i++)
        {
            resetColorArray[i] = resetColor;
        }
        tex.SetPixels32(resetColorArray);
        float sampleIncr = sampleSection / (float)width;

        const int PIXELS_PER_YIELD = 32; //Waits every 400,000 loop
        int loopCounter = 0;

        for (int x = 0; x < width; x++)
        {
            float maxValue = 0;
            float avgValue = 0;
            for (int s = 0; s < sampleIncr; s++)
            {
                float sampleIdx = sampleStart + x * sampleIncr + s;
                int idx = Math.Min((int)sampleIdx, samples.Length - 1);
                float sampleVal = Math.Abs(samples[idx]);
                avgValue += sampleVal * sampleVal;
                maxValue = Math.Max(maxValue, sampleVal * sampleVal);
            }
            avgValue /= sampleIncr;

            //For sections with huge peaks, just use avg
            if (maxValue > 0.5f)
            {
                maxValue = avgValue;
            }

            float paintHeight = Mathf.Sqrt(maxValue) * height;
            for (int y = 0; y <= paintHeight; y++)
            {
                tex.SetPixel(x, (height / 2) + y, col);
                tex.SetPixel(x, (height / 2) - y, col);
            }

            ++loopCounter;
            if (loopCounter % PIXELS_PER_YIELD == 0)
            {
                yield return null;
            }
        }
        tex.Apply();

        cb(tex);
    }

}