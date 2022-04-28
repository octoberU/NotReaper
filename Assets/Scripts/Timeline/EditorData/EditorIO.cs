using NotReaper.Grid;
using NotReaper.IO;
using NotReaper.Managers;
using NotReaper.MapIO;
using NotReaper.Models;
using NotReaper.Modifier;
using NotReaper.Notifications;
using NotReaper.Repeaters;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools.ChainBuilder;
using NotReaper.UI;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NotReaper
{
    public class EditorIO : MonoBehaviour
    {
        private static AudicaLoader loader;
        private static MapIO.AudicaExporter exporter;

        private void Start()
        {
            loader = NRDependencyInjector.Get<AudicaLoader>();
            exporter = NRDependencyInjector.Get<MapIO.AudicaExporter>();
        }
        /// <summary>
        /// Loads an existing .audica file.
        /// </summary>
        /// <param name="filepath">Filepath of .audica file.</param>
        public static void LoadAudicaFile(string filepath)
            => loader.LoadMap(filepath, null);
        /// <summary>
        /// Loads an existing .audica file.
        /// </summary>
        /// <param name="filepath">Filepath of .audica file.</param>
        /// <param name="onFinished">Action to perform when loading finished.</param>
        public static void LoadAudicaFile(string filepath, Action<bool> onFinished)
            => loader.LoadMap(filepath, onFinished);
        /// <summary>
        /// Loads a newly created audica file.
        /// </summary>
        /// <param name="filepath">Filepath of newly created .audica file.</param>
        /// <param name="onFinished">Action to perform when loading finished.</param>
        /// <param name="bpm">The BPM the map should start with (will use 150 or what is provided with midi if nothing is set).</param>
        /// <param name="numerator">The initial numerator (will use 4 or what is provided with midi if nothing is set).</param>
        /// <param name="denominator">The initial denominator (will use 4 or what is provided with midi if nothing is set).</param>
        public static void LoadNewAudicaFile(string filepath, Action<bool> onFinished, float bpm = -1f, int numerator = 4, int denominator = 4)
            => loader.LoadMap(filepath, onFinished, bpm, numerator, denominator);
        /// <summary>
        /// Opens a folder dialog to select an audica file and loads the selected file.
        /// </summary>
        /// <param name="onFinished">Action to perform when loading finished.</param>
        public static void SelectAudicaFile(Action<bool> onFinished)
            => loader.SelectMap(onFinished);
    }
}
