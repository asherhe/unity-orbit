using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class FollowWorldTransform : MonoBehaviour
{
    /// <summary>
    /// transform to follow
    /// </summary>
    public Transform follow;

    public bool shouldFollowPosition = true;
    public bool shouldFollowRotation = false;

    private void LateUpdate()
    {
        if (follow != null)
        {
            if (shouldFollowPosition) transform.position = follow.position;
            if (shouldFollowRotation) transform.rotation = follow.rotation;
        }
    }
}
