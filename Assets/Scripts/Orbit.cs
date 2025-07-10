using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// represents a particular orbit around a <c>CelestialBody</c>
/// </summary>
public class Orbit
{
    /// <summary>
    /// the celestial body this orbit goes around
    /// </summary>
    public CelestialBody body { get; private set; }

    /* orbital elements */
    /* these guys are used to uniquely identify a given orbit */

    /// <summary>
    /// specific angular momentum around the parent body.
    /// may occasionally be referred to as just "angular momentum".
    /// </summary>
    /// <remarks>
    /// the sign of the angular momentum obeys the right hand rule (positive is a clockwise orbit).
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
    /// epoch time
    /// </summary>
    /// <remarks>
    /// in Universal Time.
    /// (units seconds)
    /// </remarks>
    public double t0 { get; private set; }

    /// <summary>
    /// invoked when any of the orbital parameters are modified
    /// </summary>
    public event Action OnOrbitChanged;

    /* orbit constructors */

    /// <summary>
    /// construct an orbit from orbital elements
    /// </summary>
    /// <param name="h">angular momentum</param>
    /// <param name="e">eccentricity</param>
    /// <param name="omega">longitude of periapsis</param>
    /// <param name="M0">mean anomaly at epoch</param>
    /// <param name="t0">epoch time</param>
    /// <param name="body">parent celestial body</param>
    public Orbit(double h, double e, double omega, double M0, double t0, CelestialBody body)
    {
        this.body = body;
        this.h = h;
        this.e = e;
        this.omega = omega;
        this.M0 = M0;
        this.t0 = t0;

        PostUpdate();
    }

    /// <summary>
    /// construct an orbit from cartesian state vectors
    /// </summary>
    /// <param name="pos">position of orbit</param>
    /// <param name="vel">velocity of orbit</param>
    /// <param name="t">time (UT) where the position and velocity happen</param>
    /// <param name="body">parent celestial body</param>
    public Orbit(Vector2d pos, Vector2d vel, double t, CelestialBody body)
    {
        UpdateFromStateVectors(pos, vel, t, body);
    }


    public void UpdateFromStateVectors(Vector2d pos, Vector2d vel, double t, CelestialBody body)
    {
        // https://en.wikipedia.org/wiki/Orbit_determination#Orbit_Determination_from_a_State_Vector

        this.body = body;
        t0 = t;

        h = Vector2d.Cross(pos, vel);

        // eccentricity vector, points in the direction of periapsis
        Vector2d eccVec = Vector2d.Cross(vel, h) / body.GM - pos.Normalized;
        e = eccVec.Magnitude;

        omega = Math.Atan2(eccVec.y, eccVec.x);

        // position in the perifocal plane
        pos = pos.Rotate(-omega);

        if (e == 1.0)
        {
            M0 = 0.0; // TODO: parabolic trajectories
        }
        else
        {
            pos *= body.GM * (1 - e * e) / (h * h); // 1/a
            pos.x += e;
            pos.y /= Math.Sqrt(Math.Abs(1 - e * e));
            if (h < 0.0) pos.y = -pos.y;

            double E;
            if (e < 1.0) E = Math.Atan2(pos.y, pos.x);
            else E = -Math.Atanh(pos.y / pos.x);
            M0 = CalcKepler(E);
        }

        PostUpdate();
    }
    public void UpdateFromStateVectors(Vector2d pos, Vector2d vel)
    {
        UpdateFromStateVectors(pos, vel, Universe.Instance.UT, body);
    }

    /// <summary>
    /// calculations to run after this orbit's parameters have changed
    /// </summary>
    private void PostUpdate()
    {
        a = h * h / (body.GM * (1 - e * e));
        periapsis = a * (1 - e * e) / (1 + e);
        apoapsis = a * (1 - e * e) / (1 - e);
        meanMotion = Math.Abs(body.GM * body.GM * (1.0 - e * e) / (h * h * h));

        CheckSOI();

        OnOrbitChanged?.Invoke();
    }

    /* sphere of influence */

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

    /// <summary>
    /// state vectors when this orbit enters the SOI of its parent body
    /// </summary>
    public StateVector soiCapture;
    /// <summary>
    /// state vectors when this orbit leaves the SOI of its parent body
    /// </summary>
    public StateVector soiEscape;

