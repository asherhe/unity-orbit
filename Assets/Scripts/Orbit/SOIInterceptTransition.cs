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

        protected override TransitionResult CalcTransitionResult()
        {
            nextCapture = null; nextCaptureBody = null;

            EncounterCalculator.Encounter captureEncounter = null;
            foreach (var satellite in orbit.body.satellites)
            {
                var encounters = _enc.GetEncounters(satellite.orbit, Universe.Instance.UT);
                foreach (var e in encounters)
                    if (e.distance < satellite.soiRadius && (captureEncounter == null || e.state.time < captureEncounter.state.time))
                    {
                        captureEncounter = e;
                        captureEncounter.orbitingObject = satellite;
                    }
            }
            if (captureEncounter == null) return TransitionResult.None;

            var b = (CelestialBody)captureEncounter.orbitingObject;
            var t = captureEncounter.state.time;

            // estimated time to traverse the SOI radius
            var soiTrav = b.soiRadius / captureEncounter.state.vel.Magnitude;

            // distance to SOI edge
            double SOIDistance(double t) => (_prop.GetPosition(t) - b.GetPosition(t)).Magnitude - b.soiRadius;

            double captureTime = 0;
            try
            {
                captureTime = Brent.FindRoot(
                    SOIDistance,
                    t - 2 * soiTrav, t,
                    accuracy: 1e-12,
                    maxIterations: 100
                );
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            nextCapture = _prop.GetStateVectors(captureTime);
            var bodyState = b.GetStateVectors(captureTime);
            return new TransitionResult(
                nextCapture.Value, new OrbitState(
                    nextCapture?.pos - bodyState.pos,
                    nextCapture?.vel - bodyState.pos,
                    captureTime, b
                )
            );
        }
    }
}
