using MathNet.Numerics.RootFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// represents a particular orbit around a <c>CelestialBody</c>
/// </summary>
public class Orbit
{
    /// <summary>
    /// the celestial body this orbit goes around
    /// </summary>
    public CelestialBody body { get; private set; }
    /// <summary>
    /// standard gravitational parameter
    /// </summary>
    public double GM { get => body.GM; }

    /* orbital elements */
    /* these guys are used to uniquely identify a given orbit */

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

    /// <summary>
    /// converts a vector from body space to perifocal space
    /// </summary>
    private Vector2d BodyToPerifocal(Vector2d vbody)
    {
        var perifocal = vbody.Rotate(-omega);
        if (h < 0) perifocal.y = -perifocal.y;
        return perifocal;
    }
    /// <summary>
    /// converts a vector from body space to perifocal space
    /// </summary>
    private Vector2d PerifocalToBody(Vector2d vperifocal)
    {
        var body = new Vector2d(vperifocal);
        if (h < 0) body.y = -body.y;
        body = body.Rotate(omega);
        return body;
    }

    public void UpdateFromStateVectors(Vector2d pos, Vector2d vel, double t, CelestialBody body)
    {
        // https://en.wikipedia.org/wiki/Orbit_determination#Orbit_Determination_from_a_State_Vector

        this.body = body;
        t0 = t;

        h = Vector2d.Cross(pos, vel);

        // eccentricity vector, points in the direction of periapsis
        Vector2d eccVec = Vector2d.Cross(vel, h) / GM - pos.Normalized;
        e = eccVec.Magnitude;

        omega = Math.Atan2(eccVec.y, eccVec.x);

        // position in the perifocal plane
        pos = BodyToPerifocal(pos);

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
            M0 = CalcKepler(E);
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
            M0 = CalcKepler(F);
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

        PostUpdate();
    }
    public void UpdateFromStateVectors(Vector2d pos, Vector2d vel)
    {
        UpdateFromStateVectors(pos, vel, Universe.Instance.UT, body);
    }


    /* get orbit info */

    /// <summary>
    /// the semimajor axis (in meters) of the orbit
    /// </summary>
    public double A { get; private set; }

    public double Periapsis { get; private set; }
    public double Apoapsis { get; private set; }

    /// <summary>
    /// commonly used value in calculations. equal to 1-e^2 when e<1 and e^2-1 when e>1
    /// </summary>
    public double BetaSquared { get; private set; }

    /// <summary>
    /// orbital period in seconds
    /// </summary>
    public double Period { get; private set; }

    /// <summary>
    /// mean motion is the rate at which the mean anomaly changes
    /// </summary>
    public double MeanMotion { get; private set; }

    /// <summary>
    /// get mean anomaly at a given time
    /// </summary>
    public double GetMeanAnomaly(double UT) => M0 + (UT - t0) * MeanMotion;

    /// <summary>
    /// calculates the value of the RHS of kepler's equation.
    /// used to solve for eccentric anomaly.
    /// </summary>
    /// <param name="E">eccentric anomaly</param>
    private double CalcKepler(double E)
    {
        if (e < 1) return E - e * Math.Sin(E);
        else if (e > 1) return e * Math.Sinh(E) - E;
        else return 0; // kepler's equation doesn't apply to parabolic orbits
    }
    /// <summary>
    /// the derivative of CalcKepler
    /// </summary>
    /// <param name="E">eccentric anomaly</param>
    private double CalcDKepler(double E)
    {
        if (e < 1) return 1 - e * Math.Cos(E);
        else if (e > 1) return e * Math.Cosh(E) - 1;
        else return 0; // kepler's equation doesn't apply to parabolic orbits
    }

    /// <summary>
    /// get eccentric anomaly at a given time
    /// </summary>
    public double GetEccentricAnomaly(double UT) => GetEccentricAnomalyFromMeanAnomaly(GetMeanAnomaly(UT));
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

    public double GetTrueAnomaly(double UT) => GetTrueAnomalyFromMeanAnomaly(GetMeanAnomaly(UT));
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
        Vector2d pos;
        if (e < 1)
        {
            var E = GetEccentricAnomaly(UT);
            pos = new(
                A * (Math.Cos(E) - e),
                A * Math.Sqrt(BetaSquared) * Math.Sin(E)
            );
        }
        else if (e > 1)
        {
            var F = GetEccentricAnomaly(UT);
            pos = new(
                -A * (e - Math.Cosh(F)),
                -A * Math.Sqrt(BetaSquared) * Math.Sinh(F)
            );
        }
        else
        {
            var nu = GetTrueAnomaly(UT);
            var r = h * h / (GM * (1 + Math.Cos(nu)));
            pos = new(
                r * Math.Cos(nu),
                r * Math.Sin(nu)
            );
        }