    /// <summary>
    /// calculate the state vectors (time, position, velocity) of this orbit at the moments when this orbit enters or leaves the SOI.
    /// </summary>
    private void CheckSOI()
    {
        soiCapture = null; soiEscape = null;

        // check if body HAS an SOI first
        if (body.orbit == null) return;

        if (e == 1.0) return; // TODO: this is for non-parabolic orbits only

        // calculate eccentric anomaly at SOI radius (plus or minus)
        var E = (a - body.soiRadius) / (a * e);
        if (e < 1.0) E = Math.Acos(E);
        else E = Math.Acosh(E);
        // no SOI intersection
        if (E == Double.NaN) return;

        // the two intersection points with the SOI - one with positive (p) and one with negative (n) eccentric anomaly
        // the next order of business is to determine the time they intersect
        double Mp, Mn;
        Vector2d Pp, Pn, Vp, Vn;
        // these can probably be more optimized since a lot of calculations are repeated, but i'll deal with it later
        Mp = CalcKepler(E);
        Mn = CalcKepler(-E);
        Pp = GetPositionFromEccentricAnomaly(E);
        Pn = GetPositionFromEccentricAnomaly(-E);
        Vp = GetVelocityFromEccentricAnomaly(E);
        Vn = GetVelocityFromEccentricAnomaly(-E);

        // change Mp and Mn to be on the same orbital period as the current mean anomaly
        // not for hyperbolic orbits tho because those guys don't do periodic orbits
        if (e < 1.0)
        {
            var M = GetMeanAnomaly(Universe.Instance.UT);
            // whichever periapsis we are closest to
            var Mperi = 2 * Math.PI * Math.Round(M / (2 * Math.PI));
            Mp += Mperi; Mn += Mperi;
        }

        // calculate time to SOI
        double tp, tn;
        tp = t0 + (Mp - M0) / meanMotion;
        tn = t0 + (Mn - M0) / meanMotion;

        // assign SOI state vectors, ensure chronological order
        StateVector statep, staten;
        statep = new(tp, Pp, Vp);
        staten = new(tn, Pn, Vn);
        if (tp > tn)
        {
            soiCapture = staten;
            soiEscape = statep;
        }
        else
        {
            soiCapture = statep;
            soiEscape = staten;
        }
    }

    /* encounters */
    public class Encounter
    {
        /// <summary>
        /// the orbit we have an encounter with
        /// </summary>
        public Orbit other;
        /// <summary>
        /// state of THIS orbit at the encounter
        /// </summary>
        public StateVector state;
        /// <summary>
        /// distance at encounter
        /// </summary>
        public double distance;
    }

    /// <summary>
    /// gets a list of all encounters with the given orbit
    /// </summary>
    public List<Encounter> GetEncounters(Orbit o)
    {
        throw new NotImplementedException();
    }

    /* get orbit info */

    /// <summary>
    /// the semimajor axis (in meters) of the orbit
    /// </summary>
    public double a { get; private set; }

    public double periapsis { get; private set; }
    public double apoapsis { get; private set; }

    /// <summary>
    /// mean motion is the rate at which the mean anomaly changes
    /// </summary>
    public double meanMotion { get; private set; }

    /// <summary>
    /// get mean anomaly at a given time
    /// </summary>
    public double GetMeanAnomaly(double UT) => M0 + (UT - t0) * meanMotion;

    /// <summary>
    /// calculates the value of the RHS of kepler's equation.
    /// used to solve for eccentric anomaly.
    /// </summary>
    /// <param name="E">eccentric anomaly</param>
    private double CalcKepler(double E)
    {
        if (e < 1.0) return E - e * Math.Sin(E);
        else if (e > 1.0) return e * Math.Sinh(E) - E;
        else return 0.0; // kepler's equation doesn't apply to parabolic orbits
    }
    /// <summary>
    /// the derivative of CalcKepler
    /// </summary>
    /// <param name="E">eccentric anomaly</param>
    private double CalcDKepler(double E)
    {
        if (e < 1.0) return 1 - e * Math.Cos(E);
        else if (e > 1.0) return e * Math.Cosh(E);
        else return 0; // kepler's equation doesn't apply to parabolic orbits
    }

    /// <summary>
    /// get eccentric anomaly at a given time
    /// </summary>
    public double GetEccentricAnomaly(double UT)
    {
        double M = GetMeanAnomaly(UT);
        if (M == 0.0) return 0.0;
        if (e == 0.0) return M;

        /*
         * find eccentric anomaly by solving for E in kepler's equation
         * 
         *   e<1:  M = E - e sin E
         *   e>1:  M = e sinh E - E
         */

        const int NEWTON_ITERS = 10, // surprisingly 10 is quite enough for most cases
                  BISECT_ITERS = 60; // double has 53 fractional bits

        /*
         * ROOT FINDING ALGORITHMS
         * we have a choice between using newton's method and the bisection method to find roots.
         * newton's method often converges faster, but fails in regions where slope is near zero.
         * bisection converges slower compared to newton but gives far more consistent results.
         * 
         * note that for bisection, since kepler's equation k(E) is always monotonically increasing, we only have to
         * worry about k(low) <= M < k(mid)
         * 
         * my experiments show that
         *  - FOR ELLIPTICAL ORBITS: newton is pretty stable for e<0.95. use bisection otherwise
         *  (bisection range is mean anomaly plus or minus PI)
         *  - FOR HYPERBOLIC ORBITS: newton works for |M|<2PI, but past that, results are
         *  kinda sketchy because of how steep kepler's equation becomes. bisection range
         *  is between 0 and M+1 or M-1 if M is positive or negative, respectively.
         */

        if ((0.95 < e && e < 1.0) || (e > 1.0 && Math.Abs(M) < 2 * Math.PI))
        {
            // bisection method
            double left, right;
            if (e < 1.0) { left = M - Math.PI; right = M + Math.PI; }
            else
            {
                if (M > 0.0) { left = 0; right = M + 1; }
                else { left = M - 1; right = 0; }
            }

            double leftM = CalcKepler(left);
            for (int i = 0; i < BISECT_ITERS; i++)
            {
                double mid = (left + right) / 2;
                if (leftM <= M && M < CalcKepler(mid))
                    right = mid;
                else
                {
                    left = mid;
                    leftM = CalcKepler(left);
                }
            }
            return left;
        }

        // newton's method
        double E = M;
        for (int i = 0; i < NEWTON_ITERS; i++)
            E -= (CalcKepler(E) - M) / CalcDKepler(E);
        return E;
    }

