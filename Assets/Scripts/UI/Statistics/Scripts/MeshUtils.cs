using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Statistics
{
    public class MeshUtils
    {
        private static Quaternion[] cachedQuaternionEulerArr;
        /// <summary>
        /// Caches quaternions.
        /// </summary>
        private static void CacheQuaternionEuler()
        {
            if (cachedQuaternionEulerArr != null) return;
            cachedQuaternionEulerArr = new Quaternion[360];
            for (int i = 0; i < 360; i++)
            {
                cachedQuaternionEulerArr[i] = Quaternion.Euler(0, 0, i);
            }
        }
        /// <summary>
        /// Gets a quaternion from euler angles.
        /// </summary>
        /// <param name="rotFloat">The rotation as a float.</param>
        /// <returns>A quaternion rotation.</returns>
        private static Quaternion GetQuaternionEuler(float rotFloat)
        {
            int rot = Mathf.RoundToInt(rotFloat);
            rot = rot % 360;
            if (rot < 0) rot += 360;
            if (cachedQuaternionEulerArr == null) CacheQuaternionEuler();
            return cachedQuaternionEulerArr[rot];
        }

        /// <summary>
        /// Creates an empty mesh.
        /// </summary>
        /// <returns>An empty mesh.</returns>
        public static Mesh CreateEmptyMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[0];
            mesh.uv = new Vector2[0];
            mesh.triangles = new int[0];
            return mesh;
        }
        /// <summary>
        /// Creates empty arrays for a mesh.
        /// </summary>
        /// <param name="quadCount">The amount of quads needed.</param>
        /// <param name="vertices">Empty vertices array.</param>
        /// <param name="uvs">Empty UVs array.</param>
        /// <param name="triangles">Empty triangles array.</param>
        public static void CreateEmptyMeshArrays(int quadCount, out Vector3[] vertices, out Vector2[] uvs, out int[] triangles)
        {
            vertices = new Vector3[4 * quadCount];
            uvs = new Vector2[4 * quadCount];
            triangles = new int[6 * quadCount];
        }
        /// <summary>
        /// Adds to the arrays in the mesh.
        /// </summary>
        /// <param name="vertices">Vertices array.</param>
        /// <param name="uvs">UVs array.</param>
        /// <param name="triangles">Triangles array.</param>
        /// <param name="index">The current index.</param>
        /// <param name="pos">The position of the object.</param>
        /// <param name="rot">The rotation of the object.</param>
        /// <param name="baseSize">The base size of the object.</param>
        /// <param name="uv00">Texture starting point.</param>
        /// <param name="uv11">Texture end point.</param>
        public static void AddToMeshArrays(Vector3[] vertices, Vector2[] uvs, int[] triangles, int index, Vector3 pos, float rot, Vector3 baseSize, Vector2 uv00, Vector2 uv11)
        {
            //Relocate vertices
            int vIndex = index * 4;
            int vIndex0 = vIndex;
            int vIndex1 = vIndex + 1;
            int vIndex2 = vIndex + 2;
            int vIndex3 = vIndex + 3;

            baseSize *= .5f;

            bool skewed = baseSize.x != baseSize.y;
            if (skewed)
            {
                vertices[vIndex0] = pos + GetQuaternionEuler(rot) * new Vector3(-baseSize.x, baseSize.y);
                vertices[vIndex1] = pos + GetQuaternionEuler(rot) * new Vector3(-baseSize.x, -baseSize.y);
                vertices[vIndex2] = pos + GetQuaternionEuler(rot) * new Vector3(baseSize.x, -baseSize.y);
                vertices[vIndex3] = pos + GetQuaternionEuler(rot) * baseSize;
            }
            else
            {
                vertices[vIndex0] = pos + GetQuaternionEuler(rot - 270) * baseSize;
                vertices[vIndex1] = pos + GetQuaternionEuler(rot - 180) * baseSize;
                vertices[vIndex2] = pos + GetQuaternionEuler(rot - 90) * baseSize;
                vertices[vIndex3] = pos + GetQuaternionEuler(rot - 0) * baseSize;
            }
            float scale = 47.5f;
            vertices[vIndex0].x *= scale;
            vertices[vIndex0].y *= scale;
            vertices[vIndex1].x *= scale;
            vertices[vIndex1].y *= scale;
            vertices[vIndex2].x *= scale;
            vertices[vIndex2].y *= scale;
            vertices[vIndex3].x *= scale;
            vertices[vIndex3].y *= scale;

            //Relocate UVs
            uvs[vIndex0] = new Vector2(uv00.x, uv11.y);
            uvs[vIndex1] = new Vector2(uv00.x, uv00.y);
            uvs[vIndex2] = new Vector2(uv11.x, uv00.y);
            uvs[vIndex3] = new Vector2(uv11.x, uv11.y);

            //Create triangles
            int tIndex = index * 6;

            triangles[tIndex + 0] = vIndex0;
            triangles[tIndex + 1] = vIndex3;
            triangles[tIndex + 2] = vIndex1;

            triangles[tIndex + 3] = vIndex1;
            triangles[tIndex + 4] = vIndex3;
            triangles[tIndex + 5] = vIndex2;
        }
    }
}

