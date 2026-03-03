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
        public bool IsPlannerActive
        {
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
        public Orbit.Maneuver maneuver;

        /// <summary>
        /// invoked when the state of the maneuver currently being planned is updated
        /// </summary>
        public event Action OnManeuverStateUpdate;
        /// <summary>
        /// invoked when we get a different maneuver to plan. OnManeuverStateUpdate will also be invoked after this event
        /// </summary>
        public event Action OnManeuverChanged;
        /// <summary>
        /// invoked when this planner is turned on or off
        /// </summary>
        public event Action OnPlannerToggled;

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

            gameObject.SetActive(IsPlannerActive);

            InputReader.WhenInstantiated(() =>
            {
                _inputActions = InputReader.Instance.Actions;
                _inputActions.MapView.Maneuver.performed += ctx => IsPlannerActive = !IsPlannerActive;
            });

            _closeButton.onClick.AddListener(() =>
            {
                ManeuverSystem.Instance.RemoveManeuver(maneuver);
                IsPlannerActive = false;
            });

            _timeField.formatter = t => "T" + TextDisplay.FormatTime(t - Universe.Instance.UT, showSign: true);
            _progradeField.formatter = v => "<color=#76D535>PRGD</color> " + TextDisplay.FormatSpeed(v, showSign: true);
            _radialField.formatter = v => "<color=#639BFF>RADL</color> " + TextDisplay.FormatSpeed(v, showSign: true);
            _incrementField.formatter = i => $"<sprite name=\"pm\">{TextDisplay.FormatSpeed(speedIncrements[(int)i])};<sprite name=\"pm\">{TextDisplay.FormatTime(timeIncrements[(int)i], shorten: true)}";

            OnManeuverStateUpdate += UpdateManeuverFieldValues;
            OnManeuverStateUpdate += UpdateManeuverInfoDisplay;
            OnManeuverChanged += ResetManeuverFieldValues;

            _timeField.OnValueChanged += UpdateManeuverTime;
            _progradeField.OnValueChanged += UpdateManeuverDv;
            _radialField.OnValueChanged += UpdateManeuverDv;
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
            _incrementField.Value = Math.Clamp(_incrementField.Value, 0, Math.Min(speedIncrements.Count, timeIncrements.Count) - 1);
            _timeField.increment = timeIncrements[(int)_incrementField.Value];
            _progradeField.increment = _radialField.increment = speedIncrements[(int)_incrementField.Value];
        }

        private void UpdateManeuverTime()
        {
            maneuver.UT = _timeField.Value;
            // we want to keep PR velocity change the same
            UpdateManeuverDv();
        }
        private void UpdateManeuverDv() => maneuver.DvPR = new(_progradeField.Value, _radialField.Value);

        /// <summary>
        /// called when the maneuver planner changes from enabled to disabled and vice versa
        /// </summary>
        private void OnActivityChanged()
        {
            if (IsPlannerActive) gameObject.SetActive(true);

            rectTransform.DOAnchorPos(TargetPos, 0.25f)
                .SetEase(IsPlannerActive ? Ease.OutCubic : Ease.InCubic)
                .OnComplete(() => { if (!IsPlannerActive) gameObject.SetActive(false); });

            void InvokeManeuverUpdate() => OnManeuverStateUpdate?.Invoke();

            if (IsPlannerActive)
            {
                maneuver = ManeuverSystem.Instance.GetManeuver();
                maneuver.OnManeuverStateUpdate += InvokeManeuverUpdate;
                OnManeuverChanged.Invoke();
                InvokeManeuverUpdate();
            }
            else
            {
                if (maneuver != null) maneuver.OnManeuverStateUpdate -= InvokeManeuverUpdate;
            }
            OnPlannerToggled?.Invoke();
        }

        private void UpdateManeuverFieldValues()
        {
            _timeField.Value = (float)maneuver.UT;
            // we don't change velocity fields because we don't want the prograde/radial out values to change when we shift the orbit phase
        }

        private void ResetManeuverFieldValues()
        {
            _timeField.SetValueSilent((float)maneuver.UT);
            var dvPR = maneuver.DvPR;
            _progradeField.SetValueSilent((float)dvPR.x);
            _radialField.SetValueSilent((float)dvPR.y);
        }

        private void UpdateManeuverInfoDisplay()
        {
            _burnTimeDisplay.text = "Burn time: " + TextDisplay.FormatTime(maneuver.BurnTime, shorten: true);
        }

        private void Update()
        {
            var dvRemaining = maneuver.DvRemaining.Magnitude;
            var dvTotal = maneuver.Dv.Magnitude;
            var remainingColor = dvRemaining < Math.Min(1.0, 0.1 * dvTotal) ? "green" : "grey";
            _dvDisplay.text = $"<sprite name=\"dv\"> left: <color={remainingColor}>{TextDisplay.FormatSpeed(dvRemaining, showUnits: false)}</color>/{TextDisplay.FormatSpeed(dvTotal)}";
            _burnCountdownDisplay.text = "In T" + TextDisplay.FormatTime(Universe.Instance.UT - (maneuver.UT - 0.5 * maneuver.BurnTime), showSign: true);
        }
    }
}