using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryManager : SingletonBehaviour<TrajectoryManager>
{
    public GameObject trajectoryPrefab;

    /// <summary>
    /// add a new trajectory to display
    /// </summary>
    /// <returns>newly created trajectory component</returns>
    public Trajectory AddTrajectory(IOrbitingObject o)
    {
        if (o.orbit == null)
            throw new ArgumentException("Expected an object with an orbit.");

        var trajObject = Instantiate(trajectoryPrefab, transform);
        trajObject.name = $"Trajectory {o.ToString()}";
        trajObject.transform.localPosition = Vector3.zero;
        var trajectory = trajObject.GetComponent<Trajectory>();
        trajectory.OrbitingObj = o;
        return trajectory;
    }
}
