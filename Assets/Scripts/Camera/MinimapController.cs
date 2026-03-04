using Orbit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapScaling : MonoBehaviour
{
    private Camera _camera;
    private FollowWorldTransform _follow;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _follow = GetComponent<FollowWorldTransform>();
    }

    private void Update()
    {
        var activeCraft = ActiveCraftController.Instance.craft;
        _follow.follow = activeCraft.body.transform;
        // autoscale minimap bounds to match spacecraft's orbit size
        double scale = _camera.orthographicSize;
        if (activeCraft.patches.FirstPatch.NextTransition is not SOIEscapeTransition)
        {
            scale = 2 * activeCraft.orbit.a;
        }
        else
        {
            scale = 3 * activeCraft.Position.Magnitude;
        }
        _camera.orthographicSize = (float)scale;
    }
}
