using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapScaling : MonoBehaviour
{
    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        var activeCraft = ActiveCraftController.Instance.craft;
        // autoscale minimap bounds to match spacecraft's orbit size
        double scale = _camera.orthographicSize;
        if (activeCraft.orbit.Shape == Orbit.OrbitShape.Ellipse)
        {
            // this is a pretty bad scale factor since near-parabolic trajectories will extend to ridiculous distances
            // make some smarter scaling later on
            scale = 3 * activeCraft.orbit.a;
        }
        else
        {
            scale = 3 * activeCraft.GetPosition().Magnitude;
        }
        _camera.orthographicSize = (float)scale;
    }
}
