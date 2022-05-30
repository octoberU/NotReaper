using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper;
using NotReaper.Models;
using NotReaper.Timing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NotReaper.Modifiers
{
    public class ModifierTimeline : MonoBehaviour
    {

        [SerializeField] List<GameObject> objectsToHide = new();
        [SerializeField] List<GameObject> modifierTimeline;
        
        [NRInject] private Timeline timeline;
        
        public float width = 1f;
        public float maxHeight = 2f;
        public float zIndex = 3f;

        private MeshFilter[] meshFilters;

        /// <summary>
        /// Show or hide the modifier timeline.
        /// </summary>
        /// <param name="show">True to show the modifier timeline, false to show default timeline.</param>
        public void ShowModifierTimeline(bool show)
        {
            foreach (var obj in objectsToHide)
            {
                obj.SetActive(!show);
            }
            foreach(var obj in modifierTimeline)
                obj.SetActive(show);
        }

        [Button]
        private void Redraw()
        {
            var filters = GetComponentsInChildren<MeshFilter>();
            var mesh = filters[0].mesh;
            DebugDrawTimeline(mesh, width, maxHeight, zIndex);
            
            for (int i = 1; i < filters.Length; i++)
            {
                filters[i].mesh = mesh;
            }
        }

        private void DebugDrawTimeline(Mesh mesh, float width, float maxHeight, float zIndex)
        {
            QNT_Timestamp endOfAudio = new QNT_Timestamp(1920 * 60);

            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();

            TempoChange currentTempo = new TempoChange();
            currentTempo.timeSignature = new TimeSignature(4, 4);
            currentTempo.time = new QNT_Timestamp(0);
            currentTempo.secondsFromStart = 0f;
            currentTempo.microsecondsPerQuarterNote = (UInt64)Math.Round((double)Constants.OneMinuteInMicroseconds / 120);
            uint barLengthIncr = 0;
            for (float t = 0; t < endOfAudio.tick;)
            {
                //ulong snap = (ulong)(beatSnap / 4);
                float snap = EditorBeatSnap.BeatSnap / 4f;
                float increment = 0f;
                if (snap != 0) increment = Constants.PulsesPerWholeNote / currentTempo.timeSignature.Denominator / snap;
                else increment = Constants.PulsesPerWholeNote / currentTempo.timeSignature.Denominator;

                int indexStart = vertices.Count;

                //const float width = 0.020f;
                //const float maxHeight = 0.4f;
                //const float zIndex = 3;
                float start = t / (float)Constants.PulsesPerQuarterNote;
                start -= width / 2;

                float height = 0.0f;
                if (barLengthIncr == 0)
                {
                    height = maxHeight;
                }
                else
                {
                    height = maxHeight / 4;
                }

                //For 4/4 time, set the halfway heights
                if (currentTempo.timeSignature.Numerator == 4 && currentTempo.timeSignature.Denominator == 4)
                {
                    if (barLengthIncr == 2)
                    {
                        height = maxHeight / 2;
                    }
                }

                vertices.Add(new Vector3(start, -0.5f, zIndex));
                vertices.Add(new Vector3(start + width, -0.5f, zIndex));
                vertices.Add(new Vector3(start + width, -0.5f + height, zIndex));
                vertices.Add(new Vector3(start, -0.5f + height, zIndex));

                indices.Add(indexStart + 0);
                indices.Add(indexStart + 1);
                indices.Add(indexStart + 2);

                indices.Add(indexStart + 2);
                indices.Add(indexStart + 3);
                indices.Add(indexStart + 0);

                barLengthIncr++;
                barLengthIncr = barLengthIncr % currentTempo.timeSignature.Numerator;

                bool newTempo = false;
                foreach (TempoChange tempoChange in EditorTempo.TempoChanges)
                {
                    if (t < tempoChange.time.tick && t + increment >= tempoChange.time.tick)
                    {
                        barLengthIncr = 0;
                        t = tempoChange.time.tick;
                        currentTempo = tempoChange;
                        newTempo = true;
                        break;
                    }
                }

                if (!newTempo)
                {
                    t += increment;
                }
            }
            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
        }
        
        private void Awake()
        {
            meshFilters = GetComponentsInChildren<MeshFilter>();
            foreach(var obj in objectsToHide)
                obj.SetActive(false);
        }

        private void Start()
        {
            EditorBeatSnap.onBeatSnapChanged += (_, __) => RegenerateTimeline();
            EditorFile.onAudicaFileLoaded += (_) => RegenerateTimeline();
        }

        private void RegenerateTimeline()
        {
            var mesh = meshFilters[0].mesh;
            timeline.DrawTimingBars(mesh, width, maxHeight, zIndex);
            
            for (int i = 1; i < meshFilters.Length; i++)
            {
                meshFilters[i].mesh = mesh;
            }
        }
    }
}
