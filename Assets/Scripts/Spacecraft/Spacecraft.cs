using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spacecraft : MonoBehaviour, IOrbitingObject
{
    // TODO: remove this once we have a way to automatically load crafts
    [SerializeField]
    private CelestialBody _body;

    public CelestialBody body { get => orbit.body; }
    public Orbit orbit { get; set; }
    private Trajectory _trajectory;

    public Vector2d pos { get; private set; }

    public Vector2d vel { get; private set; }

    public double altitude { get => pos.magnitude - body.radius; }

    // spacecraft control parameters
    // TODO: convert this to use parts

    /// <summary>
    /// spacecraft thrust, in m/s^2
    /// </summary>
    public float thrust = 20.0f;

    /// <summary>
    /// spacecraft turn rate, in deg/s
    /// </summary>
    public float turnRate = 60.0f;

    private float _throttle = 0.0f;
    /// <summary>
    /// spacecraft throttle (between 0.0 and 1.0)
    /// </summary>
    public float Throttle
    {
        get => _throttle;
        set { _throttle = Mathf.Clamp01(value); }
    }

    public float _steeringControl = 0.0f;
    /// <summary>
    /// input for spacecraft steering (between -1.0 and 1.0), positive is ccw
    /// </summary>
    public float SteeringControl
    {
        get => _steeringControl;
        set { _steeringControl = Mathf.Clamp(value, -1.0f, 1.0f); }
    }

    private void Awake()
    {
        // TODO: placeholder orbit, a 200km circular orbit
        orbit = Orbit.MakeCircularOrbit(200.0, _body);

        _trajectory = body.AddTrajectory(this);

        pos = orbit.GetPosition(); vel = orbit.GetVelocity();
    }

    private void FixedUpdate()
    {
        pos = orbit.GetPosition(); vel = orbit.GetVelocity();

        if (Universe.Instance.timewarpScale == 1.0)
        {
            transform.rotation *= Quaternion.Euler(0, 0, (float)(SteeringControl * turnRate * Universe.Instance.fixedDeltaTime));
            
            if (Throttle > 0.0)
            {
                Vector2d dv = new Vector2d(transform.up.x, transform.up.y) * (Throttle * thrust * Universe.Instance.fixedDeltaTime);
                orbit.UpdateFromStateVectors(pos, vel + dv, Universe.Instance.UT, body);
            }
        }

        if (ActiveCraftController.Instance.craft != this)
        {
            transform.position = pos - ActiveCraftController.Instance.craft.pos;
        }
    }
}
