using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// contains information about the in-game universe
/// </summary>
[RequireComponent(typeof(TimewarpControls))]
public class Universe : SingletonBehaviour<Universe>
{
    /// <summary>
    /// universal gravitational constant, in <c>m^3/(kg s^2)</c>
    /// </summary>
    public double G = 6.67430e-11;

    /// <summary>
    /// universal time
    /// </summary>
    public double UT = 0.0;

    public TimewarpControls Timewarp { get; private set; }
    
    /// <summary>
    /// fixed delta time adjusted for time warp
    /// </summary>
    public double fixedDeltaTime { get => Time.fixedDeltaTime * Timewarp.TimewarpScale; }

    protected override void Awake()
    {
        base.Awake();

        Timewarp = GetComponent<TimewarpControls>();
    }

    private void FixedUpdate()
    {
        UT += fixedDeltaTime;
    }
}
