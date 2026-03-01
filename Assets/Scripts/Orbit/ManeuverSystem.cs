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

    public Maneuver GetManeuver()
    {
        if (!HasManeuver) NextManeuver = new Maneuver();
        return NextManeuver;
    }
}
