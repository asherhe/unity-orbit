using MathNet.Numerics.RootFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using Vertx.Debugging;
using UnityEngine;

namespace Orbit
{
    public class EncounterCalculator
    {
        public OrbitState orbit;
        private UniversalPropagator _prop;

        public EncounterCalculator(OrbitState orbit)
        {
            this.orbit = orbit;
            _prop = new UniversalPropagator(orbit);
        }

        public struct Encounter
        {
            /// <summary>
            /// the current orbit we are on
            /// </summary>
            public OrbitState orbit;
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
            public double Distance { get => (state.pos - otherState.pos).Magnitude; }

            public Encounter(OrbitState orbit, OrbitState other, StateVectors state, StateVectors otherState)
            {
                this.orbit = orbit;
                this.other = other;
                this.state = state;
                this.otherState = otherState;
            }
        }

        public List<Encounter> GetEncounters(OrbitState other, double t)
        {
            //static void setTBounds(OrbitState o, double t, ref double tStart, ref double tEnd)
            //{
            //    if (o.Shape == OrbitShape.Ellipse)
            //    {
            //        tStart = Math.Min(t, tStart);
            //        tEnd = Math.Max(t + o.period, tEnd);
            //    }
            //    else
            //    {
            //        // todo: determine appropriate scaling factor for this
            //        double tWindow = 4.0 * Math.Sqrt(1.0 / (o.GM * Math.Abs(o.alpha * o.alpha * o.alpha)));
            //        tStart = Math.Min(t - tWindow, tStart);
            //        tEnd = Math.Max(t + tWindow, tEnd);
            //    }
            //}

            //double tStart = t, tEnd = t;
            //setTBounds(orbit, t, ref tStart, ref tEnd);
            //setTBounds(o, t, ref tStart, ref tEnd);
            //return GetEncounters(o, tStart, tEnd);

            if (orbit.Shape == OrbitShape.Ellipse)
            {
                return GetEncounters(other, t, t + orbit.period);
            }
            else
            {
                throw new NotImplementedException("only elliptical orbits are supported for automatic bound determination");
            }
        }

        /// <summary>
        /// gets a list of all encounters with the given orbit
        /// </summary>
        public List<Encounter> GetEncounters(OrbitState other, double tStart, double tEnd, int brackets = 64)
        {
            if (orbit.body != other.body)
                throw new ArgumentException("Orbit other must share the same body as this EncounterCalculator's orbit.");

            var oprop = new UniversalPropagator(other);

            // derivative of squared distance
            double DDistance(double t)
            {
                var state = _prop.GetStateVectors(t);
                var ostate = oprop.GetStateVectors(t);

                // the squared distance between two orbits is
                //    r = || p1 - p2 ||^2
                // the derivative of this is
                //   r' = 2 ( p1 - p2 ) * ( v1 - v2 )
                // we remove the coefficient of 2 because we just don't need it
                return Vector2d.Dot(
                    state.pos - ostate.pos,
                    state.vel - ostate.vel
                );
            }

            // find local minima of distance function -> find zeroes of DDistance, then confirm it is local minimum
            var encounters = new List<Encounter>();
            var dt = (tEnd - tStart) / brackets;

            // bracketing times and distances between them
            double[] T = new double[brackets + 1];
            double[] DD = new double[brackets + 1];
            for (int i = 0; i <= brackets; i++)
            {
                T[i] = tStart + i * dt;
                DD[i] = DDistance(T[i]);
            }

            // search for brackets where extrema exist
            for (int i = 0; i < brackets; i++)
            {
                double t0 = T[i], t1 = T[i + 1];
                double dd0 = DD[i], dd1 = DD[i + 1];

                if (!(dd0 < 0 && 0 < dd1)) continue;

                // we've found a local minimum for distance; determine the exact time
                double t = 0.0;
                try
                {
                    var accuracy = Math.Max(Math.Abs(t0), Math.Abs(t1)) * 1e-10;
                    t = Brent.FindRoot(
                        DDistance,
                        t0, t1,
                        accuracy: accuracy,
                        maxIterations: 100
                    );
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                encounters.Add(new Encounter(orbit, other, _prop.GetStateVectors(t), oprop.GetStateVectors(t)));
            }

            return encounters;
        }
    }
}
