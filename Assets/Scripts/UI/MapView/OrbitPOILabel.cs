using Orbit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public abstract class OrbitPOILabel : POILabel
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
                if (_orbit != null) _orbit.OnStateChanged -= RefreshLabel;
                _orbit = value;
                _orbit.OnStateChanged += RefreshLabel;
                RefreshLabel();
            }
        }

        protected override OrbitState LabelOrbit => Orbit;
    }
}