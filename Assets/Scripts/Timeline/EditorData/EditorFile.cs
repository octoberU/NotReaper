using NotReaper;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using NotReaper.MapIO;
namespace NotReaper
{
    /// <summary>
    /// Responsible for IO operations and managing the loaded AudicaFile.
    /// </summary>
    public class EditorFile : Singleton<EditorFile>
    {
        /// <summary>
        /// The loaded Audica file.
        /// </summary>
        public static AudicaFile AudicaFile { get; private set; }

        /// <summary>
        /// The desc property of <see cref="AudicaFile"/>
        /// </summary>
        public static SongDesc SongDesc
        {
            get
            {
                if (AudicaFile != null)
                    return AudicaFile.desc;
                else
                    return null;
            }
        }

        /// <summary>
        /// Indicates whether <see cref="AudicaFile"/> is fully loaded and ready.
        /// </summary>
        public static bool IsAudicaFileLoaded { get; private set; }

        /// <summary>
        /// Indicates whether the audio in <see cref="AudicaFile"/> is fully loaded and ready.
        /// </summary>
        public static bool IsAudioLoaded { get; private set; }

        /// <summary>
        /// Raised when <see cref="AudicaFile"/> is set.
        /// </summary>
        public static OnAudicaFileLoaded onAudicaFileLoaded;
        public delegate void OnAudicaFileLoaded(AudicaFile file);

        private static AudicaLoader loader;
        private static AudicaExporter exporter;

        private void Start()
        {
            loader = NRDependencyInjector.Get<AudicaLoader>();
            exporter = NRDependencyInjector.Get<AudicaExporter>();
        }


        /// <summary>
        /// Sets the currently loaded audica file.
        /// </summary>
        /// <param name="file">The loaded file.</param>
        public static void SetAudicaFile(AudicaFile file)
        {
            AudicaFile = file;
            IsAudicaFileLoaded = true;
        }
        /// <summary>
        /// Sets <see cref="AudicaFile"/> to null.
        /// </summary>
        public static void UnloadAudicaFile()
        {
            AudicaFile = null;
            IsAudicaFileLoaded =false;
        }
        /// <summary>
        /// Sets if Audica File has been fully loaded. This is *not* the case if AudicaFile != null.
        /// </summary>
        /// <param name="isLoaded">True if loaded.</param>
        public static void SetIsAudicaLoaded(bool isLoaded)
        {
            IsAudicaFileLoaded = isLoaded;
            if (isLoaded)
            {
                onAudicaFileLoaded?.Invoke(AudicaFile);
            }
        }
        /// <summary>
        /// Sets if audio has been fully loaded.
        /// </summary>
        /// <param name="isLoaded">True if loaded.</param>
        public static void SetIsAudioLoaded(bool isLoaded)
        {
            IsAudioLoaded = isLoaded;
        }

        /*public static void Save()
            => exporter.Save();

        public static void LoadFile(string path)
            => loader.LoadFile(path);*/
    }
}
