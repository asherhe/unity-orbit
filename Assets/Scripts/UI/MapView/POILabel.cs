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

        /// <summary>
        /// orbit this POILabel is attached to
        /// </summary>
        protected abstract OrbitState LabelOrbit { get; }
        
        /// <summary>
        /// determine the desired position of this POI in body space. called on orbit state update
        /// </summary>
        protected abstract Vector2d GetPosition();
        /// <summary>
        /// determine the text to show in the label. called on orbit state update.
        /// </summary>
        protected virtual string GetLabelText()
        {
            return TextDisplay.AddMetricPrefix(BodyPos.Magnitude - LabelOrbit.body.radius) + "m";
        }

        public bool IsLabelActive { get => ShowLabel; set => ShowLabel = value; }

        /// <summary>
        /// whether to adjust the alpha based on the zoom level.
        /// true by default, override if you don't want this behaviour
        /// </summary>
        protected virtual bool DoZoomAlpha => true;

        protected override void Awake()
        {
            base.Awake();
            icon.OnClick += () => IsLabelActive = !IsLabelActive;
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
            var worldPos = LabelOrbit.body.transform.position + BodyPos;
            rectTransform.anchoredPosition = FollowWorldTransformFromScreen.WorldToCanvas(worldPos);

            if (DoZoomAlpha)
            {
                // representative size for orbit
                float size;
                if (LabelOrbit.Shape == OrbitShape.Ellipse) size = (float)LabelOrbit.a;
                else size = (float)LabelOrbit.body.soiRadius;
                // size of on-screen icon in world space
                var iconSize = icon.rectTransform.rect.width * IntegerCanvasScale.Instance.Canvas2World;
                // hide label if zoomed out enough to avoid obscuring more important stuff
                Alpha = Mathf.Clamp01((size / iconSize - 0.5f) / 0.5f);
            }
        }

        /// <summary>
        /// match the text and icon color to that of the trajectory corresponding to this orbit
        /// </summary>
        protected void MatchTrajectoryColor()
        {
            var trajColor = TrajectoryManager.Instance.GetTrajectoryOf(LabelOrbit).Color;
            SetColors(trajColor, trajColor);
        }
    }
}