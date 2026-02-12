using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class CelestialBodyLabel : ObjectLabel
    {
        private RectTransform rectTransform;
        private Image _iconImage;

        [SerializeField]
        private CelestialBodyIcon _icon;
        [SerializeField]
        private TMP_Text _labelText;

        private float _alpha = 1.0f;
        public float Alpha
        {
            get => _alpha;
            set { _alpha = value; UpdateVisuals(); }
        }

        private float _textAlpha = 0.0f;
        private float TextAlpha
        {
            get => _textAlpha;
            set { _textAlpha = value; UpdateVisuals(); }
        }
        private Tweener _textAlphaTween;

        /// <summary>
        /// Owner as a CelestialBody
        /// </summary>
        private CelestialBody _body;

        protected override void Awake()
        {
            base.Awake();
            rectTransform = transform as RectTransform;

            _iconImage = _icon.GetComponent<Image>();
            _icon.OnHoverEnter += OnHoverEnter;
            _icon.OnHoverLeave += OnHoverLeave;
        }

        private void Update()
        {
            if (_body.orbit == null) return;
            var r = (float)_body.radius;
            var a = (float)_body.orbit.a;
            // size of on-screen icon in world space
            var iconSize = _iconImage.rectTransform.rect.width * IntegerCanvasScale.Instance.Canvas2World;

            // alpha for hiding label when we zoom in close enough
            var hideSOI = (iconSize / r - 2.0f) / 2.0f;
            // alpha for hiding label to avoid interfering with parent
            var hideParent = (0.5f - iconSize / a) / 0.5f;

            Alpha = Mathf.Clamp01(Mathf.Min(hideSOI, hideParent));
        }

        protected override void UpdateVisuals()
        {
            base.UpdateVisuals();
            _body = (CelestialBody)Owner;

            _iconImage.color = new Color(_body.color.r, _body.color.g, _body.color.b, Alpha);

            _labelText.text = _body.bodyName;
            _labelText.color = _body.color;
            _labelText.color = new Color(_body.color.r, _body.color.g, _body.color.b, Alpha * TextAlpha);

            _iconImage.raycastTarget = Alpha != 0.0f;
        }

        public void OnHoverEnter()
        {
            rectTransform.DOScale(1.25f, 0.25f).SetEase(Ease.OutCubic);
            _textAlphaTween?.Kill();
            _labelText.gameObject.SetActive(true);
            _textAlphaTween = DOTween.To(
                () => TextAlpha,
                v => TextAlpha = v,
                1.0f, 0.25f
            );
        }
        public void OnHoverLeave()
        {
            rectTransform.DOScale(1.0f, 0.25f).SetEase(Ease.OutCubic);
            _textAlphaTween?.Kill();
            _textAlphaTween = DOTween.To(
                () => TextAlpha,
                v => TextAlpha = v,
                0.0f, 0.25f
            ).OnComplete(() => _labelText.gameObject.SetActive(false));
        }
    }
}
