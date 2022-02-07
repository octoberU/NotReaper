using NotReaper.Targets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Statistics
{
    public class HeatmapVisualizer : MonoBehaviour
    {
        #region References and Fields
        [Header("References")]
        [SerializeField] private Transform topLeft;
        [SerializeField] private Transform topRight;
        [SerializeField] private Transform bottomLeft;
        [SerializeField] private Transform bottomRight;

        private Grid grid;
        private Mesh mesh;
        private bool updateMesh;
        #endregion

        private void Awake()
        {
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }
        /// <summary>
        /// Generates the heatmap for this map.
        /// </summary>
        /// <param name="targets">The targets for which to create the heatmap.</param>
        public void GenerateHeatmap(List<Target> targets)
        {
            int matrix = 100;
            float length = Vector3.Distance(topLeft.position, topRight.position);
            float height = Vector3.Distance(topLeft.position, bottomLeft.position);
            Vector2 cellSize = new Vector2(length, height) / matrix;
            grid = new Grid(matrix, matrix, cellSize, bottomLeft.position, targets.Count);
            grid.OnGridValueChanged.AddListener(OnGridValueChanged);
            StartCoroutine(AddNotes(targets));
        }
        /// <summary>
        /// Adds the targets to the heatmap.
        /// </summary>
        /// <param name="targets">The list of targets to add to the heatmap.</param>
        /// <remarks>Using a Coroutine to see the heatmap get drawn in realtime.</remarks>
        private IEnumerator AddNotes(List<Target> targets)
        {
            foreach (Target target in targets)
            {
                int value = Mathf.FloorToInt(grid.GetMaxValue() / 20f);
                grid.AddValue(target.gridTargetIcon.data.position, value, 5, 7);
                yield return null;
            }
        }
        /// <summary>
        /// Callback function when a value on the grid changes.
        /// </summary>
        private void OnGridValueChanged()
        {
            updateMesh = true;
        }
        /// <summary>
        /// Updates the heatmap visuals.
        /// </summary>
        private void UpdateHeatMapVisual()
        {
            MeshUtils.CreateEmptyMeshArrays(grid.GetWidth() * grid.GetHeight(), out Vector3[] vertices, out Vector2[] uv, out int[] triangles);

            for (int x = 0; x < grid.GetWidth(); x++)
            {
                for (int y = 0; y < grid.GetHeight(); y++)
                {
                    int index = x * grid.GetHeight() + y;
                    Vector3 quadSize = grid.GetCellSize();

                    int gridValue = grid.GetValue(x, y);
                    float gridValueNormalized = (float)gridValue / grid.GetMaxValue();
                    //Due to light texture offset (or some other weird stuff going on), the x-offset on the texture is set to 0.0078f, which is the gray pixel.
                    //Setting the uv to 1 ends back up at the gray pixel, so we simply subtract a little something to stop at the intended color.
                    if (gridValueNormalized >= 1f - .02f) gridValueNormalized -= .02f;
                    Vector2 gridValueUV = new Vector2(gridValueNormalized, 0f);
                    MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, grid.GetWorldPosition(x, y) + quadSize * .5f, 0f, quadSize, gridValueUV, gridValueUV);
                }
            }
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
        }
        private void LateUpdate()
        {
            if (updateMesh)
            {
                updateMesh = false;
                UpdateHeatMapVisual();
            }
        }

    }
}

