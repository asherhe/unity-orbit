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
    public class AutoSteerHandle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform _rectTransform;
        private RectTransform _parentTransform;
        private Canvas _canvas;
        private Image _image;

        [SerializeField]
        private float _handleRadius = 64.0f;

        /// <summary>
        /// sprite to show under normal circumstances
        /// </summary>
        [SerializeField]
        private Sprite _sprite;
        /// <summary>
        /// sprite to show when handle is active
        /// </summary>
        [SerializeField]
        private Sprite _activeSprite;

        /// <summary>
        /// line connector from the origin to this handle
        /// </summary>
        [SerializeField]
        private RectTransform _lineTransform;

        private float _direction = 0.0f;
        /// <summary>
        /// current handle direction, [-PI, PI]
        /// </summary>
        public float Direction
        {
            get => _direction;
            set { _direction = value; UpdateVisuals(); }
        }

        public event Action OnHandleDragged;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _parentTransform = transform.parent as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            _image = GetComponent<Image>();

            _lineTransform.pivot = new Vector2(0.0f, 0.5f);
            _lineTransform.anchoredPosition = Vector2.zero;
        }

        public void OnPointerEnter(PointerEventData data)
        {
            _rectTransform.DOScale(1.25f, 0.25f)
                .SetEase(Ease.OutCubic);
        }
        public void OnPointerExit(PointerEventData data)
        {
            _rectTransform.DOScale(1.0f, 0.25f)
                .SetEase(Ease.OutCubic);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            _image.sprite = _activeSprite;
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            Vector2 pos;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                data.position,
                null,
                out pos
            )) return;

            var dir = pos - _parentTransform.anchoredPosition;
            Direction = Mathf.Atan2(dir.y, dir.x);

            OnHandleDragged?.Invoke();
        }
        public void OnEndDrag(PointerEventData data)
        {
            _image.sprite = _sprite;
        }

        private void UpdateVisuals()
        {
            _rectTransform.anchoredPosition = _handleRadius * new Vector2(Mathf.Cos(Direction), Mathf.Sin(Direction));
            _lineTransform.localEulerAngles = new Vector3(0.0f, 0.0f, Direction * Mathf.Rad2Deg);
        }
    }
}