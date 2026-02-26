using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class CelestialBodyLabel : ObjectLabel
    {
        /// <summary>
        /// Owner as a CelestialBody
        /// </summary>
        private CelestialBody _body;

        public override string Name => _body.bodyName;

        protected override void Awake()
        {
            // run this before base.Awake so that colors are updated before UpdateVisuals()
            OnOwnerUpdated += () =>
            {
                _body = (CelestialBody)Owner;
                SetColors(_body.color, _body.color);
            };
            base.Awake();
        }

        private void Update()
        {
            if (_body == null) return;
            if (_body.orbit == null) return;
            var r = (float)_body.radius;
            var a = (float)_body.orbit.a;
            // size of on-screen icon in world space
            var iconSize = icon.rectTransform.rect.width * IntegerCanvasScale.Instance.Canvas2World;

            // alpha for hiding label when we zoom in close enough
            var hideRadius = (iconSize / r - 4.0f) / 2.0f;
            // alpha for hiding label to avoid interfering with parent
            var hideParent = (a / iconSize - 1.0f) / 0.5f;

            Alpha = Mathf.Clamp01(Mathf.Min(hideRadius, hideParent));
        }
    }
}