    /// <summary>
    /// get the position (in world space) of the orbit at the current UT
    /// </summary>
    /// <returns>position of orbit, in meters</returns>
    public Vector2d GetPosition() => GetPosition(Universe.Instance.UT);

    /// <summary>
    /// get the position (in world space) of the orbit at a given time
    /// </summary>
    /// <returns>position of orbit, in meters</returns>
    public Vector2d GetPosition(double UT)
    {
        if (e == 1.0)
        {
            return Vector2d.zero; // TODO: parabolic trajectories
        }
        else
        {
            return GetPositionFromEccentricAnomaly(GetEccentricAnomaly(UT));
        }
    }

    private Vector2d GetPositionFromEccentricAnomaly(double E)
    {
        Vector2d pos;
        if (e < 1.0) pos = new Vector2d(Math.Cos(E), Math.Sin(E));
        else pos = new Vector2d(Math.Cosh(E), -Math.Sinh(E));

        pos.x -= e;
        pos.y *= Math.Sqrt(Math.Abs(1 - e * e));
        if (h < 0.0) pos.y = -pos.y;

        pos = (pos * a).Rotate(omega);

        return pos;
    }


    /// <summary>
    /// get the position (in world space) of the orbit at the current UT
    /// </summary>
    /// <returns>position of orbit, in meters.</returns>
    public Vector2d GetVelocity() => GetVelocity(Universe.Instance.UT);

    /// <summary>
    /// get the velocity (in world space) of the orbit at a given time
    /// </summary>
    /// <returns>velocity of orbit, in meters</returns>
    public Vector2d GetVelocity(double UT)
    {
        if (e == 1.0)
        {
            return Vector2d.zero; // TODO: parabolic trajectories
        }
        else
        {
            return GetVelocityFromEccentricAnomaly(GetEccentricAnomaly(UT));
        }
    }

    private Vector2d GetVelocityFromEccentricAnomaly(double E)
    {
        Vector2d vel;
        if (e < 1.0) vel = new Vector2d(-Math.Sin(E), Math.Cos(E));
        else vel = new Vector2d(Math.Sinh(E), -Math.Cosh(E));

        vel.x *= Math.Sqrt(Math.Abs(1 - e * e));
        vel.y *= Math.Abs(1 - e * e);
        if (h < 0.0) vel.y = -vel.y;

        vel *= body.GM / (h * (e * (
            e < 1.0 ?
            Math.Cos(E) :
            Math.Cosh(E)
        ) - 1));
        vel = vel.Rotate(omega);

        return vel;
    }

    /// <summary>
    /// get the current position of this orbit, with the sun at the origin
    /// </summary>
    public Vector2d GetHeliocentricPosition()
    {
        var parentPos = Vector2d.zero;
        if (body.orbit != null) parentPos = body.orbit.GetHeliocentricPosition();
        return parentPos + GetPosition();
    }

    /// <summary>
    /// check for SOI escapes, captures, etc., and update this orbit's state accordingly
    /// </summary>
    /// <returns>true this orbit switched celestial bodies, false otherwise</returns>
    public bool CheckBodyChange() => CheckBodyChange(Universe.Instance.UT);
    public bool CheckBodyChange(double UT)
    {
        if (soiEscape != null && UT >= soiEscape.time)
        {
            var t = soiEscape.time;
            UpdateFromStateVectors(
                body.orbit.GetPosition(t) + GetPosition(t),
                body.orbit.GetVelocity(t) + GetVelocity(t),
                t,
                body.parent
            );
            return true;
        }
        return false;
    }
}

/// <summary>
/// any object in orbit around a celestial body
/// </summary>
public interface IOrbitingObject
{
    public Orbit orbit { get; }
}
