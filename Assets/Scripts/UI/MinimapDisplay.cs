using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace UI
{
    public class MinimapDisplay : MonoBehaviour
    {
        public bool IsMinimapActive { get => MapViewManager.Instance.activeView == CameraView.FlightView; }

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            MapViewManager.Instance.MapToggled += ToggleMinimap;
        }

        private void ToggleMinimap()
        {
            _rectTransform.DOAnchorPosX(
               IsMinimapActive ? -10.0f : 200.0f,
                0.25f
            ).SetEase(IsMinimapActive ? Ease.OutCubic : Ease.InCubic);
        }
    }
}
