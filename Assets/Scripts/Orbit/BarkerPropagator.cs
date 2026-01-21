using MathNet.Numerics.RootFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    // https://orbital-mechanics.space/time-since-periapsis-and-keplers-equation/parabolic-trajectories.html

    /// <summary>
    /// orbital propagation for parabolic orbits using barker's equation
    /// </summary>
    public class BarkerPropagator : IOrbitPropagator
    {
        /// <summary>
        /// constructs a new barker propagator from orbital elements
        /// </summary>
        /// <param name="GM">standard gravitational parameter, m^3/s^2</param>
        /// <param name="h">angular momentum, m^2/s</param>
        /// <param name="omega">longitude of periapsis, in radians</param>
        /// <param name="M0">mean anomaly at epoch, in radians</param>
        /// <param name="t0">epoch time, in seconds</param>
        public BarkerPropagator(
            double GM,
            double h,
            double omega,
            double M0,
            double t0
        )
        {
            this.GM = GM;
            this.h = h;
            this.omega = omega;
            this.M0 = M0;
            this.t0 = t0;

            p = h * h / GM;
            meanMotion = GM * GM / (h * h * h);
        }

        public double GM;
        public double h;
        public double omega;
        public double M0;
        public double t0;

        // derived values
        public double p;
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

        public double GetTrueAnomaly(double t)
        {
            var M = GetMeanAnomaly(t);
            var z = Math.Cbrt(3 * M + Math.Sqrt(1 + 9 * M * M));
            var D = z - 1 / z;
            var nu = 2 * Math.Atan(D);
            return nu;
        }

        public Vector2d GetPositionFromTrueAnomaly(double nu)
        {
            var r = p / (1 + Math.Cos(nu));
            var pos = r * new Vector2d(Math.Cos(nu), Math.Sin(nu));
            return PerifocalToBody(pos);
        }
        public Vector2d GetVelocityFromTrueAnomaly(double nu)
        {
            var r = p / (1 + Math.Cos(nu));
            var vtrv = Math.Abs(h) / r; // transverse velocity
            var vrad = GM * Math.Sin(nu) / Math.Abs(h); // radial velocity
            var vel = new Vector2d(vrad, vtrv);
            vel = vel.Rotate(nu);
            return PerifocalToBody(vel);
        }

        /// <summary>
        /// get the position (in world space) of the orbit at a given time
        /// </summary>
        /// <returns>position of orbit, in meters</returns>
        public Vector2d GetPosition(double t) => GetPositionFromTrueAnomaly(GetTrueAnomaly(t));

        /// <summary>
        /// get the velocity (in world space) of the orbit at a given time
        /// </summary>
        /// <returns>velocity of orbit, in meters</returns>
        public Vector2d GetVelocity(double t) => GetVelocityFromTrueAnomaly(GetTrueAnomaly(t));

        /// <summary>
        /// get the state vectors at a given time.
        /// </summary>
        /// <remarks>
        /// if you need to get both position and velocity, prefer this over calling GetPosition and GetVelocity
        /// separately because the two methods will have redundant calculations.
        /// </remarks>
        public StateVectors GetStateVectors(double t)
        {
            var nu = GetTrueAnomaly(t);
            return new(
                t,
                GetPositionFromTrueAnomaly(nu),
                GetVelocityFromTrueAnomaly(nu)
            );
        }
    }
}