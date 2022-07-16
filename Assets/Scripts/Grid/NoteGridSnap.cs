using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Targets;
using UnityEngine;
using UnityEngine.EventSystems;
using NotReaper.UserInput;
using NotReaper.Models;
using NotReaper.UI;

namespace NotReaper.Grid {

    public class NoteGridSnap : MonoBehaviour
    {

        [SerializeField] private RectTransform grid;

        private static GridBounds gridBounds;
        private class GridBounds
        {
            public readonly Vector2 bottomLeft;
            public readonly Vector2 topLeft;
            public readonly Vector2 topRight;
            public readonly Vector2 bottomRight;
            public readonly Vector2 size;
            public readonly Vector2 startPosition;

            private Vector2 cellSize;
            private Vector2 offset;
            private Vector2 halfGridSize;
            
            public GridBounds(Vector3[] corners, Vector2 gridSize)
            {
                bottomLeft = corners[0];
                topLeft = corners[1];
                topRight = corners[2];
                bottomRight = corners[3];
                size = new(Vector2.Distance(topLeft, topRight), Vector2.Distance(topLeft, bottomLeft));
                startPosition = new(topLeft.x, bottomLeft.y);

                CalculateGrid(gridSize);
            }

            
            private void CalculateGrid(Vector2 gridSize)
            {
                cellSize = size / gridSize;
                offset = startPosition - cellSize * .5f;
                halfGridSize = size * .5f;
            }

            public Vector2 GetSnappedPosition(Vector2 position)
            {
                Vector2 offsetPosition = position - offset;
                Vector2 snappedPosition = new Vector2(Mathf.FloorToInt(offsetPosition.x / cellSize.x), Mathf.FloorToInt(offsetPosition.y / cellSize.y)) * cellSize;
                snappedPosition -= halfGridSize;
                return snappedPosition;
            }

            public void UpdateGridSize(Vector2 gridSize) => CalculateGrid(gridSize);
        }
        
        private void Start()
        {
            NRSettings.OnLoad(() =>
            {
                Vector3[] corners = new Vector3[4];
                grid.GetWorldCorners(corners);
                gridBounds = new(corners, NRSettings.config.gridSize);

            });
            
            GridSizeManager.onSizeChanged += (size) => gridBounds.UpdateGridSize(size);
        }

        private static Vector2 GetNearestPointOnGrid(Vector2 pos, SnappingMode mode)
        {
            return gridBounds.GetSnappedPosition(pos);

            /*
            //pos -= gridOffset; //Enable if grid is actually offset.
            var scaleX = 1f - (float)DefaultGridX / currentX;
            pos.y += .45f;
            int x = Mathf.FloorToInt(pos.x / NotePosCalc.xSize);
            int y = Mathf.FloorToInt(pos.y / NotePosCalc.ySize);
            Debug.Log("X: " + x);
            Vector2 result = new Vector2((float) x * NotePosCalc.xSnapSize, (float)y * NotePosCalc.ySize);
            result.x += NotePosCalc.xSnapSize / 2;

            //result += gridOffset; //Enable if grid is actually offset.
            return result;
            */
        }

        public static Vector3 SnapToGrid(Vector3 pos, SnappingMode mode) {
            switch (mode) {
                case SnappingMode.Grid:
                case SnappingMode.DetailGrid:
                    return GetNearestPointOnGrid(pos, mode);
                case SnappingMode.Melee:

	                float x;
	                float y;

	                if (pos.x < 0) x = NotePosCalc.xSize * -2;
	                else x = NotePosCalc.xSize * 2;

	                if (pos.y < 0) y = NotePosCalc.ySize * -1;
	                else y = NotePosCalc.ySize * 1;

                    return new Vector3(x, y, pos.z + 5);
            }
            return new Vector3(pos.x, pos.y, pos.z + 5);
            //return new Vector3(pos.x, pos.y, 0f);
            //return new Vector2(pos.x, pos.y);
        }

        
    }


}