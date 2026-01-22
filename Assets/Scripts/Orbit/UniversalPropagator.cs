using MathNet.Numerics.RootFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// orbital propagation using the universal variable formulation
    /// </summary>
    public class UniversalPropagator : IOrbitPropagator
    {
        // https://orbital-mechanics.space/time-since-periapsis-and-keplers-equation/universal-variables.html

        public OrbitState orbit;
        public UniversalPropagator(OrbitState orbit)
        {
            this.orbit = orbit;
        }

        private double GM { get => orbit.GM; }
        private double t0 { get => orbit.t0; }
        private Vector2d p0 { get => orbit.p0; }
        private Vector2d v0 { get => orbit.v0; }
        private double r0 { get => orbit.r0; }
        private double vr0 { get => orbit.vr0; }
        private double alpha { get => orbit.alpha; }

        // cutoff for z in the stumpff functions when we decide to fallback to the z=0 case
        // any lower than this and it seems that the floating-point error from the z in the denominator starts to be bad for performance
        private const double Z_TOL = 1e-7;

        public double stumpff_C(double z)
        {
            var sqrtz = Math.Sqrt(Math.Abs(z));
            if (z > Z_TOL) return (1 - Math.Cos(sqrtz)) / z;
            else if (z < -Z_TOL) return (Math.Cosh(sqrtz) - 1) / -z;
            else return 0.5;
        }
        public double stumpff_S(double z)
        {
            var sqrtz = Math.Sqrt(Math.Abs(z));
            if (z > Z_TOL) return (sqrtz - Math.Sin(sqrtz)) / (z * sqrtz);
            else if (z < -Z_TOL) return (Math.Sinh(sqrtz) - sqrtz) / (-z * sqrtz);
            else return 1.0 / 6.0;
        }

        /// <summary>
        /// universal kepler's equation, RHS only
        /// </summary>
        /// <param name="chi">universal anomaly</param>
        public double UniversalKepler(double chi)
        {
            var z = alpha * chi * chi;
            return
                r0 * vr0 * chi * chi * stumpff_C(z) / Math.Sqrt(GM) +
                (1 - alpha * r0) * chi * chi * chi * stumpff_S(z) +
                r0 * chi;
        }

        /// <summary>
        /// derivative of UniversalKepler with respect to chi
        /// </summary>
        /// <param name="chi">universal anomaly</param>
        public double DUniversalKepler(double chi)
        {
            var z = alpha * chi * chi;
            return
                r0 * vr0 * chi * (1 - z * stumpff_S(z)) / Math.Sqrt(GM) +
                (1 - alpha * r0) * chi * chi * stumpff_C(z) + r0;
        }

        /// <summary>
        /// get the value of the universal anomaly chi at a given time
        /// </summary>
        public double GetChi(double dt)
        {
            var dtsqrtGM = dt * Math.Sqrt(GM);
            var chi0 = dtsqrtGM * Math.Abs(alpha);
            var lower = dtsqrtGM / orbit.apoapsis;
            var upper = dtsqrtGM / orbit.periapsis;
            // scale with kepler's equation to actually allow convergence to happen
            // if we use a fixed tolerance then the root finder will often "fail" to converge
            // because the desired tolerance is much more precise than what can be represented,
            // especially with precision loss through mathematical operations
            var accuracy = Math.Abs(DUniversalKepler(chi0)) * 1e-8;

            //var chi = NewtonRaphson.FindRootNearGuess(
            //    chi => UniversalKepler(chi) - dtsqrtGM,
            //    DUniversalKepler,
            //    chi0, lower - 1, upper + 1,
            //    accuracy: accuracy,
            //    maxIterations: 100
            //);

            var chi = RobustNewtonRaphson.FindRoot(
                chi => UniversalKepler(chi) - dtsqrtGM,
                DUniversalKepler,
                lower - 1, upper + 1,
                accuracy: accuracy,
                maxIterations: 100,
                subdivision: 20
            );


            return chi;
        }

        public Vector2d GetPosition(double dt, double chi, double C, double S)
        {
            return
                p0 * (1 - chi * chi * C / r0) +
                v0 * (dt - chi * chi * chi * S / Math.Sqrt(GM));
        }
        public Vector2d GetVelocity(double chi, double z, double C, double S, double r)
        {
            return
                p0 * chi * Math.Sqrt(GM) * (z * S - 1) / (r * r0) +
                v0 * (1 - chi * chi * C / r);
        }
        public Vector2d GetPosition(double t)
        {
            var dt = t - t0;
            if (orbit.Shape == OrbitShape.Ellipse)
                dt = MathUtils.Mod(dt, orbit.period);

            var chi = GetChi(dt);
            var z = alpha * chi * chi;
            return GetPosition(dt, chi, stumpff_C(z), stumpff_S(z));
        }
        public Vector2d GetVelocity(double t)
        {
            var dt = t - t0;
            if (orbit.Shape == OrbitShape.Ellipse)
                dt = MathUtils.Mod(dt, orbit.period);

            var chi = GetChi(dt);
            var z = alpha * chi * chi;
            var C = stumpff_C(z);
            var S = stumpff_S(z);
            var r = GetPosition(dt, chi, C, S).Magnitude;
            return GetVelocity(chi, z, C, S, r);
        }

        public StateVectors GetStateVectors(double dt, double chi)
        {
            var z = alpha * chi * chi;
            var C = stumpff_C(z);
            var S = stumpff_S(z);

            var p = GetPosition(dt, chi, C, S);
            var v = GetVelocity(dt, chi, C, S, p.Magnitude);

            return new(t0 + dt, p, v);
        }
        public StateVectors GetStateVectors(double t)
        {
            var dt = t - t0;
            if (orbit.Shape == OrbitShape.Ellipse)
                dt = MathUtils.Mod(dt, orbit.period);

            var chi = GetChi(dt);

            var state = GetStateVectors(dt, chi);
            return new(t, state.pos, state.vel); // since we normalized dt to the first period for ellipses
        }

        /// <summary>
        /// get the coefficient that makes the universal anomaly when multiplied with the relevant anomaly
        /// </summary>
        public double AnomCoeff
        {
            get
            {
                switch (orbit.Shape)
                {
                    case OrbitShape.Ellipse:
                        return Math.Sqrt(orbit.a); 
                    case OrbitShape.Parabola:
                        return Math.Sqrt(orbit.p);
                    case OrbitShape.Hyperbola:
                        return Math.Sqrt(-orbit.a);
                    default:
                        throw new NotImplementedException("Orbit shape must be elliptical, parabolic, or hyperbolic");
                }
            }
        }
    }
}