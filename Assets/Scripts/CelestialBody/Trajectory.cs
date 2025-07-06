using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

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
        // that UV doesn't interpolate between 0 and 1 for the loop segment
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

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Trajectory : MonoBehaviour
{
    private TrajectoryMesh trajectoryMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    public IOrbitingObject o;

    private void Awake()
    {
        trajectoryMesh = new TrajectoryMesh();
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = trajectoryMesh.mesh;
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = (Material)AssetDatabase.LoadAssetAtPath("Assets/Materials/Trajectory.mat", typeof(Material));
    }

    private void Update()
    {
        Vector2d pos = o.orbit.GetPosition();
        const int trajectorySubdivs = 400;
        List<Vector2> points = new List<Vector2>(trajectorySubdivs);

        double theta0, thetaMax;
        if (o.orbit.e <= 1.0)
        {
            theta0 = 0;
            thetaMax = theta0 + 2 * Math.PI * (o.orbit.h > 0.0 ? 1 : -1);
            trajectoryMesh.loop = true;
        }
        else
        {
            double asymptote = Math.Acos(-1.0 / o.orbit.e);
            theta0 = o.orbit.omega + asymptote;
            thetaMax = o.orbit.omega - asymptote;
            trajectoryMesh.loop = false;
        }
        double dTheta = (thetaMax - theta0) / trajectorySubdivs;

        double p = o.orbit.SemimajorAxis * (1 - o.orbit.e * o.orbit.e);
        for (int i = 0; i < trajectorySubdivs; i++)
        {
            double theta = theta0 + i * dTheta;
            double r = p / (1.0 + (float)o.orbit.e * Math.Cos(theta - o.orbit.omega));
            if (r > 0.0)
                points.Add((float)r * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta)));
        }

        trajectoryMesh.SetPointList(points);
        trajectoryMesh.UpdateMesh();
    }
}
