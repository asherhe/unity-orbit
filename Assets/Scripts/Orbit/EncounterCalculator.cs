using MathNet.Numerics.RootFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    public class EncounterCalculator
    {
        public OrbitState orbit;
        private UniVarPropagator _prop;

        public EncounterCalculator(OrbitState orbit)
        {
            this.orbit = orbit;
            _prop = new UniVarPropagator(orbit);
        }

        public class Encounter
        {
            /// <summary>
            /// the orbit we have an encounter with
            /// </summary>
            public OrbitState other;
            /// <summary>
            /// state of THIS orbit at the encounter
            /// </summary>
            public StateVectors state;
            /// <summary>
            /// state of OTHER orbit at the encounter
            /// </summary>
            public StateVectors otherState;
            /// <summary>
            /// distance at encounter
            /// </summary>
            public double distance;

            /// <summary>
            /// the object that <c>other</c> belongs to, used internally to keep tabs on encounters
            /// </summary>
            public IOrbitingObject orbitingObject;

            public Encounter(OrbitState o, StateVectors state, StateVectors otherState, double distance)
            {
                other = o;
                this.state = state;
                this.otherState = otherState;
                this.distance = distance;
            }
        }

        public List<Encounter> GetEncounters(OrbitState o, double t)
        {
            static void setTBounds(OrbitState o, double t, ref double tStart, ref double tEnd)
            {
                if (o.e < 1.0)
                {
                    tStart = Math.Min(t, tStart);
                    tEnd = Math.Max(t + o.period, tEnd);
                }
                else
                {
                    // todo: determine appropriate scaling factor for this
                    double tWindow = 4.0 * Math.Sqrt(1.0 / (o.GM * Math.Abs(o.alpha * o.alpha * o.alpha)));
                    tStart = Math.Min(t - tWindow, tStart);
                    tEnd = Math.Max(t + tWindow, tEnd);
                }
            }

            double tStart = t, tEnd = t;
            setTBounds(orbit, t, ref tStart, ref tEnd);
            setTBounds(o, t, ref tStart, ref tEnd);

            return GetEncounters(o, tStart, tEnd);
        }

        /// <summary>
        /// gets a list of all encounters with the given orbit
        /// </summary>
        public List<Encounter> GetEncounters(OrbitState o, double tStart, double tEnd, int brackets = 100)
        {
            if (orbit.body != o.body)
                throw new ArgumentException("Orbit o must share the same body as this EncounterCalculator's orbit.");

            var oprop = new UniVarPropagator(o);

            // derivative of distance
            double DDistance(double t) => 2.0 * Vector2d.Dot(
                _prop.GetPosition(t) - oprop.GetPosition(t),
                _prop.GetVelocity(t) - oprop.GetVelocity(t)
            );

            // find local minima of distance function -> find zeroes of DDistance, then confirm it is local minimum

            var encounters = new List<Encounter>();
            var dt = (tEnd - tStart) / brackets;

            // search for brackets where extrema exist
            double t0 = tStart, d0 = DDistance(t0);
            for (int i = 0; i < brackets; i++)
            {
                var t1 = t0 + dt;
                var d1 = DDistance(t1);

                // inflection point found
                if (d0 * d1 <= 0.0)
                {
                    double t = 0.0;
                    try
                    {
                        t = Brent.FindRoot(
                            DDistance,
                            t0, t1,
                            accuracy: 1e-12,
                            maxIterations: 100
                        );
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }

                    // check if local minimum
                    const double STEP = 1e-6;
                    if ((DDistance(t + 0.5 * STEP) - DDistance(t - 0.5 * STEP)) / STEP > 0.0)
                    {
                        Vector2d pos = _prop.GetPosition(t), vel = _prop.GetVelocity(t),
                             opos = oprop.GetPosition(t), ovel = oprop.GetVelocity(t);
                        encounters.Add(new Encounter(
                            o,
                            new StateVectors(t, pos, vel),
                            new StateVectors(t, opos, ovel),
                            (pos - opos).Magnitude
                        ));
                    }
                }

                t0 = t1;
                d0 = d1;
            }

            return encounters;
        }

    }
}
