using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public class MapIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public RectTransform rectTransform { get; private set; }
        private Image _iconImage;

        public Color color
        {
            get => _iconImage.color;
            set
            {
                _iconImage.color = value;
                _iconImage.raycastTarget = _iconImage.color.a != 0.0f;
            }
        }
        public Sprite sprite
        {
            get => _iconImage.sprite;
            set => _iconImage.sprite = value;
        }

        public event Action OnInitialized;

        public event Action OnHoverEnter;
        public event Action OnHoverLeave;
        public event Action OnClick;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            _iconImage = GetComponent<Image>();
            OnInitialized?.Invoke();
        }

        public void OnPointerEnter(PointerEventData data) { OnHoverEnter?.Invoke(); }
        public void OnPointerExit(PointerEventData data) { OnHoverLeave?.Invoke(); }
        public void OnPointerClick(PointerEventData data) { OnClick?.Invoke(); }
    }
}