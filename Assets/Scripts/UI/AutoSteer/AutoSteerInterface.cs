using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Orbit;
using System;

namespace UI
{
    public class AutoSteerInterface : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Spacecraft _craft;
        private Parts.CommandPlugin _command;

        private ToggleButtonGroup _toggleGroup;

        [SerializeField]
        private float _holdRadius = 40.0f;

        [SerializeField]
        private AutoSteerHandle _handle;

        private Tweener _handleTween;
        public enum HoldMode { None, Prograde, Retrograde, RadialOut, RadialIn, Maneuver }

        public HoldMode holdMode = HoldMode.None;

        [Serializable]
        public struct HoldButtons
        {
            public HoldMode mode;
            public SpriteToggleButton button;
        }
        [SerializeField]
        private List<HoldButtons> _holdButtons = new();

        private SpriteToggleButton _maneuverButton;

        /// <summary>
        /// maneuver planner that we want the maneuver hold mode to refer to
        /// </summary>
        [SerializeField]
        private ManeuverPlanner _maneuverPlanner;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _rectTransform.localScale = Vector3.zero;

            _toggleGroup = GetComponent<ToggleButtonGroup>();

            gameObject.SetActive(false);
            _maneuverButton = _holdButtons.Find(holdButton => holdButton.mode == HoldMode.Maneuver).button;
            _maneuverPlanner.OnPlannerToggled += () =>
            {
                _maneuverButton.gameObject.SetActive(_maneuverPlanner.IsPlannerActive);
                if (holdMode == HoldMode.Maneuver && !_maneuverPlanner.IsPlannerActive)
                    holdMode = HoldMode.None;
            };
            _maneuverButton.gameObject.SetActive(_maneuverPlanner.IsPlannerActive);

            ActiveCraftController.WhenInstantiated(() =>
            {
                _craft = ActiveCraftController.Instance.craft;
                _craft.OnLoaded += () =>
                {
                    _command = ActiveCraftController.Instance.command;
                    _command.OnAutoSteerToggled += ToggleInterface;
                };
            });

            _handle.OnHandleDragged += OnHandleDrag;
            _toggleGroup.OnActiveSwitched += OnHoldModeChanged;
        }

        private void Update()
        {
            foreach (var holdButton in _holdButtons)
                holdButton.button.rectTransform.anchoredPosition = _holdRadius * GetHoldDirection(holdButton.mode);

            if (holdMode != HoldMode.None && (_handleTween == null || !_handleTween.IsActive()))
                _handle.Direction = HoldDirection;

            // 0 radians on spacecraft is up
            _command.autosteerTarget = _handle.Direction - 0.5f * Mathf.PI;
        }

        private void ToggleInterface()
        {
            var isEnabled = _command.IsAutoSteerEnabled;
            if (isEnabled)
            {
                // we don't immediately enable the gameobject when autosteer is turned off
                // that has to wait until after the tween completes
                gameObject.SetActive(isEnabled);

                // initialize steer direction to current orientation
                _handle.Direction = (float)_craft.Newtonian.angle + 0.5f * Mathf.PI;
            }

            _rectTransform.DOScale(isEnabled ? 1.0f : 0.0f, 0.25f)
                .SetEase(isEnabled ? Ease.OutBack : Ease.InCubic)
                .OnComplete(() => { if (!isEnabled) gameObject.SetActive(isEnabled); });
        }

        /// <summary>
        /// invoked by _handle when it is dragged
        /// </summary>
        private void OnHandleDrag()
        {
            if (_toggleGroup.activeButton != null) _toggleGroup.activeButton.IsActive = false;
        }

        public Vector2d GetHoldDirection(HoldMode mode)
        {
            if (mode != HoldMode.None)
            {
                if (mode <= HoldMode.RadialIn)
                    return _craft.GetPRDirection((PRDirection)mode);
                if (mode == HoldMode.Maneuver)
                    return ManeuverSystem.Instance.HasManeuver ? ManeuverSystem.Instance.NextManeuver.DvRemaining.Normalized : Vector2d.zero;
            }
            return Vector2d.right.Rotate(_handle.Direction);
        }
        public float HoldDirection
        {
            get
            {
                var dir = GetHoldDirection(holdMode);
                return (float)Math.Atan2(dir.y, dir.x);
            }
        }

        /// <summary>
        /// invoked by _toggleGroup when the active hold mode is changed
        /// </summary>
        private void OnHoldModeChanged()
        {
            if (_toggleGroup.activeButton == null) holdMode = HoldMode.None;

            foreach (var holdButton in _holdButtons)
            {
                if (_toggleGroup.activeButton == holdButton.button)
                {
                    holdMode = holdButton.mode;
                    break;
                }
            }

            if (holdMode != HoldMode.None)
            {
                // tween by closest angular distance along circle to destination
                _handleTween = DOTween.To(
                    () => _handle.Direction,
                    v => _handle.Direction = v,
                    Mathf.Deg2Rad * Mathf.DeltaAngle(_handle.Direction * Mathf.Rad2Deg, HoldDirection * Mathf.Rad2Deg),
                    0.1f
                ).SetRelative().SetEase(Ease.OutCubic);
            }
        }
    }
}