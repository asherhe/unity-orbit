using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(Trajectory), typeof(MeshFilter))]
    class TrajectoryMesh : MonoBehaviour
    {
        private MeshFilter _meshFilter;

        public Vector3[] verts;
        /*
         * uvs: progress along the trajectory at this vertex
         * prev, next: positions of previous and next point
         * data: [ which side are we on? (-1 or 1), is this vertex a corner/end cap? (0 or 1) ]
         */
        public Vector2[] uvs, prev, next, data;
        public int[] tris;

        private Bounds bounds;

        public bool isLooped = false;

        public Mesh mesh;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            mesh = new Mesh();
            _meshFilter.mesh = mesh;
        }

        public void SetPointList(LinkedList<Trajectory.Point> points)
        {
            int len = points.Count;
            if (len < 2) return;

            // we add a duplicate version of the first point if we have a loop so
            // that UV doesn't interpolate between 0 and 1 for the segment that closes the loop
            int nVerts = len + (isLooped ? 1 : 0);
            verts = new Vector3[nVerts * 2];
            uvs = new Vector2[nVerts * 2];
            prev = new Vector2[nVerts * 2];
            next = new Vector2[nVerts * 2];
            data = new Vector2[nVerts * 2];
            tris = new int[6 * (nVerts - 1)];

            var node = points.First;
            int i = 0;
            Vector2 maxCoords = Vector2.zero;
            while (node != null)
            {
                var point = node.Value;
                var pos = point.pos;

                if (double.IsFinite(pos.x)) maxCoords.x = Mathf.Max(maxCoords.x, Mathf.Abs(pos.x));
                if (double.IsFinite(pos.y)) maxCoords.y = Mathf.Max(maxCoords.y, Mathf.Abs(pos.y));

                var prevNode = node == points.First ? points.Last : node.Previous;
                var nextNode = node == points.Last ? points.First : node.Next;

                verts[i * 2] = pos;
                verts[i * 2 + 1] = pos;
                uvs[i * 2] = new Vector2((float)point.u, 0);
                uvs[i * 2 + 1] = new Vector2((float)point.u, 1);
                prev[i * 2] = prevNode.Value.pos;
                prev[i * 2 + 1] = prevNode.Value.pos;
                next[i * 2] = nextNode.Value.pos;
                next[i * 2 + 1] = nextNode.Value.pos;
                data[i * 2] = new Vector2(1, 0);
                data[i * 2 + 1] = new Vector2(-1, 0);

                node = node.Next;
                i++;
            }
            // adjust render bounds for mesh so that it actually displays
            bounds = new Bounds(Vector2.zero, 2 * maxCoords);

            if (isLooped)
            {
                verts[len * 2] = verts[0];
                verts[len * 2 + 1] = verts[1];
                uvs[len * 2] = uvs[0] + new Vector2(1, 0);
                uvs[len * 2 + 1] = uvs[1] + new Vector2(1, 0);
                prev[len * 2] = prev[0];
                prev[len * 2 + 1] = prev[1];
                next[len * 2] = next[0];
                next[len * 2 + 1] = next[1];
                data[len * 2] = data[0];
                data[len * 2 + 1] = data[1];
            }
            else
            {
                data[0].y = 1;
                data[1].y = 1;
                data[len * 2 - 2].y = 2;
                data[len * 2 - 1].y = 2;
            }

            for (int j = 0; j < nVerts - 1; j++)
            {
                tris[6 * j] = 2 * j;
                tris[6 * j + 1] = 2 * j + 1;
                tris[6 * j + 2] = 2 * j + 3;

                tris[6 * j + 3] = 2 * j;
                tris[6 * j + 4] = 2 * j + 3;
                tris[6 * j + 5] = 2 * j + 2;
            }
        }

        public void UpdateMesh()
        {
            mesh.Clear();
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.uv2 = prev;
            mesh.uv3 = next;
            mesh.uv4 = data;
            mesh.triangles = tris;
            mesh.bounds = bounds;
        }
    }
}