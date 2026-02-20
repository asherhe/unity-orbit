using Orbit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public abstract class POILabel : MapLabel
    {
        /// <summary>
        /// location of this point of interest in body space
        /// </summary>
        protected Vector2d BodyPos { get; private set; }

        protected abstract CelestialBody Body { get; }

        /// <summary>
        /// determine the desired position of this POI in body space. called on orbit state update
        /// </summary>
        protected abstract Vector2d GetPosition();
        /// <summary>
        /// determine the text to show in the label. called on orbit state update.
        /// </summary>
        protected virtual string GetLabelText()
        {
            return TextDisplay.AddMetricPrefix(BodyPos.Magnitude - Body.radius) + "m";
        }

        private void Start()
        {
            RefreshLabel();
        }

        protected void RefreshLabel()
        {
            BodyPos = GetPosition();
            labelText.text = GetLabelText();
        }

        private void Update()
        {
            var worldPos = Body.transform.position + BodyPos;
            rectTransform.anchoredPosition = FollowWorldTransformFromScreen.WorldToCanvas(worldPos);
        }
    }
}