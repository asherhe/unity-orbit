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

    private void Awake()
    {
        trajectoryMesh = new TrajectoryMesh();
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = trajectoryMesh.mesh;
        follow = GetComponent<UI.FollowTransform>();
    }

    public void GenerateTrajectory()
    {
        follow.follow = Orbit.body.transform;

        const int TRAJECTORY_SUBDIVS = 100;
        List<Vector2> points = new(TRAJECTORY_SUBDIVS); // subdivision points used in trajectory mesh
        List<double> nus = new(TRAJECTORY_SUBDIVS); // true anomaly at each subdivision point

        double nu1, nu2;
        if (Orbit.e < 1.0)
        {
            nu1 = 0;
            nu2 = nu1 + 2 * Math.PI * (Orbit.h > 0.0 ? 1 : -1);
            trajectoryMesh.loop = true;
        }
        else
        {
            // furthest distance we will render parabolic and hyperbolic trajectories to
            const double MAX_R = 1e13;
            // true anomaly to MAX_R
            double asymptote = Math.Acos((Math.Abs(Orbit.p) - MAX_R) / (MAX_R * Orbit.e));
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
            points.Add((float)r * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta)));
            nus.Add(nu);
        }

        // TODO: infer UV from nu
        trajectoryMesh.SetPointList(points);
        trajectoryMesh.UpdateMesh();
    }


    class TrajectoryMesh
    {
        public Vector3[] verts;
        public Vector2[] uvs, prev, next, data;
        public int[] tris;

        private Bounds bounds;

        public bool loop = false;

        public Mesh mesh;

        public TrajectoryMesh()
        {
            mesh = new Mesh();
        }

        public void SetPointList(List<Vector2> points)
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

            Vector2 maxCoords = Vector2.zero;
            for (int i = 0; i < len; i++)
            {
                maxCoords.x = Mathf.Max(maxCoords.x, Mathf.Abs(points[i].x));
                maxCoords.y = Mathf.Max(maxCoords.y, Mathf.Abs(points[i].y));

                verts[i * 2] = points[i];
                verts[i * 2 + 1] = points[i];
                uvs[i * 2] = new Vector2((float)i / (nVerts - 1), 0);
                uvs[i * 2 + 1] = new Vector2((float)i / (nVerts - 1), 1);
                prev[MathUtils.Mod(i + 1, len) * 2] = points[i];
                prev[MathUtils.Mod(i + 1, len) * 2 + 1] = points[i];
                next[MathUtils.Mod(i - 1, len) * 2] = points[i];
                next[MathUtils.Mod(i - 1, len) * 2 + 1] = points[i];
                data[i * 2] = new Vector2(1, 0);
                data[i * 2 + 1] = new Vector2(-1, 0);
            }
            bounds = new Bounds(Vector2.zero, 2 * maxCoords);

            if (loop)
            {
                verts[len * 2] = points[0];
                verts[len * 2 + 1] = points[0];
                uvs[len * 2] = new Vector2(1, 0);
                uvs[len * 2 + 1] = new Vector2(1, 1);
                prev[len * 2] = points[len - 1];
                prev[len * 2 + 1] = points[len - 1];
                next[len * 2] = points[1];
                next[len * 2 + 1] = points[1];
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

            for (int i = 0; i < nVerts - 1; i++)
            {
                tris[6 * i] = 2 * i;
                tris[6 * i + 1] = 2 * i + 1;
                tris[6 * i + 2] = 2 * i + 3;

                tris[6 * i + 3] = 2 * i;
                tris[6 * i + 4] = 2 * i + 2;
                tris[6 * i + 5] = 2 * i + 3;
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
