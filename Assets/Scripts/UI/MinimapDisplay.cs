using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MinimapDisplay : MonoBehaviour
{
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        MapViewManager.Instance.MapToggled += ToggleMinimap;
    }

    private void ToggleMinimap()
    {
        _rectTransform.DOAnchorPosX(
            MapViewManager.Instance.activeView == CameraView.MapView ? 200.0f : -10.0f,
            0.25f
        ).SetEase(MapViewManager.Instance.activeView == CameraView.MapView ? Ease.InCubic : Ease.OutCubic);
    }
}
