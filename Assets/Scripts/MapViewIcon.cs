using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSpaceIcon : MonoBehaviour
{
    public float scale = 0.1f;

    private void Update()
    {
        transform.localScale = Vector3.one * scale * MapViewManager.Instance.activeCamera.orthographicSize;
    }
}
