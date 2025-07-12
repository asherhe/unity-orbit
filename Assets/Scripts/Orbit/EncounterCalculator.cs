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
        private KeplerianPropagator _prop;

        public EncounterCalculator(OrbitState orbit)
        {
            this.orbit = orbit;
            _prop = new(orbit);
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
            public StateVector state;
            /// <summary>
            /// distance at encounter
            /// </summary>
            public double distance;

            /// <summary>
            /// the object that <c>other</c> belongs to, used internally to keep tabs on encounters
            /// </summary>
            public IOrbitingObject orbitingObject;

            public Encounter(OrbitState o, StateVector state, double distance)
            {
                other = o;
                this.state = state;
                this.distance = distance;
            }
        }

        public List<Encounter> GetEncounters(OrbitState o, double t)
        {
            if (orbit.e < 1.0)
                return GetEncounters(o, t, t + orbit.Period);
            else if (orbit.e == 1.0)
                throw new NotImplementedException();
            else
            {
                if (o.e < 1.0)
                {
                    // end time is when x=-o.apoapsis in this hyperbola's perifocal frame
                    // probably involves some eccanom magic to find that time
                    var E = Math.Acosh(o.Apoapsis / orbit.A - orbit.e);
                    var M = _prop.CalcKepler(E);
                    var tEnd = orbit.t0 + Math.Abs((M - orbit.M0) / orbit.MeanMotion); // abs to find the later one
                    if (t > tEnd) return new();
                    else return GetEncounters(o, t, tEnd);
                }
                else
                {
                    // TODO
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// gets a list of all encounters with the given orbit
        /// </summary>
        public List<Encounter> GetEncounters(OrbitState o, double tStart, double tEnd, int brackets = 100)
        {
            if (orbit.body != o.body)
                throw new ArgumentException("Orbit o must share the same body as this EncounterCalculator's orbit.");

            var oprop = new KeplerianPropagator(o);

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
                            new StateVector(t, pos, vel),
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
