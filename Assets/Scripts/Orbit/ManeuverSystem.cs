using Orbit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManeuverSystem : SingletonBehaviour<ManeuverSystem>
{
    /// <summary>
    /// next pending maneuver
    /// TODO: i'd like to add multiple maneuvers at some point
    /// </summary>
    public Maneuver NextManeuver { get; private set; }

    public bool HasManeuver => NextManeuver != null;

    /// <summary>
    /// get the next pending maneuver, initializing a new one if none exists
    /// </summary>
    public Maneuver GetManeuver()
    {
        if (!HasManeuver) NextManeuver = new Maneuver();
        return NextManeuver;
    }

    /// <summary>
    /// remove a maneuver from the list of pending maneuvers
    /// </summary>
    public void RemoveManeuver(Maneuver maneuver)
    {
        // TODO: dummy parameter in anticipation for maneuver list
        NextManeuver = null;
    }
}
