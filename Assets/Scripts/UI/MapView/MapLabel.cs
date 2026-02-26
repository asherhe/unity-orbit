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
        private float _targetTextAlpha;

        private bool _isHovered = false;
        /// <summary>
        /// whether this label is currently being hovered
        /// </summary>
        protected bool IsHovered
        {
            get => _isHovered;
            private set { _isHovered = value; TweenTextAlpha(); }
        }

        private bool _showLabel = false;
        /// <summary>
        /// whether to show label text even without hover
        /// </summary>
        protected bool ShowLabel
        {
            get => _showLabel;
            set { _showLabel = value; TweenTextAlpha(); }
        }

        protected Color iconColor { get; private set; }
        protected Color textColor { get; private set; }
        protected void SetColors(Color icon, Color text)
        {
            iconColor = icon; textColor = text;
            UpdateVisuals();
        }

        protected virtual void Awake()
        {
            rectTransform = transform as RectTransform;

            icon.OnHoverEnter += OnHoverEnter;
            icon.OnHoverLeave += OnHoverLeave;

            iconColor = icon.color;
            textColor = labelText.color;
        }

        protected virtual void UpdateVisuals()
        {
            icon.color = new Color(iconColor.r, iconColor.g, iconColor.b, Alpha);
            labelText.color = new Color(textColor.r, textColor.g, textColor.b, Alpha * TextAlpha);
        }

        private void TweenTextAlpha()
        {
            var alpha = IsHovered || ShowLabel ? 1.0f : 0.0f;
            if (_targetTextAlpha != alpha)
            {
                _targetTextAlpha = alpha;
                if (alpha == 1.0f) labelText.gameObject.SetActive(true);
                _textAlphaTween?.Kill();
                _textAlphaTween = DOTween.To(
                    () => TextAlpha,
                    v => TextAlpha = v,
                    alpha, 0.25f
                );
                if (alpha == 0.0f) _textAlphaTween.OnComplete(() => labelText.gameObject.SetActive(false));
            }
        }
        private void OnHoverEnter()
        {
            IsHovered = true;
            rectTransform.DOScale(1.25f, 0.25f).SetEase(Ease.OutCubic);
        }
        private void OnHoverLeave()
        {
            IsHovered = false;
            rectTransform.DOScale(1.0f, 0.25f).SetEase(Ease.OutCubic);
        }
    }
}
