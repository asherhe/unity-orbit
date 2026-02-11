using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace UI
{
    public class AutoSteerInterface : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Spacecraft _craft;
        private Parts.CommandPlugin _command;

        private ToggleButtonGroup _toggleGroup;

        /// <summary>
        /// prograde vector, or craft velocity
        /// </summary>
        private Vector2d _prograde = Vector2d.zero;

        [SerializeField]
        private float _holdRadius = 40.0f;

        [SerializeField]
        private AutoSteerHandle _handle;

        private Tweener _handleTween;

        [SerializeField]
        private SpriteToggleButton _progradeHold;
        [SerializeField]
        private SpriteToggleButton _retrogradeHold;
        [SerializeField]
        private SpriteToggleButton _radialOutHold;
        [SerializeField]
        private SpriteToggleButton _radialInHold;

        public enum HoldMode { None, Prograde, Retrograde, RadialOut, RadialIn }
        public HoldMode holdMode = HoldMode.None;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _rectTransform.localScale = Vector3.zero;

            _toggleGroup = GetComponent<ToggleButtonGroup>();

            gameObject.SetActive(false);

            _craft = ActiveCraftController.Instance.craft;
            _craft.OnLoaded += () =>
            {
                _command = ActiveCraftController.Instance.command;
                _command.OnAutoSteerToggled += ToggleInterface;
            };

            _handle.OnHandleDragged += OnHandleDrag;
            _toggleGroup.OnActiveSwitched += OnHoldModeChanged;
        }

        public float GetDirection(HoldMode mode)
        {
            var progradeDir = Mathf.Atan2((float)_prograde.y, (float)_prograde.x);
            var orbitDirection = _craft.orbit.h > 0 ? 1.0f : -1.0f;
            switch (mode)
            {
                case HoldMode.Prograde:
                    return progradeDir;
                case HoldMode.Retrograde:
                    return MathUtils.NormalizeAngle(progradeDir + Mathf.PI);
                case HoldMode.RadialOut:
                    return MathUtils.NormalizeAngle(progradeDir - orbitDirection * 0.5f * Mathf.PI);
                case HoldMode.RadialIn:
                    return MathUtils.NormalizeAngle(progradeDir + orbitDirection * 0.5f * Mathf.PI);
                default:
                    return _handle.Direction;
            }
        }

        private void Update()
        {
            var progradePos = _holdRadius * (Vector2)_prograde.Normalized;
            var radialOutPos = Mathf.Sign((float)_craft.orbit.h) * new Vector2(progradePos.y, -progradePos.x);

            ((RectTransform)_progradeHold.transform).anchoredPosition = progradePos;
            ((RectTransform)_retrogradeHold.transform).anchoredPosition = -progradePos;
            ((RectTransform)_radialOutHold.transform).anchoredPosition = radialOutPos;
            ((RectTransform)_radialInHold.transform).anchoredPosition = -radialOutPos;

            if (holdMode != HoldMode.None && (_handleTween == null || !_handleTween.IsActive()))
                _handle.Direction = GetDirection(holdMode);
        }

        private void FixedUpdate()
        {
            _prograde = _craft.GetVelocity();
        }

        private void ToggleInterface()
        {
            var isEnabled = _command.IsAutoSteerEnabled;
            if (isEnabled) gameObject.SetActive(isEnabled);

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
            // TODO: set command target direction
        }

        /// <summary>
        /// invoked by _toggleGroup when the active hold mode is changed
        /// </summary>
        private void OnHoldModeChanged()
        {
            if (_toggleGroup.activeButton == null) holdMode = HoldMode.None;
            if (_toggleGroup.activeButton == _progradeHold) holdMode = HoldMode.Prograde;
            if (_toggleGroup.activeButton == _retrogradeHold) holdMode = HoldMode.Retrograde;
            if (_toggleGroup.activeButton == _radialOutHold) holdMode = HoldMode.RadialOut;
            if (_toggleGroup.activeButton == _radialInHold) holdMode = HoldMode.RadialIn;

            if (holdMode != HoldMode.None)
            {
                float targetDirection = GetDirection(holdMode);
                _handleTween = DOTween.To(
                    () => _handle.Direction,
                    v => _handle.Direction = v,
                    Mathf.Deg2Rad * Mathf.DeltaAngle(_handle.Direction * Mathf.Rad2Deg, targetDirection * Mathf.Rad2Deg),
                    0.1f
                ).SetRelative().SetEase(Ease.OutCubic);
            }
        }
    }
}