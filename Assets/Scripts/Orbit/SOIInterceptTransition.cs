using MathNet.Numerics.RootFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// deals with the detection and handling of intercepts with celestial bodies
    /// </summary>
    public class SOIInterceptTransition : OrbitTransitionHandler
    {
        private UniversalPropagator _prop;
        private EncounterCalculator _enc;
        private SOIEscapeTransition _esc;

        /// <summary>
        /// initialize a SOIInterceptTransition for a given orbit
        /// </summary>
        /// <param name="orbit">orbit for which to check for intercepts</param>
        /// <param name="esc">
        /// SOIEscapeTransition that is also attached to orbit. it is assumed that the results of this transition are checked before this object's CheckTransitions()
        /// </param>
        public SOIInterceptTransition(OrbitState orbit, SOIEscapeTransition esc) : base(orbit)
        {
            _prop = new UniversalPropagator(orbit);
            _enc = new EncounterCalculator(orbit);
            _esc = esc;
        }

        /// <summary>
        /// the state of this orbit at the time of the next capture
        /// </summary>
        public StateVectors? nextCapture { get; private set; }
        /// <summary>
        /// the celestial body that will capture this orbit
        /// </summary>
        public CelestialBody nextCaptureBody { get; private set; }

        protected override TransitionResult CalcTransitionResult(double UT)
        {
            nextCapture = null; nextCaptureBody = null;

            EncounterCalculator.Encounter? earliestEnc = null;
            double expiry = double.PositiveInfinity;
            foreach (var satellite in orbit.body.satellites)
            {
                if (orbit.apoapsis < satellite.orbit.periapsis - satellite.soiRadius ||
                    orbit.periapsis > satellite.orbit.apoapsis + satellite.soiRadius)
                    continue;

                double tStart = UT, tEnd = UT + 1e8;
                if (orbit.Shape == OrbitShape.Ellipse) tEnd = UT + orbit.period;
                if (_esc.HasTransition)
                {
                    tStart = Math.Max(tStart, (double)_esc.SOICapture?.time);
                    tEnd = Math.Min(tEnd, (double)_esc.SOIEscape?.time);
                }
                expiry = Math.Min(expiry, tEnd);
                var encounters = _enc.GetEncounters(satellite.orbit, tStart, tEnd);
                Debug.DrawLine(
                    _prop.GetPosition(tStart) + CameraFocus.Instance.GetRelativePosition(orbit.body),
                    _prop.GetPosition(tEnd) + CameraFocus.Instance.GetRelativePosition(orbit.body)
                );
                foreach (var e in encounters)
                {
                    if (e.Distance < satellite.soiRadius && (!earliestEnc.HasValue || e.state.time < earliestEnc?.state.time))
                    {
                        earliestEnc = e;
                    }
                }
            }
            if (!earliestEnc.HasValue) return TransitionResult.ExpiresAt(expiry);
            var captureEncounter = earliestEnc.Value;

            nextCaptureBody = (CelestialBody)captureEncounter.other.Owner;
            var t = captureEncounter.state.time;

            // estimated time to traverse the SOI radius, doubled for extra wiggle room
            var soiTrav = 2 * nextCaptureBody.soiRadius / (captureEncounter.state.vel - captureEncounter.otherState.vel).Magnitude;

            // distance to SOI edge
            double SOIDistance(double t) => (_prop.GetPosition(t) - nextCaptureBody.GetPosition(t)).Magnitude - nextCaptureBody.soiRadius;

            double captureTime = 0;
            try
            {
                captureTime = Brent.FindRoot(
                    SOIDistance,
                    t - soiTrav, t,
                    accuracy: Math.Abs(t) * 1e-10,
                    maxIterations: 100
                );
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            nextCapture = _prop.GetStateVectors(captureTime);
            var bodyState = nextCaptureBody.GetStateVectors(captureTime);
            return new TransitionResult(
                nextCapture.Value, new OrbitState(
                    nextCapture?.pos - bodyState.pos,
                    nextCapture?.vel - bodyState.vel,
                    captureTime, nextCaptureBody
                )
            );
        }
    }
}
