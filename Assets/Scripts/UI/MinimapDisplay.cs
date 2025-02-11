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
        DOTween.To(
            () => _rectTransform.anchoredPosition.x,
            x => _rectTransform.anchoredPosition = new Vector2(x, _rectTransform.anchoredPosition.y),
            MapViewManager.Instance.isInMapView ? 200.0f : -10.0f,
            0.25f
        ).SetEase(MapViewManager.Instance.isInMapView ? Ease.InCubic : Ease.OutCubic);
    }
}
