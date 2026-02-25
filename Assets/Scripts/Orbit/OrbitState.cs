using MathNet.Numerics.RootFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// represents a particular orbit around a <c>CelestialBody</c>.
    /// 
    /// OrbitState is mutable - directly overwriting instances of OrbitState is highly discouraged;
    /// use the Update functions instead to modify orbit state.
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

        private OrbitingObject _owner;
        /// <summary>
        /// the orbiting object that occupies this orbit. possibly <c>null</c>
        /// </summary>
        public OrbitingObject Owner
        {
            get
            {
                if (_owner == null) throw new InvalidOperationException("this orbit does not have an owner");
                return _owner;
            }
            set { _owner = value; }
        }

        public double t0 { get; private set; }
        public Vector2d p0 { get; private set; }
        public Vector2d v0 { get; private set; }

        /// <summary>
        /// invoked when any of the orbital parameters are modified
        /// </summary>
        public event Action OnStateChanged;


        /* derived orbital parameters */

        /// <summary>
        /// distance from body at epoch, the magnitude of p0
        /// </summary>
        public double r0 { get; private set; }
        /// <summary>
        /// magnitude of radial velocity at epoch
        /// </summary>
        public double vr0 { get; private set; }
        /// <summary>
        /// reciprocal semimajor axis, used to determine orbit shape
        /// <para>alpha>0 is elliptical</para>
        /// <para>alpha=0 is parabolic</para>
        /// <para>alpha<0 is hyperbolic</para>
        /// </summary>
        public double alpha { get; private set; }

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
        /// true anomaly at epoch
        /// </summary>
        public double nu0 { get; private set; }
        /// <summary>
        /// eccentric anomaly at epoch, only available for e<1
        /// </summary>
        public double E0 { get; private set; }
        /// <summary>
        /// barker's variable at epoch, only available for e=1
        /// </summary>
        public double D0 { get; private set; }
        /// <summary>
        /// hyperbolic eccentric anomaly at epoch, only available for e>1
        /// </summary>
        public double F0 { get; private set; }

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
        /// semi-latus rectum
        /// </summary>
        public double p { get; private set; }

        /// <summary>
        /// shape of this orbit
        /// </summary>
        public OrbitShape Shape { get => ClassifyEccentricityShape(e); }

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

            IOrbitPropagator prop;
            if (ClassifyEccentricityShape(e) == OrbitShape.Parabola) prop = new BarkerPropagator(body.GM, h, omega, M0, t0);
            else prop = new KeplerianPropagator(body.GM, h, e, omega, M0, t0);
            var state = prop.GetStateVectors(t0);
            p0 = state.pos; v0 = state.vel;

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
        /// <summary>
        /// construct an orbit from cartesian state vectors
        /// </summary>
        /// <param name="state">state vectors at epoch</param>
        /// <param name="body">parent celestial body</param>
        public OrbitState(StateVectors state, CelestialBody body)
        {
            UpdateFromStateVectors(state.pos, state.vel, state.time, body);
        }

        public OrbitState(OrbitState other)
        {
            CopyFrom(other);
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
        /// copy state from another orbit
        /// </summary>
        public void CopyFrom(OrbitState o)
        {
            UpdateFromStateVectors(o.p0, o.v0, o.t0, o.body);
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
            r0 = p0.Magnitude;
            vr0 = Vector2d.Dot(p0, v0) / r0;
            alpha = 2 / r0 - v0.Magnitude2 / GM;
            a = 1 / alpha;

            h = Vector2d.Cross(p0, v0);

            // eccentricity vector, points in the direction of periapsis
            Vector2d eccVec = Vector2d.Cross(v0, h) / GM - p0.Normalized;
            e = eccVec.Magnitude;

            omega = Math.Atan2(eccVec.y, eccVec.x);

            p = h * h / GM;

            periapsis = p / (1 + e);
            apoapsis = Shape == OrbitShape.Ellipse ? (p / (1 - e)) : double.PositiveInfinity;

            period = 2 * Math.PI * Math.Sqrt(a * a * a / GM);

            nu0 = CalcNu(p0);

            switch (Shape)
            {
                case OrbitShape.Ellipse:
                    E0 = CalcAnomaly(nu0);
                    M0 = E0 - e * Math.Sin(E0);
                    break;
                case OrbitShape.Parabola:
                    D0 = CalcAnomaly(nu0);
                    M0 = 0.5 * D0 + D0 * D0 * D0 / 6.0;
                    break;
                case OrbitShape.Hyperbola:
                    F0 = CalcAnomaly(nu0);
                    M0 = e * Math.Sinh(F0) - F0;
                    break;
            }

            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// calculate the relevant anomaly for any orbits
        /// </summary>
        /// <param name="nu">true anomaly</param>
        /// <returns>eccentric anomaly when e<1, barker's variable when e=1, hyperbolic eccentric anomaly when e>1</returns>
        public double CalcAnomaly(double nu)
        {
            switch (Shape)
            {
                case OrbitShape.Ellipse:
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
                    if (h < 0) E = -E;
                    E = MathUtils.Mod(E, 2 * Math.PI);
                    return E;

                case OrbitShape.Parabola:
                    // parabolic orbit
                    // https://orbital-mechanics.space/time-since-periapsis-and-keplers-equation/parabolic-trajectories.html
                    // barker's variable, that's it lol
                    var D = Math.Tan(0.5 * nu);
                    if (h < 0) D = -D;
                    return D;

                case OrbitShape.Hyperbola:
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
                    if (h < 0) F = -F;
                    return F;

                default:
                    throw new NotImplementedException("Orbit shape must be elliptical, parabolic, or hyperbolic");
            }
        }

        /// <summary>
        /// get the relevant anomaly at epoch (t=t0)
        /// </summary>
        public double Anomaly0
        {
            get
            {
                switch (Shape)
                {
                    case OrbitShape.Ellipse:
                        return E0;
                    case OrbitShape.Parabola:
                        return D0;
                    case OrbitShape.Hyperbola:
                        return F0;
                    default:
                        throw new NotImplementedException("Orbit shape must be elliptical, parabolic, or hyperbolic");
                }
            }
        }

        /// <summary>
        /// calculate the true anomaly at any given position, normalized to [ -PI, PI ]
        /// </summary>
        public double CalcNu(Vector2d pos)
        {
            var nu = Math.Atan2(pos.y, pos.x) - omega;
            if (nu > Math.PI) nu -= 2 * Math.PI;
            if (nu < -Math.PI) nu += 2 * Math.PI;
            return nu;
        }

        /// <summary>
        /// get distance from center for a given true anomaly
        /// </summary>
        public double GetDistanceFromNu(double nu)
        {
            return p / (1 + e * Math.Cos(nu));
        }

        /// <summary>
        /// determine whether an eccentricity value belongs to an elliptical, parabolic, or hyperbolic orbit
        /// </summary>
        public static OrbitShape ClassifyEccentricityShape(double e)
        {
            // how close is 'close enough' to a parabola?
            const double TOL = 1e-5;
            if (e + TOL < 1) return OrbitShape.Ellipse;
            if (1 < e - TOL) return OrbitShape.Hyperbola;
            else return OrbitShape.Parabola;
        }
    }

    /// <summary>
    /// describes the shape of an orbit
    /// </summary>
    public enum OrbitShape { Ellipse, Parabola, Hyperbola }

    /// <summary>
    /// a time, a position, and a velocity.
    /// </summary>
    public readonly struct StateVectors
    {
        public readonly double time;
        public readonly Vector2d pos, vel;
        public StateVectors(double time, Vector2d pos, Vector2d vel)
        {
            this.time = time;
            this.pos = pos;
            this.vel = vel;
        }

        public static StateVectors None = new StateVectors(double.NaN, null, null);
        public static bool IsNone(StateVectors state) => double.IsNaN(state.time) && state.pos == null && state.vel == null;

        public override string ToString() => $"[ t={time}; p={pos}; v={vel} ]";
    }
}
