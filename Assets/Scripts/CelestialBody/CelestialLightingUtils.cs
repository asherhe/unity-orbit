using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// some lighting utility functions for use in space lighting
/// </summary>
public class CelestialLightingUtils
{
    /// <summary>
    /// determine the intensity of sunlight falling at a given distance to the sun
    /// </summary>
    /// <param name="heliocentric">heliocentric position to sample</param>
    /// <returns>unitless sun intensity, scaled to roughly 1 at Hamswell orbit</returns>
    public static double SunIntensity(Vector2d heliocentric) => 8.5e21 / heliocentric.Magnitude2;

    /// <summary>
    /// determine the fraction incident sunlight that falls on an orbiting object due to 
    /// </summary>
    /// <returns>fraction of incident sunlight that makes it to the point - 0 is completely shadowed, 1 is completely illuminated</returns>
    public static double CastBodySoftShadow(OrbitingObject o)
    {
        // body is sun: no shadows
        if (o.orbit.body.orbit == null) return 1.0f;

        var heliocentric = o.GetHeliocentricPosition();
        var sunDirection = -heliocentric.Normalized;

        // find closest point along sun ray to parent body
        var t = Math.Max(-Vector2d.Dot(sunDirection, o.Position), 0.0);
        var closest = o.Position + t * sunDirection;
        // altitude of closest point
        var alt = closest.Magnitude - o.orbit.body.radius;
        // apparent radius of the sun projected on to the sunlight's point of closest approach
        var apparentRad = CelestialBodyManager.Instance.celestialBodies["Sun"].radius / heliocentric.Magnitude * t;
        // the distance of the sun above the horizon, in apparent radii
        var hhorizon = Math.Clamp(alt / apparentRad, -1, 1);
        // fraction of sun that is visible
        // the area of visible sun disk (in apparent radii) is the integral of 2 sqrt(1 - x^2) from -1 to hhorizon
        // we want to divide that by PI to get the fraction of light. integrating gives us the expression
        //     0.5 + 1/PI ( h sqrt( 1 - h^2 ) + asin h )
        // where h is hhorizon
        var diskFrac = 0.5 + (hhorizon * Math.Sqrt(1 - hhorizon * hhorizon) + Math.Asin(hhorizon)) / Math.PI;
        return diskFrac;
    }
}
