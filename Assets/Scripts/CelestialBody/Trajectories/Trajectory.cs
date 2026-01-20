using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(UI.FollowTransform))]
public class Trajectory : MonoBehaviour
{
    private TrajectoryMesh trajectoryMesh;
    private MeshFilter meshFilter;
    private UI.FollowTransform follow;

    private OrbitState _orbit;
    private UniVarPropagator _prop;
    public OrbitState Orbit
    {
        get => _orbit;
        set
        {
            if (_orbit == value) return;
            if (_orbit != null) _orbit.OnStateChanged -= GenerateTrajectory;
            _orbit = value;
            _prop = new UniVarPropagator(_orbit);
            _orbit.OnStateChanged += GenerateTrajectory;
            GenerateTrajectory();
        }
    }

    // furthest distance we will render parabolic and hyperbolic trajectories to
    [SerializeField]
    private double maxRenderDistance = 1e13;

    private void Awake()
    {
        trajectoryMesh = new TrajectoryMesh();
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = trajectoryMesh.mesh;
        follow = GetComponent<UI.FollowTransform>();
    }

    /// <summary>
    /// data about each vectex in the trajectory mesh
    /// </summary>
    private readonly struct TrajectoryPoint
    {
        public readonly Vector2 pos;
        public readonly double nu;
        public TrajectoryPoint(Vector2 pos, double nu)
        {
            this.pos = pos; this.nu = nu;
        }
    }

    public void GenerateTrajectory()
    {
        follow.follow = Orbit.body.transform;

        const int TRAJECTORY_SUBDIVS = 100;
        LinkedList<TrajectoryPoint> points = new();

        double nu1, nu2;
        if (Orbit.e < 1.0)
        {
            nu1 = 0;
            nu2 = nu1 + 2 * Math.PI * (Orbit.h > 0.0 ? 1 : -1);
            trajectoryMesh.loop = true;
        }
        else
        {
            // true anomaly to MAX_R
            double asymptote = Math.Acos((Math.Abs(Orbit.p) - maxRenderDistance) / (maxRenderDistance * Orbit.e));
            nu1 = asymptote;
            nu2 = -asymptote;
            trajectoryMesh.loop = false;
        }
        double dTheta = (nu2 - nu1) / (TRAJECTORY_SUBDIVS - 1);

        for (int i = 0; i < TRAJECTORY_SUBDIVS; i++)
        {
            double nu = nu1 + i * dTheta;
            double r = Orbit.GetDistanceFromNu(nu);
            if (r <= 0.0) continue;

            var theta = Orbit.omega + nu;
            points.AddLast(new TrajectoryPoint(
                (float)r * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta)),
                nu
            ));
        }

        // TODO: infer UV from nu
        trajectoryMesh.SetPointList(points);
        trajectoryMesh.UpdateMesh();
    }


    class TrajectoryMesh
    {
        public Vector3[] verts;
        /*
         * uvs: progress along the trajectory at this vertex
         * prev, next: positions of previous and next point
         * data: [ which side are we on? (-1 or 1), is this vertex a corner/end cap? (0 or 1) ]
         */
        public Vector2[] uvs, prev, next, data;
        public int[] tris;

        private Bounds bounds;

        public bool loop = false;

        public Mesh mesh;

        public TrajectoryMesh()
        {
            mesh = new Mesh();
        }

        public void SetPointList(LinkedList<TrajectoryPoint> points)
        {
            int len = points.Count;
            if (len < 2) return;

            // we add a duplicate version of the first point if we have a loop so
            // that UV doesn't interpolate between 0 and 1 for the segment that closes the loop
            int nVerts = len + (loop ? 1 : 0);
            verts = new Vector3[nVerts * 2];
            uvs = new Vector2[nVerts * 2];
            prev = new Vector2[nVerts * 2];
            next = new Vector2[nVerts * 2];
            data = new Vector2[nVerts * 2];
            tris = new int[6 * (nVerts - 1)];

            int i = 0;
            Vector2 maxCoords = Vector2.zero;
            foreach (var point in points)
            {
                var pos = point.pos;
                maxCoords.x = Mathf.Max(maxCoords.x, Mathf.Abs(pos.x));
                maxCoords.y = Mathf.Max(maxCoords.y, Mathf.Abs(pos.y));

                verts[i * 2] = pos;
                verts[i * 2 + 1] = pos;
                uvs[i * 2] = new Vector2((float)i / (nVerts - 1), 0);
                uvs[i * 2 + 1] = new Vector2((float)i / (nVerts - 1), 1);
                prev[MathUtils.Mod(i + 1, len) * 2] = pos;
                prev[MathUtils.Mod(i + 1, len) * 2 + 1] = pos;
                next[MathUtils.Mod(i - 1, len) * 2] = pos;
                next[MathUtils.Mod(i - 1, len) * 2 + 1] = pos;
                data[i * 2] = new Vector2(1, 0);
                data[i * 2 + 1] = new Vector2(-1, 0);

                i++;
            }
            // adjust render bounds for mesh so that it actually displays
            bounds = new Bounds(Vector2.zero, 2 * maxCoords);

            if (loop)
            {
                var pos0 = points.First.Value.pos;
                var pos1 = points.First.Next.Value.pos;
                var posL = points.Last.Value.pos;
                verts[len * 2] = pos0;
                verts[len * 2 + 1] = pos0;
                uvs[len * 2] = new Vector2(1, 0);
                uvs[len * 2 + 1] = new Vector2(1, 1);
                prev[len * 2] = posL;
                prev[len * 2 + 1] = posL;
                next[len * 2] = pos1;
                next[len * 2 + 1] = pos1;
                data[len * 2] = new Vector2(1, 0);
                data[len * 2 + 1] = new Vector2(-1, 0);
            }
            else
            {
                data[0].y = 1;
                data[1].y = 1;
                data[len - 2].y = 2;
                data[len - 1].y = 2;
            }

            for (int j = 0; j < nVerts - 1; j++)
            {
                tris[6 * j] = 2 * j;
                tris[6 * j + 1] = 2 * j + 1;
                tris[6 * j + 2] = 2 * j + 3;

                tris[6 * j + 3] = 2 * j;
                tris[6 * j + 4] = 2 * j + 2;
                tris[6 * j + 5] = 2 * j + 3;
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
