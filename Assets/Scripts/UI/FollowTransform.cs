using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    /// <summary>
    /// transform to follow
    /// </summary>
    public Transform follow;

    private void LateUpdate()
    {
        if (follow != null)
            transform.position = follow.position;
    }
}
