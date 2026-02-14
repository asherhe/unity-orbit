using Orbit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public abstract class PatchPOILabel : MapLabel
    {
        private Patch _patch;
        /// <summary>
        /// patch that is attached to this POILabel
        /// </summary>
        public Patch Patch
        {
            get => _patch;
            set
            {
                if (_patch == value) return;
                if (_patch != null) _patch.OnTransitionUpdate -= OnPatchChanged;
                _patch = value;
                _patch.OnTransitionUpdate += OnPatchChanged;
            }
        }

        private void Start()
        {
            OnPatchChanged();
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
            return TextDisplay.AddMetricPrefix(bodyPos.Magnitude - Patch.patchOrbit.body.radius) + "m";
        }

        protected void OnPatchChanged()
        {
            bodyPos = GetPosition();
            labelText.text = GetLabelText();
        }

        private void Update()
        {
            var worldPos = Patch.patchOrbit.body.transform.position + bodyPos;
            rectTransform.anchoredPosition = FollowWorldTransformFromScreen.WorldToCanvas(worldPos);
        }
    }
}