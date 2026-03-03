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


    public struct PlanetShineProperties
    {
        public Color color;
        public float intensity;
        public Vector4 direction;
        public float spread;

        public static PlanetShineProperties None
        {
            get
            {
                PlanetShineProperties props = new();
                props.intensity = 0f;
                return props;
            }
        }
    }
    public static PlanetShineProperties ComputePlanetShine(OrbitingObject o)
    {
        // adapted from KSP PlanetShine mod by Valerian
        // https://github.com/PapaJoesSoup/ksp-planetshine/blob/master/PlanetShine/PlanetShine.cs#L186
        var body = o.orbit.body;
        // sun does not need planet shine
        if (body.orbit == null) return PlanetShineProperties.None;

        // shrink body slightly to avoid numerical issues
        var bodyRadius = body.radius * 0.999;

        var bodyHeliocentric = body.GetHeliocentricPosition();
        // direction from body center to sun
        var bodySunDir = -bodyHeliocentric.Normalized;
        var bodySunlight = SunIntensity(bodyHeliocentric);

        // direction from body to object
        var bodyObjDir = o.Position.Normalized;
        // distance to body center
        var objBodyDistance = o.Position.Magnitude;
        // altitude above body surface
        var objAlt = Math.Max(objBodyDistance - bodyRadius, 1); // we don't like low altitudes because it messes math up

        // visible body surface as seen from object, as a fraction of body radius
        // uses similar triangles instead of tangents so it's a little off, but still good enough
        var visibleSurface = objAlt / objBodyDistance;
        // angles from the sun at which no and all visible area is illuminated from the pov of the object
        var litAngleMax = Math.PI / 2 + (1 + visibleSurface);
        var litAngleMin = Math.PI / 2 + (1 - visibleSurface);
        // angle from sun to object relative to body center
        var signedSunAngle = Vector2d.Angle(bodySunDir, bodyObjDir);
        var sunAngle = Math.Abs(signedSunAngle);
        // fraction of visible surface that is illuminated
        var litFrac = (litAngleMax - sunAngle) / (litAngleMax - litAngleMin);
        litFrac = Math.Clamp(litFrac, 0, 1);

        // angle between the vessel and the average center of illuminated surface
        var litAngleAvg = (Math.PI / 2 * visibleSurface) * (1 - (litFrac * (1 - sunAngle / Math.PI)));
        // light intensity reduction as a result of body surface's "diffuse" reflection
        // boost it to make it look better (0.3 term)
        var litAngleEffect = Math.Clamp(0.3 + 1 - (sunAngle - litAngleAvg) / (Math.PI / 2), 0, 1);

        // average source of incident reflected light to object
        var lightPos = bodyRadius * bodyObjDir.Rotate(-Math.Sign(signedSunAngle) * litAngleAvg);
        // direction from object to average light source
        var lightDir = (lightPos-o.Position).Normalized;

        // TODO: implement atmospheric effects at a later date

        // apparent angular size of whole body
        var bodyAngularSize = Math.Acos(Math.Sqrt(Math.Max(objBodyDistance * objBodyDistance - bodyRadius * bodyRadius, 1)) / objBodyDistance);
        // apparent angular size of light source
        var lightAngularSize = bodyAngularSize * Math.Min(Math.PI / 4, (litFrac * (1 - sunAngle / Math.PI)));
        // light falloff based on distance
        var objScaleDistance = objBodyDistance / bodyRadius;
        // inverse square law
        var lightDistanceEffect = 1 / (objScaleDistance * objScaleDistance);
        // NOTE: KSP planetshine included reinhard mapping for hdr, will this be necessary here?

        // combine everything for total shine intensity
        var shineIntensity = bodySunlight * litFrac * litAngleEffect * lightDistanceEffect * body.planetShineConfig.intensity;

        // our illumination formula uses modified diffuse reflectance, where light intensity I is
        //     I = s + (1-s) cos(theta)
        // where theta is the angle from the light to the normal. we want I = 0 when theta is at
        // right angles with lightAngularSize, or theta = PI/2 + lightAngularSize. in this case,
        // cos(theta) is equivalent to -sin(lightAngularSize). solving for s yields
        //     s = sin(lightAngularSize) / ( sin(lightAngularSize) + 1 )
        var sineLightAngularSize = Math.Sin(lightAngularSize);
        var shineSpread = sineLightAngularSize / (sineLightAngularSize + 1);

        PlanetShineProperties props = new();
        props.color = body.planetShineConfig.color;
        props.intensity = (float)shineIntensity;
        props.direction = lightDir;
        props.spread = (float)shineSpread;
        return props;
    }
}
