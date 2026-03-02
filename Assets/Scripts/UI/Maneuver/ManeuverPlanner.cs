using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ManeuverPlanner : MonoBehaviour
    {
        public RectTransform rectTransform { get; private set; }
        private InputActions _inputActions;

        [SerializeField]
        private ManeuverField _timeField, _progradeField, _radialField, _incrementField;

        [SerializeField]
        private Button _closeButton;

        [SerializeField]
        private TMP_Text _dvDisplay, _burnTimeDisplay, _burnCountdownDisplay;

        private bool _isPlannerActive = false;
        public bool IsPlannerActive { 
            get => _isPlannerActive;
            private set
            {
                if (value == _isPlannerActive) return;
                _isPlannerActive = value;
                OnActivityChanged();
            }
        }

        private Vector2 activePos, inactivePos;
        private Vector2 TargetPos => IsPlannerActive ? activePos : inactivePos;

        /// <summary>
        /// maneuver that is currently being planned
        /// </summary>
        private Orbit.Maneuver _maneuver;

        private readonly List<float> speedIncrements = new()
        {
            0.1f, 1, 5, 10, 50, 100, 500, 1000, 5000
        };
        private readonly List<float> timeIncrements = new()
        {
            1, 10, 60, 600, 3600, 36000, 86400, 864000, 8640000
        };

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            activePos = rectTransform.anchoredPosition;

            InputReader.WhenInstantiated(() =>
            {
                _inputActions = InputReader.Instance.Actions;
                _inputActions.MapView.Maneuver.performed += ctx => IsPlannerActive = !IsPlannerActive;
            });

            _closeButton.onClick.AddListener(() =>
            {
                ManeuverSystem.Instance.RemoveManeuver(_maneuver);
                IsPlannerActive = false;
            });

            _timeField.formatter = t => "T" + TextDisplay.FormatTime(t - Universe.Instance.UT, showSign: true);
            _progradeField.formatter = v => "PRGD " + TextDisplay.FormatSpeed(v, showSign: true);
            _radialField.formatter = v => "RADL " + TextDisplay.FormatSpeed(v, showSign: true);
            _incrementField.formatter = i => $"<sprite name=\"pm\">{TextDisplay.FormatSpeed(speedIncrements[(int)i])};<sprite name=\"pm\">{TextDisplay.FormatTime(timeIncrements[(int)i], shorten: true)}";

            _incrementField.OnValueChanged += UpdateFieldIncrements;
            UpdateFieldIncrements();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (rectTransform == null) return;
            // wait until ContentSizeFitter does its job
            inactivePos = activePos + Vector2.left * (rectTransform.rect.width + 40f);
            rectTransform.anchoredPosition = TargetPos;
        }

        private void UpdateFieldIncrements()
        {
            _incrementField.value = Math.Clamp(_incrementField.value, 0, Math.Min(speedIncrements.Count, timeIncrements.Count) - 1);
            _timeField.increment = timeIncrements[(int)_incrementField.value];
            _progradeField.increment = _radialField.increment = speedIncrements[(int)_incrementField.value];
        }

        /// <summary>
        /// called when the maneuver planner changes from enabled to disabled and vice versa
        /// </summary>
        private void OnActivityChanged()
        {
            rectTransform.DOAnchorPos(TargetPos, 0.25f)
                .SetEase(IsPlannerActive ? Ease.OutCubic : Ease.InCubic);

            if (IsPlannerActive)
            {
                _maneuver = ManeuverSystem.Instance.GetManeuver();
                OnActiveManeuverChanged();
            }
        }

        /// <summary>
        /// called when the maneuver that is currently being planned is changed to a different one
        /// </summary>
        private void OnActiveManeuverChanged()
        {
            _timeField.value = (float)_maneuver.UT;
        }
    }
}