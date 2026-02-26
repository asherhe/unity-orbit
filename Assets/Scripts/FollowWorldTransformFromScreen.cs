using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FollowWorldTransformFromScreen : MonoBehaviour
{
    /// <summary>
    /// world-space transform to follow
    /// </summary>
    public Transform follow;

    private static Canvas _canvas;
    private static RectTransform _canvasRectTransform;
    private RectTransform _rectTransform;

    public bool shouldFollowPosition = true;
    public bool shouldFollowRotation = false;

    private void Awake()
    {
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
            _canvasRectTransform = _canvas.GetComponent<RectTransform>();
        }
        _rectTransform = GetComponent<RectTransform>();
    }

    public static Vector2 WorldToCanvas(Vector3 pos)
    {
        var screenPos = Camera.main.WorldToScreenPoint(pos);
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, screenPos, null, out canvasPos);
        return canvasPos;
    }

    private void LateUpdate()
    {
        if (follow != null)
        {
            if (shouldFollowPosition)
            {
                // note: ensure that this RectTransform is anchored to the canvas origin
                _rectTransform.anchoredPosition = WorldToCanvas(follow.position);
            }
            if (shouldFollowRotation) transform.rotation = follow.rotation;
        }
    }
}
