using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Trajectory : MonoBehaviour
{
    public LineRenderer lineRenderer { get; private set; }

    public IHasOrbit o;

    public float width = 0.01f;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Show in Map");

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.material = (Material)AssetDatabase.LoadAssetAtPath("Assets/Materials/Trajectory.mat", typeof(Material));
    }

    private void Update()
    {
        Vector2d pos = o.orbit.GetPosition();
        const int trajectorySubdivs = 400;
        Vector3[] points = new Vector3[trajectorySubdivs];

        double theta0, thetaMax;
        if (o.orbit.e <= 1.0)
        {
            theta0 = Math.Atan2(pos.y, pos.x);
            thetaMax = theta0 + 2 * Math.PI * (o.orbit.h > 0.0 ? 1 : -1);
            lineRenderer.loop = true;
        }
        else
        {
            double asymptote = Math.Acos(-1.0 / o.orbit.e);
            theta0 = o.orbit.omega - asymptote;
            thetaMax = o.orbit.omega + asymptote;
            lineRenderer.loop = false;
        }
        double dTheta = (thetaMax - theta0) / trajectorySubdivs;

        double p = o.orbit.GetSemimajorAxis() * (1 - o.orbit.e * o.orbit.e);

        int numPoints = 0;
        for (int i = 0; i < trajectorySubdivs; i++)
        {
            double theta = theta0 + i * dTheta;
            double r = p / (1.0 + (float)o.orbit.e * Math.Cos(theta - o.orbit.omega));
            if (r < 0.0) continue;
            points[numPoints++] = (float)r * new Vector3((float)Math.Cos(theta), (float)Math.Sin(theta));
        }
        lineRenderer.positionCount = numPoints;
        lineRenderer.SetPositions(points);
        lineRenderer.widthMultiplier = width * MapViewManager.Instance.activeCamera.orthographicSize;
    }
}
