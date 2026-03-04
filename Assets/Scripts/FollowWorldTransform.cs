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

    public bool keepZPosition = false;

    private void LateUpdate()
    {
        if (follow != null)
        {
            if (shouldFollowPosition) {
                if (keepZPosition) transform.position = new(follow.position.x, follow.position.y, transform.position.z);
                else transform.position = follow.position;
            }
            if (shouldFollowRotation) transform.rotation = follow.rotation;
        }
    }
}
