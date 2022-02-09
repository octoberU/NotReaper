using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.Statistics
{
    /// <summary>
    /// Handles statistics data.
    /// </summary>
    public class StatisticsManager : MonoBehaviour
    {
        public static StatisticsManager Instance { get; private set; } = null;
        public Statistics Stats { get; private set; }

        #region References and fields
        [Header("References")]
        [SerializeField] private HeatmapVisualizer heatmap;
        [SerializeField] private StatisticsUI window;
        #endregion

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.Log("StatisticManager already exists.");
                return;
            }
            Instance = this;
        }

        public void GatherStatistics()
        {
            var targets = Timeline.orderedNotes;
            Stats = new Statistics(targets);
            heatmap.GenerateHeatmap(targets);
            StatisticsUI.Instance.CreateDataEntry("Notes", new Statistics.Data(targets, Stats.Total, Stats.LeftTargets, Stats.RightTargets), StatisticsUI.StatLocation.FirstHighlight);
            StatisticsUI.Instance.CreateDataEntryWithoutPercentage("NPS", Stats.NPS, StatisticsUI.StatLocation.SecondHighlight);
            foreach(var entry in Stats.DataList)
            {
                if (entry.Key == TargetBehavior.Hold) StatisticsUI.Instance.CreateDataEntry("Sustain", entry.Value);
                else if (entry.Key == TargetBehavior.Mine) StatisticsUI.Instance.CreateDataEntry(entry.Key, entry.Value).EnableLeftRight(false);
                else StatisticsUI.Instance.CreateDataEntry(entry.Key, entry.Value);
            }            
        }

        #region Data Classes
        /// <summary>
        /// Holds all data.
        /// </summary>
        public class Statistics
        {
            #region Properties
            public int Total { get; }
            public int LeftTargets { get; }
            public int RightTargets { get; }
            public float NPS { get; }
            public Data Standard { get; }
            public Data Horizontal { get; }
            public Data Vertical { get; }
            public Data Sustain { get; }
            public Data ChainStart { get; }
            public Data ChainNode { get; }
            public Data Melee { get; }
            public Data Mine { get; }
            public Dictionary<TargetBehavior, Data> DataList { get; }
            #endregion

            #region Constructor
            /// <summary>
            /// Creates a new Statistics object and gets all needed data.
            /// </summary>
            /// <param name="targets">The list of targets of this map.</param>
            public Statistics(List<Target> targets)
            {
                //Get Data
                DataList = new Dictionary<TargetBehavior, Data>();
                DataList.Add(TargetBehavior.Standard, new Data(targets, TargetBehavior.Standard));
                DataList.Add(TargetBehavior.Horizontal, new Data(targets, TargetBehavior.Horizontal));
                DataList.Add(TargetBehavior.Vertical, new Data(targets, TargetBehavior.Vertical));
                DataList.Add(TargetBehavior.Hold, new Data(targets, TargetBehavior.Hold));
                DataList.Add(TargetBehavior.ChainStart, new Data(targets, TargetBehavior.ChainStart));
                DataList.Add(TargetBehavior.Chain, new Data(targets, TargetBehavior.Chain));
                DataList.Add(TargetBehavior.Melee, new Data(targets, TargetBehavior.Melee));
                DataList.Add(TargetBehavior.Mine, new Data(targets, TargetBehavior.Mine));
                //Get Total data
                foreach(var entry in DataList)
                {
                    LeftTargets += entry.Value.Left;
                    RightTargets += entry.Value.Right;
                    Total += entry.Value.Total;
                }
                if (targets.Count == 0) return;
                //Get Percentages
                var start = targets.First().data.time;
                var end = targets.Last().data.time;
                var bpm = Timeline.instance.GetTempoForTime(start);
                float beatsBetweenTargets = new QNT_Timestamp(end.tick - start.tick).ToBeatTime();
                float mapLength = (bpm.microsecondsPerQuarterNote / 1000f) * beatsBetweenTargets / 1000f;
                NPS = (float)Math.Round(targets.Count / mapLength, 1);
            }
            #endregion

            /// <summary>
            /// Holds precise data about a behavior.
            /// </summary>
            public class Data
            {
                #region Properties
                public int Total { get; }
                public int Left { get; }
                public int Right { get; }
                public int PercentageTotal { get; }
                public int PercentageLeft { get; }
                public int PercentageRight { get; }
                #endregion

                #region Constructors
                /// <summary>
                /// Gathers data for a behavior.
                /// </summary>
                /// <param name="targets">The targets of this map.</param>
                /// <param name="behavior">The behavior you desire data for.</param>
                public Data(List<Target> targets, TargetBehavior behavior)
                {
                    GetBehaviorCount(targets, behavior, out int left, out int right, out int total);
                    Left = left;
                    Right = right;
                    Total = total;
                    int tc = targets.Count;
                    PercentageTotal = GetPercentage(tc, Total);
                    PercentageLeft = GetPercentage(Total, Left);
                    PercentageRight = GetPercentage(Total, Right);
                }
                /// <summary>
                /// Gathers data for non-behavior.
                /// </summary>
                /// <param name="targets"></param>
                /// <param name="total"></param>
                /// <param name="left"></param>
                /// <param name="right"></param>
                public Data(List<Target> targets, int total, int left, int right)
                {
                    Total = total;
                    Left = left;
                    Right = right;

                    int tc = targets.Count;
                    PercentageTotal = GetPercentage(tc, Total);
                    PercentageLeft = GetPercentage(Total, Left);
                    PercentageRight = GetPercentage(Total, Right);
                }
                #endregion

                #region Helpers
                /// <summary>
                /// Gets percentage.
                /// </summary>
                /// <param name="totalTargets">The total amount of targets.</param>
                /// <param name="myTargets">The amount of targets to divide with.</param>
                /// <returns>Rounded percentage.</returns>
                private int GetPercentage(int totalTargets, int myTargets)
                {
                    if (totalTargets <= 0 || myTargets <= 0) return 0;
                    float percentage = (float)myTargets / totalTargets * 100f;
                    return Mathf.RoundToInt(percentage);
                }
                /// <summary>
                /// Gets the count of a certain behavior.
                /// </summary>
                /// <param name="targets">The targets to go through.</param>
                /// <param name="behavior">The behavior to look for.</param>
                /// <param name="left">Left hand count.</param>
                /// <param name="right">Right hand count.</param>
                /// <param name="total">Total count.</param>
                private void GetBehaviorCount(List<Target> targets, TargetBehavior behavior, out int left, out int right, out int total)
                {
                    var behaviors = targets.Where(t => t.data.behavior == behavior).ToList();                   
                    left = GetHandCount(behaviors, TargetHandType.Left);
                    right = GetHandCount(behaviors, TargetHandType.Right);
                    total = behaviors.Count;
                    
                }
                /// <summary>
                /// Gets the count of a certain hand.
                /// </summary>
                /// <param name="targets">The targets to go through.</param>
                /// <param name="hand">The hand to count.</param>
                /// <returns>Count of hand.</returns>
                private int GetHandCount(List<Target> targets, TargetHandType hand)
                {
                    return targets.Where(t => t.data.handType == hand).Count();
                }
                #endregion
            }
        }
        #endregion
    }
}

