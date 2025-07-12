using MathNet.Numerics.RootFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace Orbit
{
    /// <summary>
    /// represents a particular orbit around a <c>CelestialBody</c>
    /// </summary>
    public class OrbitState
    {
        /// <summary>
        /// the celestial body this orbit goes around
        /// </summary>
        public CelestialBody body { get; private set; }
        /// <summary>
        /// standard gravitational parameter
        /// </summary>
        public double GM { get => body.GM; }

        public double t0 { get; private set; }
        public Vector2d p0 { get; private set; }
        public Vector2d v0 { get; private set; }

        /// <summary>
        /// invoked when any of the orbital parameters are modified
        /// </summary>
        public event Action OnStateChanged;


        /* derived orbital parameters */

        /// <summary>
        /// specific angular momentum around the parent body.
        /// may occasionally be referred to as just "angular momentum".
        /// </summary>
        /// <remarks>
        /// the sign of the angular momentum obeys the right hand rule (positive is a counterclockwise orbit).
        /// (units <c>m^2 / s</c>)
        /// </remarks>
        public double h { get; private set; }

        /// <summary>
        /// eccentricity
        /// </summary>
        /// <remarks>
        /// a circular orbit has zero eccentricity.
        /// an elliptical orbit has eccentricity between 0 and 1.
        /// a parabolic orbit has eccentricity of 1
        /// a hyperbolic orbit has eccentricity above 1
        /// </remarks>
        public double e { get; private set; }

        /// <summary>
        /// longitude of periapsis
        /// </summary>
        /// <remarks>
        /// the longitude of periapsis indicates the angle this orbit is pointing in.
        /// to be more specific, it describes the angle between the +x axis to the periapsis of the orbit
        /// (angle is expressed in radians)
        /// </remarks>
        public double omega { get; private set; }

        /// <summary>
        /// mean anomaly at epoch
        /// </summary>
        public double M0 { get; private set; }

        /// <summary>
        /// the semimajor axis (in meters) of the orbit
        /// </summary>
        public double a { get; private set; }

        /// <summary>
        /// periapsis distance from central body
        /// </summary>
        public double periapsis { get; private set; }
        /// <summary>
        /// apoapsis distance from central body
        /// </summary>
        public double apoapsis { get; private set; }

        /// <summary>
        /// orbital period in seconds
        /// </summary>
        public double period { get; private set; }


        /// <summary>
        /// construct an orbit from orbital elements
        /// </summary>
        /// <param name="h">angular momentum</param>
        /// <param name="e">eccentricity</param>
        /// <param name="omega">longitude of periapsis</param>
        /// <param name="M0">mean anomaly at epoch</param>
        /// <param name="t0">epoch time</param>
        /// <param name="body">parent celestial body</param>
        public OrbitState(double h, double e, double omega, double M0, double t0, CelestialBody body)
        {
            this.body = body;
            this.t0 = t0;
            var kprop = new KeplerianPropagator(body.GM, h, e, omega, M0, t0);
            p0 = kprop.GetPosition(t0);
            v0 = kprop.GetVelocity(t0);

            PostUpdate();
        }

        /// <summary>
        /// construct an orbit from cartesian state vectors
        /// </summary>
        /// <param name="pos">position of orbit</param>
        /// <param name="vel">velocity of orbit</param>
        /// <param name="t">time (UT) where the position and velocity happen</param>
        /// <param name="body">parent celestial body</param>
        public OrbitState(Vector2d pos, Vector2d vel, double t, CelestialBody body)
        {
            UpdateFromStateVectors(pos, vel, t, body);
        }

        public void UpdateFromStateVectors(Vector2d pos, Vector2d vel, double t, CelestialBody body)
        {
            this.body = body;
            t0 = t;
            p0 = pos;
            v0 = vel;

            PostUpdate();
        }
        public void UpdateFromStateVectors(Vector2d pos, Vector2d vel)
        {
            UpdateFromStateVectors(pos, vel, Universe.Instance.UT, body);
        }


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
        /// calculations to run after this orbit's parameters have changed
        /// </summary>
        private void PostUpdate()
        {
            h = Vector2d.Cross(p0, v0);

            // eccentricity vector, points in the direction of periapsis
            Vector2d eccVec = Vector2d.Cross(v0, h) / GM - p0.Normalized;
            e = eccVec.Magnitude;

            omega = Math.Atan2(eccVec.y, eccVec.x);

            a = h * h / (GM * (1 - e * e));
            periapsis = a * (1 - e);
            apoapsis = (e < 1) ? (a * (1 + e)) : double.PositiveInfinity; // betasquared is fine because we know e<1

            period = 2 * Math.PI * Math.Sqrt(a * a * a / GM);

            // position in the perifocal plane
            var pos = BodyToPerifocal(p0);

            // true anomaly, range [-PI, PI]
            var nu = Math.Atan2(pos.y, pos.x);

            if (e == 0.0)
            {
                M0 = nu;
            }
            else if (e < 1.0)
            {
                // elliptical orbit
                // https://orbital-mechanics.space/time-since-periapsis-and-keplers-equation/elliptical-orbits.html

                // the equation for eccentric anomaly is
                // 
                // tan( E/2 ) = tan( nu/2 ) * sqrt( (1-e) / (1+e) )
                // OR: E = 2 atan( tan( nu/2 ) * sqrt( (1-e) / (1+e) )
                // 
                // this issue is that, when nu is close to +-PI (i.e. apoapsis), tan gets blown up to a very large unstable number,
                // after which we use atan and compress it to +-PI/2, losing a lot of stability and precision in the process.
                // 
                // to address this we leverage the Atan2 function to keep relatively managable what would have been a very unstable calculation:
                // 
                // E = 2 atan2( sin(nu/2) * sqrt(1-e), cos(nu/2) * sqrt(1+e) )
                // 
                // this avoids any instabilities that arise when the orbit is near apoapsis
                // 
                // NOTE: still unstable for near-parabolic (e close to 1) orbits. might pretend it is parabolic in this case

                var E = 2 * Math.Atan2(
                    Math.Sin(0.5 * nu) * Math.Sqrt(1 - e),
                    Math.Cos(0.5 * nu) * Math.Sqrt(1 + e)
                );
                if (E < 0) E += 2 * Math.PI;
                M0 = E - e * Math.Sin(E);
            }
            else if (e > 1.0)
            {
                // hyperbolic orbit
                // https://orbital-mechanics.space/time-since-periapsis-and-keplers-equation/hyperbolic-trajectories.html

                // the equation for hyperbolic eccentric anomaly is
                // 
                // tanh( F/2 ) = tan( nu/2 ) * sqrt( (e-1) / (e+1) )
                // 
                // if we want to solve for F, we need to use atanh, which is somewhat unstable around the edges of its domain
                // instead, we can use sinh because sinh has the best stability:
                // 
                // sinh( F/2 ) = sin( nu/2 ) * sqrt( (e-1) / (1 + E cos(nu)) )
                // 
                // NOTE 1: this alternative form is only valid for -PI < nu < PI
                // NOTE 2: still unstable near asymptotes, perhaps add fallback for that

                var F = 2 * Math.Asinh(Math.Sin(0.5 * nu) * Math.Sqrt((e - 1) / (1 + e * Math.Cos(nu))));
                if (nu < 0) F = -F;
                M0 = e * Math.Sinh(F) - F;
            }
            else
            {
                // parabolic
                // https://orbital-mechanics.space/time-since-periapsis-and-keplers-equation/parabolic-trajectories.html

                // pretty stable compared to elliptical and hyperbolic orbits
                // as usual, some floating point trouble at infinity but that's alright

                double D = Math.Tan(0.5 * nu);
                M0 = 0.5 * D + D * D * D / 6.0;
            }

            OnStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// a time, a position, and a velocity.
    /// </summary>
    public class StateVector
    {
        public double time;
        public Vector2d pos, vel;
        public StateVector(double time, Vector2d pos, Vector2d vel)
        {
            this.time = time;
            this.pos = pos;
            this.vel = vel;
        }
    }
}
