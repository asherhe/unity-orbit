using Orbit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ManeuverLabel : OrbitPOILabel
    {
        private Maneuver _maneuver;
        public Maneuver Maneuver
        {
            get => _maneuver;
            set
            {
                _maneuver = value;
                // maneuver point is guarenteed to be on this orbit
                // it also updates whenever we update the maneuver state
                // perfect for this job
                Orbit = _maneuver.resultOrbit;
            }
        }

        protected override Vector2d GetPosition() => _maneuver.Position;
        protected override string GetLabelText() => "Maneuver";
    }
}