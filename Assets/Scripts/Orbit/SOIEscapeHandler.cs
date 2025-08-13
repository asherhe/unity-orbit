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
            soiCapture = null; soiEscape = null;

            // check if body HAS an SOI first
            if (orbit.body.orbit == null) return;

            if (orbit.apoapsis < orbit.body.soiRadius) return;

            var rsoi = orbit.body.soiRadius;
            var e = orbit.e; var a = orbit.a;

            // determine true anomaly at SOI intersection
            var nu = Math.Acos(((e == 1.0 ? orbit.h / orbit.GM : a * (1 - e * e)) / rsoi - 1) / e);
            var anomaly = orbit.CalcAnomaly(nu);

            // prepare to calculate universal anomaly
            double coeff = 0, anomaly0 = 0;
            if (e < 1)
            {
                coeff = Math.Sqrt(a);
                anomaly0 = orbit.E0;
            }
            else if (e > 1)
            {
                coeff = Math.Sqrt(-a);
                anomaly0 = orbit.F0;
            }
            else if (e == 1)
            {
                coeff = orbit.h / Math.Sqrt(orbit.GM);
                anomaly = Math.Tan(0.5 * nu);
                anomaly0 = Math.Tan(0.5 * orbit.nu0);
            }

            var chi1 = coeff * (anomaly - anomaly0);
            var chi2 = coeff * (-anomaly - anomaly0);

            // determine dt from universal anomaly via universal kepler's equation
            double dtFromChi(double chi)
            {
                double r0 = orbit.r0, vr0 = orbit.vr0, alpha = orbit.alpha;
                var z = alpha * chi * chi;
                var sqrtGM = Math.Sqrt(orbit.GM);
                return r0 * vr0 * chi * chi * _prop.stumpff_C(z) / orbit.GM +
                    (1 - alpha * r0) * chi * chi * chi * _prop.stumpff_S(z) / sqrtGM +
                    r0 * chi / sqrtGM;
            }

            var t1 = orbit.t0 + dtFromChi(chi1);
            var t2 = orbit.t0 + dtFromChi(chi2);
            if (t1 > t2) (t1, t2) = (t2, t1);

            StateVectors stateFromChi (double chi, double t)
            {
                var z = orbit.alpha * chi * chi;
                var dt = t - orbit.t0;
                var C = _prop.stumpff_C(z);
                var S = _prop.stumpff_S(z);

                var p = _prop.GetPosition(dt, chi, C, S);
                var v = _prop.GetVelocity(chi, z, C, S, p.Magnitude);
                return new StateVectors(t, p, v);
            }

            soiCapture = stateFromChi(chi1, t1);
            soiEscape = stateFromChi(chi2, t2);
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