using Orbit;
using System;
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

    private UI.ManeuverLabel label;

    public bool HasManeuver => NextManeuver != null;

    /// <summary>
    /// invoked when we get a new maneuver or if the manevuer is cleared
    /// </summary>
    public event Action OnManeuverChanged;

    /// <summary>
    /// get the next pending maneuver, initializing a new one if none exists
    /// </summary>
    public Maneuver GetManeuver()
    {
        if (!HasManeuver) { 
            NextManeuver = new Maneuver(ActiveCraftController.Instance.craft);
            label = UI.MapLabelManager.Instance.AddManeuverLabel(NextManeuver);
            OnManeuverChanged?.Invoke();
        }
        return NextManeuver;
    }

    /// <summary>
    /// remove a maneuver from the list of pending maneuvers
    /// </summary>
    public void RemoveManeuver(Maneuver maneuver)
    {
        // TODO: maneuver is a dummy parameter in anticipation for maneuver list
        NextManeuver.Dispose();
        NextManeuver = null;
        Destroy(label.gameObject);
        OnManeuverChanged?.Invoke();
    }
}
