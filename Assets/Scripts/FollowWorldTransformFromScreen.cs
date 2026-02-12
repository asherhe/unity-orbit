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

    private Canvas _canvas;
    private RectTransform _canvasRectTransform;
    private RectTransform _rectTransform;

    public bool shouldFollowPosition = true;
    public bool shouldFollowRotation = false;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _canvasRectTransform = _canvas.GetComponent<RectTransform>();
        _rectTransform = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (follow != null)
        {
            if (shouldFollowPosition) {
                var screenPos = Camera.main.WorldToScreenPoint(follow.position);
                transform.position = follow.position;
                Vector2 canvasPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, screenPos, null, out canvasPos);
                // ensure that this RectTransform is anchored to the canvas origin
                _rectTransform.anchoredPosition = canvasPos;
            }
            if (shouldFollowRotation) transform.rotation = follow.rotation;
        }
    }
}
