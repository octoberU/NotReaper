using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using NotReaper.Models;

namespace NotReaper.Statistics
{
    /// <summary>
    /// Represents a Statistics Element in the UI.
    /// </summary>
    public class StatisticsElement : MonoBehaviour
    {
        #region References and Fields
        [Header("References")]
        [SerializeField] private TextMeshProUGUI textTitle;
        [SerializeField] private TextMeshProUGUI textValueLeft;
        [SerializeField] private TextMeshProUGUI textValueRight;
        [SerializeField] private TextMeshProUGUI textValueTotal;
        [SerializeField] private GameObject leftRightRow;

        private bool isNoPercentage = false;
        private StatisticsManager.Statistics.Data data;
        #endregion

        #region Set Text
        /// <summary>
        /// Sets the text without using percentages.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="value">The value.</param>
        /// <returns>This StatisticsElement.</returns>
        public StatisticsElement SetTextWithoutPercentage(string title, float value)
        {
            textTitle.text = title;
            textValueTotal.text = value.ToString();
            EnableLeftRight(false);
            isNoPercentage = true;
            return this;
        }
        /// <summary>
        /// Sets the text.
        /// </summary>
        /// <param name="behavior">The behavior the data belongs to.</param>
        /// <param name="data">The behavior's data.</param>
        /// <returns>This StatisticsElement.</returns>
        public StatisticsElement SetText(TargetBehavior behavior, StatisticsManager.Statistics.Data data)
        {
            return SetText(behavior.ToString(), data);
        }
        /// <summary>
        /// Sets the text.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="value">The value.</param>
        /// <param name="data">The data.</param>
        /// <returns>This StatisticsElement.</returns>
        public StatisticsElement SetText(string title, StatisticsManager.Statistics.Data data, bool showPercentage = false)
        {
            this.data = data;
            textTitle.text = title;
            textValueLeft.text = $"{(showPercentage ? $"{data.PercentageLeft}%" : $"{data.Left}")}";
            textValueRight.text = $"{(showPercentage ? $"{data.PercentageRight}%" : $"{data.Right}")}";
            textValueTotal.text = $"{(showPercentage ? $"{data.PercentageTotal}%" : $"{data.Total}")}";
            return this;
        }
        #endregion

        #region Misc
        /// <summary>
        /// Enables the Left/Right component of the display.
        /// </summary>
        /// <param name="enable">True to enable.</param>
        public void EnableLeftRight(bool enable)
        {
            leftRightRow.SetActive(enable);
        }
        /// <summary>
        /// Resets this object back to default state.
        /// </summary>
        public void Reset()
        {
            EnableLeftRight(true);
            isNoPercentage = false;
            textTitle.text = "";
            textValueLeft.text = "";
            textValueRight.text = "";
            textValueTotal.text = "";
        }
        /// <summary>
        /// Display percentages instead of numbers.
        /// </summary>
        /// <param name="show">True for percentages.</param>
        public void ShowPercentage(bool show)
        {
            if (isNoPercentage) return;
            SetText(textTitle.text, data, show);
        }
        #endregion
    }
}

