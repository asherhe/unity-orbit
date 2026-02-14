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
    public class MapLabel : MonoBehaviour
    {
        public RectTransform rectTransform { get; private set; }
        protected Image iconImage;

        [SerializeField]
        protected MapIcon icon;
        [SerializeField]
        protected TMP_Text labelText;

        private float _alpha = 1.0f;
        public float Alpha
        {
            get => _alpha;
            set { _alpha = value; UpdateVisuals(); }
        }

        private float _textAlpha = 0.0f;
        protected float TextAlpha
        {
            get => _textAlpha;
            set { _textAlpha = value; UpdateVisuals(); }
        }
        private Tweener _textAlphaTween;

        protected Color iconColor { get; private set; } = Color.white;
        protected Color textColor { get; private set; } = Color.white;
        protected void SetColors(Color icon, Color text)
        {
            iconColor = icon; textColor = text;
            UpdateVisuals();
        }

        protected virtual void Awake()
        {
            rectTransform = transform as RectTransform;

            iconImage = icon.GetComponent<Image>();
            icon.OnHoverEnter += OnHoverEnter;
            icon.OnHoverLeave += OnHoverLeave;
        }

        protected virtual void UpdateVisuals()
        {
            iconImage.color = new Color(iconColor.r, iconColor.g, iconColor.b, Alpha);
            labelText.color = new Color(textColor.r, textColor.g, textColor.b, Alpha * TextAlpha);
            iconImage.raycastTarget = Alpha != 0.0f;
        }
        private void OnHoverEnter()
        {
            rectTransform.DOScale(1.25f, 0.25f).SetEase(Ease.OutCubic);
            _textAlphaTween?.Kill();
            labelText.gameObject.SetActive(true);
            _textAlphaTween = DOTween.To(
                () => TextAlpha,
                v => TextAlpha = v,
                1.0f, 0.25f
            );
        }
        private void OnHoverLeave()
        {
            rectTransform.DOScale(1.0f, 0.25f).SetEase(Ease.OutCubic);
            _textAlphaTween?.Kill();
            _textAlphaTween = DOTween.To(
                () => TextAlpha,
                v => TextAlpha = v,
                0.0f, 0.25f
            ).OnComplete(() => labelText.gameObject.SetActive(false));
        }
    }
}
