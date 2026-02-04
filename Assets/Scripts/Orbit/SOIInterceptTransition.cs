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
    public class SOIInterceptTransition : OrbitTransition
    {
        private UniversalPropagator _prop;
        private EncounterCalculator _enc;

        public SOIInterceptTransition(OrbitState orbit) : base(orbit)
        {
            _prop = new UniversalPropagator(orbit);
            _enc = new EncounterCalculator(orbit);
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
            double expiry = UT + orbit.period; // TODO: integrate with dynamic bound readjustment
            foreach (var satellite in orbit.body.satellites)
            {
                // TODO: time bounds only work for elliptical so far
                var encounters = _enc.GetEncounters(satellite.orbit, UT, UT + orbit.period);
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

            var body = (CelestialBody)captureEncounter.other.Owner;
            var t = captureEncounter.state.time;

            // estimated time to traverse the SOI radius, doubled for extra wiggle room
            var soiTrav = 2 * body.soiRadius / (captureEncounter.state.vel - captureEncounter.otherState.vel).Magnitude;

            // distance to SOI edge
            double SOIDistance(double t) => (_prop.GetPosition(t) - body.GetPosition(t)).Magnitude - body.soiRadius;

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
            var bodyState = body.GetStateVectors(captureTime);
            return new TransitionResult(
                nextCapture.Value, new OrbitState(
                    nextCapture?.pos - bodyState.pos,
                    nextCapture?.vel - bodyState.vel,
                    captureTime, body
                )
            );
        }
    }
}
