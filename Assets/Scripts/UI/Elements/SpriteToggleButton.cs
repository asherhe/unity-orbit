using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class SpriteToggleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public RectTransform rectTransform { get; private set; }
        private Image _image;

        [SerializeField]
        private Sprite _sprite;
        [SerializeField]
        private Sprite _activeSprite;

        [SerializeField]
        private float _hoverScale = 1.25f;
        [SerializeField]
        private float _hoverTransitionTime = 0.25f;

        private bool _isActive = false;
        /// <summary>
        /// whether this button is active
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value) return;
                _isActive = value;
                OnToggled?.Invoke();
            }
        }

        public event Action OnToggled;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            _image = GetComponent<Image>();

            OnToggled += UpdateVisuals;
            UpdateVisuals();
        }

        public void OnPointerEnter(PointerEventData data)
        {
            rectTransform.DOScale(_hoverScale, _hoverTransitionTime)
                .SetEase(Ease.OutCubic);
        }
        public void OnPointerExit(PointerEventData data)
        {
            rectTransform.DOScale(1.0f, _hoverTransitionTime)
                .SetEase(Ease.OutCubic);
        }

        public void OnPointerClick(PointerEventData data)
        {
            IsActive = !IsActive;
        }

        private void UpdateVisuals()
        {
            _image.sprite = IsActive ? _activeSprite : _sprite;
        }
    }
}