using MathNet.Numerics.Distributions;
using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
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

    /// <summary>
    /// furthest distance to which we will render parabolic and hyperbolic trajectories (m)
    /// </summary>
    [SerializeField]
    private double maxRenderDistance = 1e13;
    /// <summary>
    /// maximum error allowable in generated trajectory mesh from original conic, as a fraction of semi-latus rectum
    /// NOTE: ideally we want this to maybe be a fraction of the camera size but that would require regenerating the mesh more times than necessary
    /// </summary>
    [SerializeField]
    private double quality = 2e-4;

    /// <summary>
    /// boundaries that restrict the range of true anomalies we draw. true anomaly is always in [ -PI, +PI ]
    /// </summary>
    public double nuMin = double.NegativeInfinity, nuMax = double.PositiveInfinity;

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
        public readonly double r;
        public readonly double nu;
        public readonly double u;
        /// <summary>
        /// construct a trajectory point
        /// </summary>
        /// <param name="r">distance from center</param>
        /// <param name="nu">true anomaly</param>
        /// <param name="o">orbit state</param>
        public TrajectoryPoint(double r, double nu, OrbitState o)
        {
            this.r = r; this.nu = nu;
            var theta = (float)(nu + o.omega);
            pos = (float)r * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));

            // TODO: migrate out of constructor into dedicated block in GenerateTrajectory()
            var prop = new UniVarPropagator(o);
            var coeff = prop.AnomCoeff;
            var anomaly = o.CalcAnomaly(nu);
            var chi = coeff * MathUtils.Mod(anomaly - o.Anomaly0, 2 * Math.PI);
            var dt = prop.UniversalKepler(chi) / Math.Sqrt(o.GM);

            // characteristic time 
            double tscale = 0;
            if (o.e < 1.0) tscale = o.period;
            else if (o.e == 1.0) tscale = 4 * Math.Sqrt(o.periapsis * o.periapsis * o.periapsis / o.GM) / 3;
            else tscale = Math.Sqrt(o.p * o.p * o.p / o.GM) / (o.e * o.e - 1);

            u = dt / tscale;

            // flip direction to match orbit direction
            if (o.h < 0) u = 1 - u;
        }
    }

    public void GenerateTrajectory()
    {
        follow.follow = Orbit.body.transform;

        double nu1, nu2;
        if (Orbit.e < 1.0)
        {
            nu1 = -Math.PI;
            nu2 = Math.PI;
            trajectoryMesh.isLooped = true;
        }
        else
        {
            // true anomaly to maxRenderDistance
            double asymptote = Math.Acos((Math.Abs(Orbit.p) - maxRenderDistance) / (maxRenderDistance * Orbit.e));
            nu1 = -asymptote;
            nu2 = asymptote;
            trajectoryMesh.isLooped = false;
        }

        // cancel looping if trajectory gets clipped
        if (nu1 < nuMin || nu2 > nuMax) trajectoryMesh.isLooped = false;

        nu1 = Math.Max(nu1, nuMin);
        nu2 = Math.Min(nu2, nuMax);

        // if we are drawing a looped mesh: shift the seam to periapsis so that vertex UV interpolation is sharp
        if (trajectoryMesh.isLooped) { nu1 = 0; nu2 = 2 * Math.PI; }

        LinkedList<TrajectoryPoint> points = new();

        // error tolerance
        double tol = Orbit.p * quality;

        void SubdivideMesh(LinkedListNode<TrajectoryPoint> start, LinkedListNode<TrajectoryPoint> end, double nuStart, double nuEnd, int depth)
        {
            if (depth == 0) return;

            var nuMid = 0.5 * (nuStart + nuEnd);
            // real distance at this point
            var rMid = Orbit.GetDistanceFromNu(nuMid);
            // chord distance based on start and end points (i.e. straight line connecting start to end)
            double r1 = start.Value.r, r2 = end.Value.r;
            var chordMid = 2 * r1 * r2 * Math.Cos(0.5 * (nuEnd - nuStart)) / (r1 + r2);

            // deviates too far
            if (Math.Abs(rMid - chordMid) > tol)
            {
                var mid = points.AddAfter(start, new TrajectoryPoint(rMid, nuMid, Orbit));
                SubdivideMesh(start, mid, nuStart, nuMid, depth - 1);
                SubdivideMesh(mid, end, nuMid, nuEnd, depth - 1);
            }
        }

        points.AddLast(new TrajectoryPoint(Orbit.GetDistanceFromNu(nu1), nu1, Orbit));
        points.AddLast(new TrajectoryPoint(Orbit.GetDistanceFromNu(nu2), nu2, Orbit));
        SubdivideMesh(points.First, points.Last, nu1, nu2, 24);

        if (trajectoryMesh.isLooped)
        {
            // remove duplicate point that closes the loop
            points.RemoveLast();

            // reverse the list if necessary so that the first vertex is always u=0
            if (Orbit.h < 0.0)
            {
                var curr = points.First;
                while (curr.Next != null)
                {
                    var next = curr.Next;
                    points.Remove(next);
                    points.AddFirst(next.Value);
                }
            }
        }

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

        public bool isLooped = false;

        public Mesh mesh = new();

        public void SetPointList(LinkedList<TrajectoryPoint> points)
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

                maxCoords.x = Mathf.Max(maxCoords.x, Mathf.Abs(pos.x));
                maxCoords.y = Mathf.Max(maxCoords.y, Mathf.Abs(pos.y));

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
