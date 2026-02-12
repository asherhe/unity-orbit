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
        /// <summary>
        /// Owner as a CelestialBody
        /// </summary>
        private CelestialBody _body;

        protected override void Awake()
        {
            // run this before base.awake so that colors are updated before UpdateVisuals()
            OnOwnerUpdated += () =>
            {
                _body = (CelestialBody)Owner;
                labelText.text = _body.bodyName;
                SetColors(_body.color, _body.color);
            };
            base.Awake();
        }

        private void Update()
        {
            if (_body.orbit == null) return;
            var r = (float)_body.radius;
            var a = (float)_body.orbit.a;
            // size of on-screen icon in world space
            var iconSize = iconImage.rectTransform.rect.width * IntegerCanvasScale.Instance.Canvas2World;

            // alpha for hiding label when we zoom in close enough
            var hideSOI = (iconSize / r - 2.0f) / 2.0f;
            // alpha for hiding label to avoid interfering with parent
            var hideParent = (0.5f - iconSize / a) / 0.5f;

            Alpha = Mathf.Clamp01(Mathf.Min(hideSOI, hideParent));
        }

        protected override void UpdateVisuals()
        {
            base.UpdateVisuals();
        }
    }
}
