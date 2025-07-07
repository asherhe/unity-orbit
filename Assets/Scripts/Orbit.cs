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
    public CelestialBody body;


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
    public double h;

    /// <summary>
    /// eccentricity
    /// </summary>
    /// <remarks>
    /// a circular orbit has zero eccentricity.
    /// an elliptical orbit has eccentricity between 0 and 1.
    /// a parabolic orbit has eccentricity of 1
    /// a hyperbolic orbit has eccentricity above 1
    /// </remarks>
    public double e;

    /// <summary>
    /// longitude of periapsis
    /// </summary>
    /// <remarks>
    /// the longitude of periapsis indicates the angle this orbit is pointing in.
    /// to be more specific, it describes the angle between the +x axis to the periapsis of the orbit
    /// (angle is expressed in radians)
    /// </remarks>
    public double omega;

    /// <summary>
    /// mean anomaly at epoch
    /// </summary>
    public double M0;

    /// <summary>
    /// epoch time
    /// </summary>
    /// <remarks>
    /// in Universal Time.
    /// (units seconds)
    /// </remarks>
    public double t0;


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

    /// <summary>
    /// makes a clockwise circular orbit around a given body. 
    /// </summary>
    /// <param name="rad">altitude of the circular orbit, in km</param>
    /// <param name="body">celestial body this orbit should go around</param>
    /// <returns>a new circular orbit, with the location of the orbit being at the top of the +y direction at the current UT</returns>
    public static Orbit MakeCircularOrbit(double rad, CelestialBody body)
    {
        rad = rad * 1000.0 + body.radius;
        return new Orbit(
            -Math.Sqrt(rad * body.GM),
            0.0, 0.0,
            1.5 * Math.PI, Universe.Instance.UT,
            body
        );
    }


    public void UpdateFromStateVectors(Vector2d pos, Vector2d vel, double t, CelestialBody body)
    {
        // https://en.wikipedia.org/wiki/Orbit_determination#Orbit_Determination_from_a_State_Vector

        t0 = t;

        h = Vector2d.Cross(pos, vel);

        // eccentricity vector, points in the direction of periapsis
        Vector2d eccVec = Vector2d.Cross(vel, h) / body.GM - pos.normalized;
        e = eccVec.magnitude;

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

            if (e < 1.0)
            {
                double E = Math.Atan2(pos.y, pos.x);
                M0 = E - e * Math.Sin(E);
            }
            else
            {
                double E = -Math.Atanh(pos.y / pos.x);
                M0 = e * Math.Sinh(E) - E;
            }
        }
    }
    public void UpdateFromStateVectors(Vector2d pos, Vector2d vel)
    {
        UpdateFromStateVectors(pos, vel, Universe.Instance.UT, body);
    }

    /* get orbit info */

    /// <summary>
    /// the semimajor axis (in meters) of the orbit
    /// </summary>
    public double SemimajorAxis { get => h * h / (body.GM * (1 - e * e)); }

    /// <summary>
    /// get mean anomaly at a given time
    /// </summary>
    public double GetMeanAnomaly(double UT) => M0 + (UT - t0) * Math.Abs(body.GM * body.GM * (1.0 - e * e) / (h * h * h));

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

            for (int i = 0; i < BISECT_ITERS; i++)
            {
                double mid = (left + right) / 2;
                if (CalcKepler(left) <= M && M < CalcKepler(mid)) right = mid;
                else left = mid;
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
            double E = GetEccentricAnomaly(UT);

            Vector2d pos;
            if (e < 1.0) pos = new Vector2d(Math.Cos(E), Math.Sin(E));
            else pos = new Vector2d(Math.Cosh(E), -Math.Sinh(E));

            pos.x -= e;
            pos.y *= Math.Sqrt(Math.Abs(1 - e * e));
            if (h < 0.0) pos.y = -pos.y;

            pos = (pos * SemimajorAxis).Rotate(omega);

            return pos;
        }
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
            double E = GetEccentricAnomaly(UT);

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
    }
}

/// <summary>
/// any object in orbit around a celestial body
/// </summary>
public interface IOrbitingObject
{
    public Orbit orbit { get; }
}
