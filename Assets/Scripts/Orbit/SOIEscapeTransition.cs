using MathNet.Numerics;
using MathNet.Numerics.RootFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// deals with the detection and handling of escape trajectories from the SOI of the current celestial body
    /// </summary>
    public class SOIEscapeTransition : OrbitTransition
    {
        private UniversalPropagator _prop;

        public SOIEscapeTransition(OrbitState orbit) : base(orbit)
        {
            _prop = new UniversalPropagator(orbit);
        }

        /// <summary>
        /// state of orbit at the place where it enters the SOI
        /// </summary>
        public StateVectors? SOICapture { get; private set; }
        /// <summary>
        /// state of orbit at the place where it exits the SOI
        /// </summary>
        public StateVectors? SOIEscape { get; private set; }

        protected override TransitionResult CalcTransitionResult()
        {
            SOICapture = null; SOIEscape = null;

            // check if body HAS an SOI first
            if (orbit.body.orbit == null) return TransitionResult.None;
            // whether we will cross the SOI
            if (orbit.apoapsis < orbit.body.soiRadius) return TransitionResult.None;

            var rsoi = orbit.body.soiRadius;
            var e = orbit.e; var a = orbit.a;

            // determine true anomaly at SOI intersection
            var nu = Math.Acos((Math.Abs(orbit.p) - rsoi) / (rsoi * e));
            var anomaly = orbit.CalcAnomaly(nu);
            var anomaly0 = orbit.Anomaly0;

            if (e < 1)
            {
                // ensure that E < PI (it might wrap around apoapsis)
                if (anomaly > Math.PI) anomaly = 2 * Math.PI - anomaly;
                // normalize to [ -PI, PI ]
                if (anomaly0 > Math.PI) anomaly0 -= 2 * Math.PI;
            }

            // universal anomaly
            var coeff = _prop.AnomCoeff;
            // universal variable (subtracted by initial chi) at SOI intersection
            var chi1 = coeff * (anomaly - anomaly0);
            var chi2 = coeff * (-anomaly - anomaly0);

            // dt from universal anomaly - universal kepler eqn / sqrt(GM)
            var sqrtGM = Math.Sqrt(orbit.GM);
            var t1 = orbit.t0 + _prop.UniversalKepler(chi1) / sqrtGM;
            var t2 = orbit.t0 + _prop.UniversalKepler(chi2) / sqrtGM;
            if (t1 > t2)
            {
                (t1, t2) = (t2, t1);
                (chi1, chi2) = (chi2, chi1);
            }

            SOICapture = _prop.GetStateVectors(t1, chi1);
            SOIEscape = _prop.GetStateVectors(t2, chi2);

            return new TransitionResult(
                SOIEscape.Value, new OrbitState(
                    orbit.body.GetPosition(t2) + SOIEscape?.pos,
                    orbit.body.GetVelocity(t2) + SOIEscape?.vel,
                    t2, orbit.body.parent
                )
            );
        }
    }
}