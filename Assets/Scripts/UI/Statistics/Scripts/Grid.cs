using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NotReaper.Statistics
{
    /// <summary>
    /// Creates a grid and stores values.
    /// </summary>
    public class Grid
    {
        #region Fields
        public UnityEvent OnGridValueChanged = new UnityEvent();

        private int maxValue = 100;
        private int minValue = 0;
        private int width;
        private int height;
        private Vector2 cellSize;
        private Vector3 originPosition;
        private int[,] gridArray;
        #endregion

        /// <summary>
        /// Creates a new grid with the given parameters.
        /// </summary>
        /// <param name="width">The width of the grid.</param>
        /// <param name="height">The height of the grid.</param>
        /// <param name="cellSize">The size of each cell.</param>
        /// <param name="originPosition">The origin position of the grid.</param>
        /// <param name="maxValue">The max value a cell can hold.</param>
        public Grid(int width, int height, Vector2 cellSize, Vector3 originPosition, int maxValue)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
            this.originPosition = originPosition;
            this.maxValue = maxValue;

            gridArray = new int[width, height];
        }
        #region Getter
        /// <summary>
        /// Get the max value a cell can hold.
        /// </summary>
        /// <returns>The max value.</returns>
        public int GetMaxValue()
        {
            return maxValue;
        }
        /// <summary>
        /// Get the width of the grid.
        /// </summary>
        /// <returns>The width of the grid.</returns>
        public int GetWidth()
        {
            return width;
        }
        /// <summary>
        /// Get the height of the grid.
        /// </summary>
        /// <returns>The height of the grid.</returns>
        public int GetHeight()
        {
            return height;
        }
        /// <summary>
        /// Get the size of cells.
        /// </summary>
        /// <returns>The size of cells.</returns>
        public Vector2 GetCellSize()
        {
            return cellSize;
        }
        /// <summary>
        /// Get the world position of a cell from the given coordinates.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <returns>The world position of the cell.</returns>
        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x, y) * cellSize + (Vector2)originPosition;
        }
        /// <summary>
        /// Get coordiantes from world position.
        /// </summary>
        /// <param name="worldPosition">The world position.</param>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        private void GetXY(Vector3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize.x);
            y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize.y);
        }
        /// <summary>
        /// Get the value of the cell with the given coordinates.
        /// </summary>
        /// <param name="x">X coodinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <returns>The value of the desired cell.</returns>
        public int GetValue(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                return gridArray[x, y];
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// Get the value of the cell closest to the world position.
        /// </summary>
        /// <param name="worldPosition">The world position.</param>
        /// <returns>The value of the cell.</returns>
        public int GetValue(Vector3 worldPosition)
        {
            int x, y;
            GetXY(worldPosition, out x, out y);
            return GetValue(x, y);
        }
        #endregion

        #region Setter
        /// <summary>
        /// Set the value of the cell at the given coordinates.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="value">The value to assign to the cell.</param>
        public void SetValue(int x, int y, int value)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                gridArray[x, y] = Mathf.Clamp(value, minValue, maxValue);
                OnGridValueChanged.Invoke();
            }
        }
        /// <summary>
        /// Set the value of the cell closet to the given world position.
        /// </summary>
        /// <param name="worldPosition">The world position.</param>
        /// <param name="value">The value to assign to the cell.</param>
        public void SetValue(Vector3 worldPosition, int value)
        {
            int x, y;
            GetXY(worldPosition, out x, out y);
            SetValue(x, y, value);
        }
        /// <summary>
        /// Adds a value to the cell at the given coordinates.
        /// </summary>
        /// <param name="x">X coodinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="value">The value to add to the cell.</param>
        public void AddValue(int x, int y, int value)
        {
            SetValue(x, y, GetValue(x, y) + value);
        }
        /// <summary>
        /// Adds a value to the cell at the given world position.
        /// </summary>
        /// <param name="worldPosition">The world position.</param>
        /// <param name="value">The value to addd to the cell.</param>
        /// <param name="fullValueRange">How many cells around the chosen cell receive the full value.</param>
        /// <param name="totalRange">The range this value spreads around the surrounding cells.</param>
        public void AddValue(Vector3 worldPosition, int value, int fullValueRange, int totalRange)
        {
            int lowerValueAmount = Mathf.RoundToInt((float)value / (totalRange - fullValueRange));

            GetXY(worldPosition, out int originX, out int originY);
            for (int x = 0; x < totalRange; x++)
            {
                for (int y = 0; y < totalRange - x; y++)
                {
                    int radius = x + y;
                    int addValueAmount = value;
                    if (radius >= fullValueRange)
                    {
                        addValueAmount -= lowerValueAmount * (radius - fullValueRange);
                    }

                    AddValue(originX + x, originY + y, addValueAmount);

                    if (x != 0)
                    {
                        AddValue(originX - x, originY + y, addValueAmount);
                    }
                    if (y != 0)
                    {
                        AddValue(originX + x, originY - y, addValueAmount);
                        if (x != 0)
                        {
                            AddValue(originX - x, originY - y, addValueAmount);
                        }
                    }
                }
            }
        }
        #endregion
    }
}

