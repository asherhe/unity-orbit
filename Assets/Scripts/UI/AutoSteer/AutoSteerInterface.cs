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

        [SerializeField]
        private float _holdRadius = 40.0f;

        [SerializeField]
        private AutoSteerHandle _handle;

        [SerializeField]
        private RectTransform _progradeHold;
        [SerializeField]
        private RectTransform _retrogradeHold;
        [SerializeField]
        private RectTransform _radialOutHold;
        [SerializeField]
        private RectTransform _radialInHold;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _rectTransform.localScale = Vector3.zero;
            gameObject.SetActive(false);

            _craft = ActiveCraftController.Instance.craft;
            _craft.OnLoaded += () =>
            {
                _command = ActiveCraftController.Instance.command;
                _command.OnAutoSteerToggled += ToggleHandles;
            };

            _handle.OnHandleDragged += () =>
            {
                // TODO: set command target direction
            };
        }

        private void Update()
        {
            var progradePos = _holdRadius * (Vector2)_craft.GetVelocity().Normalized;
            var radialOutPos = Mathf.Sign((float)_craft.orbit.h) * new Vector2(progradePos.y, -progradePos.x);

            _progradeHold.anchoredPosition = progradePos;
            _retrogradeHold.anchoredPosition = -progradePos;
            _radialOutHold.anchoredPosition = radialOutPos;
            _radialInHold.anchoredPosition = -radialOutPos;
        }

        private void ToggleHandles()
        {
            var isEnabled = _command.IsAutoSteerEnabled;
            if (isEnabled) gameObject.SetActive(isEnabled);

            _rectTransform.DOScale(isEnabled ? 1.0f : 0.0f, 0.25f)
                .SetEase(isEnabled ? Ease.OutBack : Ease.InCubic)
                .OnComplete(() => { if (!isEnabled) gameObject.SetActive(isEnabled); });
        }
    }
}