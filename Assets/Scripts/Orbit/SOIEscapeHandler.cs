using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// deals with the detection and handling of escape trajectories from the SOI of the current celestial body
    /// </summary>
    public class SOIEscapeHndler
    {
        public OrbitState orbit;
        private UniVarPropagator _prop;

        public SOIEscapeHndler(OrbitState orbit)
        {
            this.orbit = orbit;
            _prop = new UniVarPropagator(orbit);
        }


        /// <summary>
        /// state of orbit at the place where it enters the SOI
        /// </summary>
        public StateVectors soiCapture;
        /// <summary>
        /// state of orbit at the place where it exits the SOI
        /// </summary>
        public StateVectors soiEscape;

        /// <summary>
        /// calculate the state vectors (time, position, velocity) of this orbit at the moments when this orbit enters or leaves the SOI.
        /// </summary>
        public void CheckSOITimes()
        {
            /*
            soiCapture = null; soiEscape = null;

            // check if body HAS an SOI first
            if (orbit.body.orbit == null) return;

            if (orbit.e == 1.0) throw new NotImplementedException(); // TODO: this is for non-parabolic orbits only

            // calculate eccentric anomaly at SOI radius (plus or minus)
            var E = (orbit.a - orbit.body.soiRadius) / (orbit.a * orbit.e);
            if (orbit.e < 1.0) E = Math.Acos(E);
            else E = Math.Acosh(E);
            // no SOI intersection
            if (E == double.NaN) return;

            // the two intersection points with the SOI - one with positive (p) and one with negative (n) eccentric anomaly
            // the next order of business is to determine the time they intersect
            var Mp = _prop.CalcKepler(E);
            var Mn = _prop.CalcKepler(-E);

            // change Mp and Mn to be on the same orbital period as the current mean anomaly
            // not for hyperbolic orbits tho because those guys don't do periodic orbits
            if (orbit.e < 1.0)
            {
                var M = _prop.GetMeanAnomaly(Universe.Instance.UT);
                // whichever periapsis we are closest to
                var Mperi = 2 * Math.PI * Math.Round(M / (2 * Math.PI));
                Mp += Mperi; Mn += Mperi;
            }

            // calculate time to SOI
            double tp, tn;
            tp = orbit.t0 + (Mp - orbit.M0) / orbit.MeanMotion;
            tn = orbit.t0 + (Mn - orbit.M0) / orbit.MeanMotion;

            // assign SOI state vectors, ensure chronological order
            StateVectors statep, staten;
            statep = new(tp, _prop.GetPosition(tp), _prop.GetVelocity(tp));
            staten = new(tn, _prop.GetPosition(tn), _prop.GetVelocity(tn));
            if (tp > tn)
            {
                soiCapture = staten; soiEscape = statep;
            }
            else
            {
                soiCapture = statep; soiEscape = staten;
            }
            */
        }

        public void EscapeSOI()
        {
            var t = soiEscape.time;
            orbit.UpdateFromStateVectors(
                orbit.body.GetPosition(t) + soiEscape.pos,
                orbit.body.GetVelocity(t) + soiEscape.vel,
                t, orbit.body.parent
            );
        }
    }
}