using Orbit;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI
{
    public abstract class POILabel : MapLabel
    {
        private OrbitState _orbit;
        /// <summary>
        /// orbit that is attached to this POILabel
        /// </summary>
        public OrbitState Orbit
        {
            get => _orbit;
            set
            {
                if (_orbit == value) return;
                if (_orbit != null) _orbit.OnStateChanged -= OnOrbitChange;
                _orbit = value;
                _orbit.OnStateChanged += OnOrbitChange;
            }
        }

        private void Start()
        {
            OnOrbitChange();
        }

        /// <summary>
        /// location of this point of interest in body space
        /// </summary>
        protected Vector2d bodyPos;

        /// <summary>
        /// determine the desired position of this POI in body space. called on orbit state update
        /// </summary>
        protected abstract Vector2d GetPosition();
        /// <summary>
        /// determine the text to show in the label. called on orbit state update.
        /// </summary>
        protected virtual string GetLabelText()
        {
            return TextDisplay.AddMetricPrefix(bodyPos.Magnitude - Orbit.body.radius) + "m";
        }

        protected void OnOrbitChange()
        {
            bodyPos = GetPosition();
            labelText.text = GetLabelText();
        }

        private void Update()
        {
            var worldPos = _orbit.body.transform.position + bodyPos;
            rectTransform.anchoredPosition = FollowWorldTransformFromScreen.WorldToCanvas(worldPos);
        }
    }
}