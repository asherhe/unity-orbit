using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CelestialBodyData : ScriptableObject
{
    /* body physical info */

    /// <summary>
    /// body radius (at sea level), in km
    /// </summary>
    public double radius = 6371.0;

    /// <summary>
    /// gravitational acceleration at sea level (m/s^2)
    /// </summary>
    /// <remarks>
    /// we use this to determine the necessary mass to make this possible
    /// </remarks>
    public double surfaceG = 9.81;

    /// <summary>
    /// length of a day, in julian days
    /// </summary>
    public double dayLength = 1.0;

    /* body display */

    /// <summary>
    /// material to use for body surface
    /// </summary>
    public Material surfaceMaterial;

    /// <summary>
    /// material to use for body atmosphere (if applicable)
    /// </summary>
    public Material atmMaterial;

    /// <summary>
    /// color of planet glow
    /// </summary>
    public Color glowColor = new Color(1, 1, 1, 1);

    /* atmosphere & clouds */

    /// <summary>
    /// whether this celestial body has an atmosphere
    /// </summary>
    public bool hasAtmosphere = true;

    /// <summary>
    /// height of atmosphere, in km
    /// </summary>
    public double atmosphereHeight = 100.0;

    /// <summary>
    /// atmospheric pressure at sea level, in atm
    /// </summary>
    public double atmosphereSeaLevelPressure = 1.0;

    /// <summary>
    /// scale height of atmosphere, the altitude where air pressure is 1/e of pressure at sea level (in km)
    /// </summary>
    public double atmosphereScaleHeight = 8.5;

    /// <summary>
    /// rayleight scattering coefficients
    /// </summary>
    public Vector4 rayleighScattering = new Vector4(0.000005804542996261093f, 0.000013562911419845635f, 0.00003026590629238531f, 0.000012619774364741572f);
}
