using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// handles newtonian physics for this spacecraft
/// </summary>
[RequireComponent(typeof(Spacecraft))]
public class SpacecraftNewtonian : MonoBehaviour
{
    /// <summary>
    /// mass of spacecraft IGNORING part plugin masses
    /// </summary>
    public double dryMass = 0.0;
    /// <summary>
    /// center of mass of spacecraft IGNORING part plugin masses
    /// </summary>
    public Vector2d dryCOM = Vector2d.zero;
    /// <summary>
    /// mass of plugins
    /// </summary>
    public double pluginMass = 0.0;
    /// <summary>
    /// center of mass of plugins
    /// </summary>
    public Vector2d pluginCOM = Vector2d.zero;

    /// <summary>
    /// craft wet mass (w. plugin masses)
    /// </summary>
    public double Mass { get => dryMass + pluginMass; }
    /// <summary>
    /// craft wet center of mass (w. plugin masses)
    /// 
    /// note that the determination of center of mass assumes that each part is a point mass
    /// located at wherever the specified location of the part is
    /// </summary>
    public Vector2d CenterOfMass { get => Mass == 0 ? Vector2d.zero : (dryMass * dryCOM + pluginMass * pluginCOM) / Mass; }


    /// <summary>
    /// angle of the spacecraft's local +x axis counterclockwise from the world +x axis, in radians
    /// </summary>
    public double angle;
    /// <summary>
    /// moment of inertia around the center of mass, in kg m^2
    /// 
    /// note that the determination of moment of inertia assumes that each part is a point mass
    /// located at wherever the specified location of the part is. this frankly does not work well
    /// when a heavy part is close to the center of mass (or if the craft is just one part), so TODO
    /// </summary>
    public double momentOfIntertia;
    /// <summary>
    /// angular momentum of this craft's rotation around its center of mass, in kg m^2 s^-1
    /// </summary>
    public double angularMomentum;
    /// <summary>
    /// angular velocity of thsi craft's rotation around its COM, in rad/s
    /// </summary>
    public double angularVelocity { get => momentOfIntertia == 0.0 ? 0.0 : angularMomentum / momentOfIntertia; }

    private void FixedUpdate()
    {
        Debug.Log($"theta={angle}; I={momentOfIntertia}; L={angularMomentum}; omega={angularVelocity}");
        angle += angularVelocity * Universe.Instance.fixedDeltaTime;
        angle = angle % (2 * Math.PI);
    }
}