        return PerifocalToBody(pos);
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
        Vector2d vel;
        if (e < 1)
        {
            var E = GetEccentricAnomaly(UT);
            var r = A * (1 - e * Math.Cos(E));
            vel = new(
                -Math.Sin(E),
                Math.Sqrt(BetaSquared) * Math.Cos(E)
            );
            vel *= Math.Sqrt(GM * A) / r;
        }
        else if (e > 1)
        {
            var F = GetEccentricAnomaly(UT);
            var r = -A * (e * Math.Cosh(F) - 1);
            vel = new(
                -Math.Sinh(F),
                Math.Sqrt(BetaSquared) * Math.Cosh(F)
            );
            vel *= Math.Sqrt(GM * -A) / r;
        }
        else
        {
            var nu = GetTrueAnomaly(UT);
            vel = new(
                -Math.Sin(nu),
                1 + Math.Cos(nu)
            );
            vel *= GM / h;
        }
        return PerifocalToBody(vel);
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
    /// calculations to run after this orbit's parameters have changed
    /// </summary>
    private void PostUpdate()
    {
        A = h * h / (GM * (1 - e * e));
        Periapsis = A * (1 - e);
        Apoapsis = (e < 1) ? (A * (1 + e)) : double.PositiveInfinity; // betasquared is fine because we know e<1
        BetaSquared = Math.Abs(1 - e * e); // 1-e^2 for elliptical, e^2-1 for hyperbolic
        if (e == 1)
            MeanMotion = GM * GM / (h * h * h);
        else
            MeanMotion = Math.Sqrt(GM / Math.Abs(A * A * A));
        Period = 2 * Math.PI / MeanMotion;

        // CheckSOI();
        // CheckCaptures();
        // ^ TODO: i keep getting flooded by errors because of these, commenting out for now

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
        var E = (A - body.soiRadius) / (A * e);
        if (e < 1.0) E = Math.Acos(E);
        else E = Math.Acosh(E);
        // no SOI intersection
        if (E == Double.NaN) return;

        // the two intersection points with the SOI - one with positive (p) and one with negative (n) eccentric anomaly
        // the next order of business is to determine the time they intersect
        var Mp = CalcKepler(E);
        var Mn = CalcKepler(-E);

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
        tp = t0 + (Mp - M0) / MeanMotion;
        tn = t0 + (Mn - M0) / MeanMotion;

        // assign SOI state vectors, ensure chronological order
        StateVector statep, staten;
        statep = new(tp, GetPosition(tp), GetVelocity(tp));
        staten = new(tn, GetPosition(tn), GetVelocity(tn));
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

        /// <summary>
        /// the object that <c>other</c> belongs to, used internally to keep tabs on encounters
        /// </summary>
        public IOrbitingObject orbitingObject;

        public Encounter(Orbit o, StateVector state, double distance)
        {
            other = o;
            this.state = state;
            this.distance = distance;
        }
    }

    public List<Encounter> GetEncounters(Orbit o) => GetEncounters(o, Universe.Instance.UT);
    public List<Encounter> GetEncounters(Orbit o, double UT)
    {
        if (e < 1.0)
            return GetEncounters(o, UT, UT + Period);
        else if (e == 1.0)
            throw new NotImplementedException();
        else
        {
            if (o.e < 1.0)
            {
                // end time is when x=-o.apoapsis in this hyperbola's perifocal frame
                // probably involves some eccanom magic to find that time
                var E = Math.Acosh(o.Apoapsis / A - e);
                var M = CalcKepler(E);
                var t = t0 + Math.Abs((M - M0) / MeanMotion); // abs to find the later one
                if (UT > t) return new();
                else return GetEncounters(o, UT, t);
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
    public List<Encounter> GetEncounters(Orbit o, double tStart, double tEnd, int brackets = 100)
    {
        if (body != o.body)
            throw new ArgumentException("Orbit o must share the same body as this orbit.");

        // derivative of distance
        double DDistance(double t) => 2.0 * Vector2d.Dot(
            GetPosition(t) - o.GetPosition(t),
            GetVelocity(t) - o.GetVelocity(t)
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
                    Vector2d pos = GetPosition(t), vel = GetVelocity(t),
                         opos = o.GetPosition(t), ovel = o.GetVelocity(t);
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

    /* SOI captures */

    /// <summary>
    /// the state of this orbit at the time of the next capture
    /// </summary>
    public StateVector nextCapture { get; private set; }
    /// <summary>
    /// the celestial body that will capture this orbit
    /// </summary>
    public CelestialBody nextCaptureBody { get; private set; }

    private void CheckCaptures()
    {
        nextCapture = null; nextCaptureBody = null;

        Encounter captureEncounter = null;
        foreach (var satellite in body.satellites)
        {
            var encounters = GetEncounters(satellite.orbit);
            foreach (var e in encounters)
                if (e.distance < satellite.soiRadius && (captureEncounter == null || e.state.time < captureEncounter.state.time))
                {
                    captureEncounter = e;
                    captureEncounter.orbitingObject = satellite;
                }
        }
        if (captureEncounter == null) return;

        var b = (CelestialBody)captureEncounter.orbitingObject;
        var o = captureEncounter.other;
        var t = captureEncounter.state.time;

        // estimated time to traverse the SOI radius
        var soiTrav = b.soiRadius / captureEncounter.state.vel.Magnitude;

        // distance to SOI edge
        double SOIDistance(double t) => (GetPosition(t) - o.GetPosition(t)).Magnitude - b.soiRadius;

        double captureTime = 0;
        try
        {
            captureTime = Brent.FindRoot(
                SOIDistance,
                t - 2 * soiTrav, t,
                accuracy: 1e-12,
                maxIterations: 100
            );
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        nextCapture = new(captureTime, GetPosition(captureTime), GetVelocity(captureTime));
        nextCaptureBody = b;
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
            Debug.Log("soi escape");
            var t = soiEscape.time;
            UpdateFromStateVectors(
                body.orbit.GetPosition(t) + soiEscape.pos,
                body.orbit.GetVelocity(t) + soiEscape.vel,
                t, body.parent
            );
            return true;
        }
        if (nextCapture != null && UT >= nextCapture.time)
        {
            Debug.Log("soi capture");
            var t = nextCapture.time;
            Debug.Log($"p={GetPosition(t)} v={GetVelocity(t)}");
            UpdateFromStateVectors(
                nextCapture.pos - nextCaptureBody.orbit.GetPosition(t),
                nextCapture.vel - nextCaptureBody.orbit.GetVelocity(t),
                t, nextCaptureBody
            );
            Debug.Log($"p={GetPosition(t) + body.orbit.GetPosition(t)} v={GetVelocity(t) + body.orbit.GetVelocity(t)}");
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
