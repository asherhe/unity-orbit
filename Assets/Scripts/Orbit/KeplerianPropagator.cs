using MathNet.Numerics.RootFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    public class KeplerianPropagator
    {
        /// <summary>
        /// constructs a new keplerian propagator from orbital elements
        /// </summary>
        /// <param name="GM">standard gravitational parameter, m^3/s^2</param>
        /// <param name="h">angular momentum, m^2/s</param>
        /// <param name="e">eccentricity</param>
        /// <param name="omega">longitude of periapsis, in radians</param>
        /// <param name="M0">mean anomaly at epoch, in radians</param>
        /// <param name="t0">epoch time, in seconds</param>
        public KeplerianPropagator(
            double GM,
            double h,
            double e,
            double omega,
            double M0,
            double t0
        )
        {
            this.GM = GM;
            this.h = h;
            this.e = e;
            this.omega = omega;
            this.M0 = M0;
            this.t0 = t0;

            a = h * h / (GM * (1 - e * e));
            betaSquared = Math.Abs(1 - e * e); // 1-e^2 for elliptical, e^2-1 for hyperbolic
            if (e == 1)
                meanMotion = GM * GM / (h * h * h);
            else
                meanMotion = Math.Sqrt(GM / Math.Abs(a * a * a));
        }

        public double GM;
        public double h;
        public double e;
        public double omega;
        public double M0;
        public double t0;

        // derived values
        public double a;
        public double betaSquared;
        public double meanMotion;

        /// <summary>
        /// converts a vector from body space to perifocal space
        /// </summary>
        public Vector2d BodyToPerifocal(Vector2d vbody)
        {
            var perifocal = vbody.Rotate(-omega);
            if (h < 0) perifocal.y = -perifocal.y;
            return perifocal;
        }
        /// <summary>
        /// converts a vector from body space to perifocal space
        /// </summary>
        public Vector2d PerifocalToBody(Vector2d vperifocal)
        {
            var body = new Vector2d(vperifocal);
            if (h < 0) body.y = -body.y;
            body = body.Rotate(omega);
            return body;
        }

        /// <summary>
        /// get mean anomaly at a given time
        /// </summary>
        public double GetMeanAnomaly(double t) => M0 + (t - t0) * meanMotion;

        /// <summary>
        /// calculates the value of the RHS of kepler's equation.
        /// used to solve for eccentric anomaly.
        /// </summary>
        /// <param name="E">eccentric anomaly</param>
        public double CalcKepler(double E)
        {
            if (e < 1) return E - e * Math.Sin(E);
            else if (e > 1) return e * Math.Sinh(E) - E;
            else return 0; // kepler's equation doesn't apply to parabolic orbits
        }
        /// <summary>
        /// the derivative of CalcKepler
        /// </summary>
        /// <param name="E">eccentric anomaly</param>
        public double CalcDKepler(double E)
        {
            if (e < 1) return 1 - e * Math.Cos(E);
            else if (e > 1) return e * Math.Cosh(E) - 1;
            else return 0; // kepler's equation doesn't apply to parabolic orbits
        }

        /// <summary>
        /// get eccentric anomaly at a given time
        /// </summary>
        public double GetEccentricAnomaly(double t) => GetEccentricAnomalyFromMeanAnomaly(GetMeanAnomaly(t));
        public double GetEccentricAnomalyFromMeanAnomaly(double M)
        {
            if (e == 1.0) throw new InvalidOperationException("Cannot calculate the eccentric anomaly of a parabolic orbit.");
            if (M == 0.0) return 0.0;
            if (e == 0.0) return M;

            /*
             * find eccentric anomaly by solving for E in kepler's equation
             * 
             *   e<1:  M = E - e sin E
             *   e>1:  M = e sinh E - E
             */

            double left, right;
            if (e < 1)
            {
                left = 0;
                right = 2 * Math.PI;
                // normalize to [0, 2PI]
                M = MathUtils.Mod(M, 2 * Math.PI);
            }
            else
            {
                left = Math.Min(0, M - 1);
                right = Math.Max(0, M + 1);
            }

            double E = 0.0;
            try
            {
                E = RobustNewtonRaphson.FindRoot(
                    E => CalcKepler(E) - M,
                    CalcDKepler,
                    left, right,
                    accuracy: 1e-12,
                    maxIterations: 90,
                    subdivision: 10
                );
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return E;
        }

        public double GetTrueAnomaly(double t) => GetTrueAnomalyFromMeanAnomaly(GetMeanAnomaly(t));
        public double GetTrueAnomalyFromMeanAnomaly(double M)
        {
            if (e < 1)
            {
                throw new NotImplementedException();
            }
            else if (e > 1)
            {
                throw new NotImplementedException();
            }
            else
            {
                var z = Math.Cbrt(3 * M + Math.Sqrt(1 + 9 * M * M));
                var nu = 2 * Math.Atan(z - 1 / z);
                return nu;
            }
        }

        /// <summary>
        /// get the position of this orbit from some anomaly, depending on the orbit type
        /// </summary>
        /// <param name="anomaly">eccentric anomaly when e<1, true anomaly when e=0, and hyperbolic eccentric anomaly when e>1</param>
        /// <returns>position vector</returns>
        private Vector2d GetPositionFromAnomaly(double t, double anomaly)
        {
            Vector2d pos;
            if (e < 1)
            {
                pos = new(
                    a * (Math.Cos(anomaly) - e),
                    a * Math.Sqrt(betaSquared) * Math.Sin(anomaly)
                );
            }
            else if (e > 1)
            {
                pos = new(
                    -a * (e - Math.Cosh(anomaly)),
                    -a * Math.Sqrt(betaSquared) * Math.Sinh(anomaly)
                );
            }
            else
            {
                var r = h * h / (GM * (1 + Math.Cos(anomaly)));
                pos = new(
                    r * Math.Cos(anomaly),
                    r * Math.Sin(anomaly)
                );
            }

            return PerifocalToBody(pos);
        }
        /// <summary>
        /// get the velocity of this orbit from some anomaly, depending on the orbit type
        /// </summary>
        /// <param name="anomaly">eccentric anomaly when e<1, true anomaly when e=0, and hyperbolic eccentric anomaly when e>1</param>
        /// <returns>position vector</returns>
        private Vector2d GetVelocityFromAnomaly(double t, double anomaly)
        {
            Vector2d vel;
            if (e < 1)
            {
                var r = a * (1 - e * Math.Cos(anomaly));
                vel = new(
                    -Math.Sin(anomaly),
                    Math.Sqrt(betaSquared) * Math.Cos(anomaly)
                );
                vel *= Math.Sqrt(GM * a) / r;
            }
            else if (e > 1)
            {
                var r = -a * (e * Math.Cosh(anomaly) - 1);
                vel = new(
                    -Math.Sinh(anomaly),
                    Math.Sqrt(betaSquared) * Math.Cosh(anomaly)
                );
                vel *= Math.Sqrt(GM * -a) / r;
            }
            else
            {
                vel = new(
                    -Math.Sin(anomaly),
                    1 + Math.Cos(anomaly)
                );
                vel *= GM / h;
            }
            return PerifocalToBody(vel);
        }
        /// <summary>
        /// get the relevant anomaly for orbital propagation (E for ellipses, nu for parabolas, and F for hyperbolas)
        /// </summary>
        private double GetAnomaly(double t)
        {
            if (e < 1) return GetEccentricAnomaly(t);
            else if (e > 1) return GetEccentricAnomaly(t);
            else return GetTrueAnomaly(t);
        }

        /// <summary>
        /// get the position (in world space) of the orbit at a given time
        /// </summary>
        /// <returns>position of orbit, in meters</returns>
        public Vector2d GetPosition(double t) => GetPositionFromAnomaly(t, GetAnomaly(t));

        /// <summary>
        /// get the velocity (in world space) of the orbit at a given time
        /// </summary>
        /// <returns>velocity of orbit, in meters</returns>
        public Vector2d GetVelocity(double t) => GetVelocityFromAnomaly(t, GetAnomaly(t));

        /// <summary>
        /// get the state vectors at a given time.
        /// </summary>
        /// <remarks>
        /// if you need to get both position and velocity, prefer this over calling GetPosition and GetVelocity
        /// separately because the two methods will have redundant calculations.
        /// </remarks>
        public StateVectors GetStateVectors(double t)
        {
            var anomaly = GetAnomaly(t);
            return new(
                t,
                GetPositionFromAnomaly(t, GetAnomaly(t)),
                GetVelocityFromAnomaly(t, GetAnomaly(t))
            );
        }
    }
}