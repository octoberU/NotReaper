using AudicaTools;
using DifficultyCalculation;
using NotReaper.Managers;
using NotReaper.Models;
using NotReaper.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Forms;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;
namespace NotReaper.Statistics
{
    public class StatisticsUI : NRMenu
    {

        public static StatisticsUI Instance { get; private set; } = null;

        #region References and Fields
        [Header("References")]
        [SerializeField] private StatisticsElement prefabLeft;
        [SerializeField] private Transform parentLeft;
        [SerializeField] private Transform entryHolder;
        [SerializeField] private StatisticsElement firstHighlightSlot;
        [SerializeField] private StatisticsElement secondHighlightSlot;
        [Space, Header("Text")]
        [SerializeField] private TextMeshProUGUI songLabel;
        [SerializeField] private TextMeshProUGUI artistLabel;
        [SerializeField] private TextMeshProUGUI mapperLabel;
        [SerializeField] private TextMeshProUGUI difficultyLabel;
        [Space, Header("Toggle")]
        [SerializeField] private Toggle percentageToggle;

        public bool IsOpen => gameObject.activeInHierarchy;
        private CanvasGroup canvas;

        List<StatisticsElement> leftPool = new List<StatisticsElement>();
        List<StatisticsElement> activeLeftElements = new List<StatisticsElement>();
        #endregion

        #region Awake and Start
        protected override void Awake()
        {
            if (Instance != null)
            {
                Debug.Log("StatisticsUI already exists.");
                return;
            }
            Instance = this;
            canvas = GetComponent<CanvasGroup>();
            canvas.alpha = 0f;
            base.Awake();
        }

        private void Start()
        {
            gameObject.SetActive(false);
        }
        #endregion

        #region UI Events
        public override void Show()
        {
            Open();
        }
        public override void Hide()
        {
            Close();
        }
        public override void ShowHelp()
        {
            NRHelp.Instance.ShowStatistics();
        }
        /// <summary>
        /// Opens this window.
        /// </summary>
        public void Open()
        {
            OnActivated();
            canvas.DOFade(1f, .3f);
            songLabel.text = EditorFile.SongDesc.title.ToLowerInvariant();
            artistLabel.text = EditorFile.SongDesc.artist.ToLowerInvariant();
            mapperLabel.text = $"by {EditorFile.SongDesc.author}".ToLowerInvariant();
            float rating = DifficultyCalculator.GetRating(new Audica(EditorFile.AudicaFile.filepath), DifficultyManager.I.loadedIndex);
            rating = (float)Math.Round(rating, 2);
            difficultyLabel.text = $"difficulty: {rating}";
            StatisticsManager.Instance.GatherStatistics();
        }
        /// <summary>
        /// Closes this window.
        /// </summary>
        public void Close()
        {
            canvas.DOFade(0f, .3f).OnComplete(() =>
            {
                ClearEntries();
                OnDeactivated();
            });
        }

        /// <summary>
        /// Shows or hides percentages.
        /// </summary>
        public void OnPercentageToggled()
        {
            foreach (var entry in activeLeftElements) entry.ShowPercentage(percentageToggle.isOn);
            firstHighlightSlot.ShowPercentage(percentageToggle.isOn);
            secondHighlightSlot.ShowPercentage(percentageToggle.isOn);
        }
        #endregion

        #region Create Entries
        /// <summary>
        /// Create Data Entry.
        /// </summary>
        /// <param name="behavior">The data's behavior.</param>
        /// <param name="data">The data.</param>
        /// <param name="location">The location where the data should be displayed.</param>
        /// <returns>The manipulated StatisticsElement.</returns>
        internal StatisticsElement CreateDataEntry(TargetBehavior behavior, StatisticsManager.Statistics.Data data, StatLocation location = StatLocation.Left)
        {
            return SpawnElement(location).SetText(behavior, data);
        }
        /// <summary>
        /// Create Data Entry.
        /// </summary>
        /// <param name="title">The data's title.</param>
        /// <param name="data">The data.</param>
        /// <param name="location">The location where the data should be displayed.</param>
        /// <returns>The manipulated StatisticsElement.</returns>
        internal StatisticsElement CreateDataEntry(string title, StatisticsManager.Statistics.Data data, StatLocation location = StatLocation.Left)
        {
            return SpawnElement(location).SetText(title, data);
        }
        /// <summary>
        /// Create Data Entry without allowing percentages.
        /// </summary>
        /// <param name="title">The data's title.</param>
        /// <param name="value">The value to display.</param>
        /// <param name="location">The location where the data should be displayed.</param>
        /// <returns>The manipulated StatisticsElement.</returns>
        internal StatisticsElement CreateDataEntryWithoutPercentage(string title, float value, StatLocation location = StatLocation.Left)
        {
            return SpawnElement(location).SetTextWithoutPercentage(title, value);
        }
        #endregion

        #region Object Pooling and Spawn Handling
        /// <summary>
        /// Clears all active entries and returns them to the pool.
        /// </summary>
        internal void ClearEntries()
        {
            for(int i = activeLeftElements.Count - 1; i >= 0; i--)
            {
                var element = activeLeftElements[i];
                activeLeftElements.RemoveAt(i);
                leftPool.Add(element);
                element.Reset();
                element.transform.SetParent(entryHolder);
                element.gameObject.SetActive(false);
            }
            firstHighlightSlot.Reset();
            secondHighlightSlot.Reset();
            firstHighlightSlot.gameObject.SetActive(false);
            secondHighlightSlot.gameObject.SetActive(false);
        }
        /// <summary>
        /// Spawns a new StatisticsElement at the given location.
        /// </summary>
        /// <param name="location">The location the element should spawn at.</param>
        /// <returns>The spawned StatisticsElement.</returns>
        private StatisticsElement SpawnElement(StatLocation location)
        {
            return location == StatLocation.Left ? SpawnLeftPanelElement() : GetHighlightSlot(location);
        }
        /// <summary>
        /// Gets a highlight slot.
        /// </summary>
        /// <param name="location">The desired highlight slot.</param>
        /// <returns>The selected StatisticsElement highlight slot.</returns>
        private StatisticsElement GetHighlightSlot(StatLocation location)
        {
            if (firstHighlightSlot) firstHighlightSlot.gameObject.SetActive(true);
            else secondHighlightSlot.gameObject.SetActive(true);
            return location == StatLocation.FirstHighlight ? firstHighlightSlot : secondHighlightSlot;
        }
        /// <summary>
        /// Spawns a StatisticsElement in the left panel.
        /// </summary>
        /// <returns>The spawned StatisticsElement.</returns>
        private StatisticsElement SpawnLeftPanelElement()
        {
            StatisticsElement element;

            if(leftPool.Count > 0)
            {
                element = leftPool[0];
                leftPool.RemoveAt(0);
            }
            else
            {
                element = Instantiate(prefabLeft, parentLeft);
            }
            element.gameObject.SetActive(true);
            element.transform.SetParent(parentLeft);
            activeLeftElements.Add(element);
            return element;
        }
        #endregion
        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Close();
        }
        /// <summary>
        /// Describes the location of StatisticsElement.
        /// </summary>
        internal enum StatLocation
        {
            Left,
            FirstHighlight,
            SecondHighlight
        }
    }
}

