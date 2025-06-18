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
    private double _mass = 0.0;
    /// <summary>
    /// center of mass of spacecraft IGNORING part plugin masses
    /// </summary>
    private Vector2d _COM = Vector2d.zero;

    /// <summary>
    /// craft wet mass (w. plugin masses)
    /// </summary>
    public double Mass { get => _mass; }
    /// <summary>
    /// craft wet center of mass (w. plugin masses)
    /// 
    /// note that the determination of center of mass assumes that each part is a point mass
    /// located at wherever the specified location of the part is
    /// </summary>
    public Vector2d CenterOfMass { get => _COM; }

    public event Action OnMassChanged;

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

    /// <summary>
    /// reset mass to zero
    /// </summary>
    public void ZeroMass()
    { 
        _mass = 0.0;
        _COM = Vector2d.zero;
        OnMassChanged?.Invoke();
    }

    /// <summary>
    /// adds a point mass and updates center of mass, moment of inertia, etc.
    /// 
    /// note that SpacecraftNewtonian does not actually keep track of individual point masses,
    /// but instead updates mass and center of mass as if a point mass was added.
    /// </summary>
    /// <param name="craftPos">location of mass in craft space, in meters</param>
    /// <param name="mass">mass, in kg</param>
    public void AddPointMass(Vector2d craftPos, double mass)
    {
        _COM = _mass * _COM + mass * craftPos;
        _mass += mass;
        _COM /= _mass;
        OnMassChanged?.Invoke();
    }

    private void FixedUpdate()
    {
        angle += angularVelocity * Universe.Instance.fixedDeltaTime;
        angle = angle % (2 * Math.PI);
    }
}
