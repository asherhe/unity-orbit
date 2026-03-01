using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace UI
{
    public class MinimapDisplay : MonoBehaviour
    {
        private RectTransform rectTransform;
        public bool IsMinimapActive { get => MapViewManager.Instance.activeView == CameraView.FlightView; }

        private Vector2 activePos, inactivePos;
        private Vector2 TargetPos => IsMinimapActive ? activePos : inactivePos;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            activePos = rectTransform.anchoredPosition;
            inactivePos = rectTransform.anchoredPosition + Vector2.right * (rectTransform.rect.width + 40f);
            rectTransform.anchoredPosition = TargetPos;

            MapViewManager.WhenInstantiated(() =>
            {
                MapViewManager.Instance.OnMapToggled += ToggleMinimap;
            });
        }

        private void ToggleMinimap()
        {
            rectTransform.DOAnchorPos(TargetPos, 0.25f)
                .SetEase(IsMinimapActive ? Ease.OutCubic : Ease.InCubic);
        }
    }
}
