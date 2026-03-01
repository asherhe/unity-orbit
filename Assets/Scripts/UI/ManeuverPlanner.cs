using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ManeuverPlanner : MonoBehaviour
    {
        public RectTransform rectTransform { get; private set; }
        private InputActions _inputActions;

        public bool IsPlannerActive { get; private set; } = false;

        /// <summary>
        /// maneuver that is currently being planned
        /// </summary>
        private Orbit.Maneuver _maneuver;

        private Vector2 activePos, inactivePos;
        private Vector2 TargetPos => IsPlannerActive ? activePos : inactivePos;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            activePos = rectTransform.anchoredPosition;

            InputReader.WhenInstantiated(() =>
            {
                _inputActions = InputReader.Instance.Actions;
                _inputActions.MapView.Maneuver.performed += ctx => TogglePlanner();
            });
        }

        private void OnRectTransformDimensionsChange()
        {
            // wait until ContentSizeFitter does its job
            if (rectTransform == null) rectTransform = transform as RectTransform;
            inactivePos = activePos + Vector2.up * (rectTransform.rect.height + 40f);
            rectTransform.anchoredPosition = TargetPos;
        }

        private void TogglePlanner()
        {
            IsPlannerActive = !IsPlannerActive;
            rectTransform.DOAnchorPos(TargetPos, 0.25f)
                .SetEase(IsPlannerActive ? Ease.OutCubic : Ease.InCubic);
        }
    }
}