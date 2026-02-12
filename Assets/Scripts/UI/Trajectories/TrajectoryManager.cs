using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class TrajectoryManager : SingletonBehaviour<TrajectoryManager>
    {
        public GameObject trajectoryPrefab;
        private readonly HashSet<Trajectory> _trajectories = new();

        /// <summary>
        /// add a new trajectory to display
        /// </summary>
        /// <returns>newly created trajectory component</returns>
        public Trajectory AddTrajectory(OrbitState o)
        {
            var trajObject = Instantiate(trajectoryPrefab, transform);
            trajObject.transform.localPosition = Vector3.zero;
            var trajectory = trajObject.GetComponent<Trajectory>();
            trajectory.Orbit = o;
            _trajectories.Add(trajectory);
            return trajectory;
        }

        /// <summary>
        /// remove a trajectory from display
        /// </summary>
        /// <returns>true if trajectory was found and removed, false otherwise</returns>
        public bool RemoveTrajectory(Trajectory t)
        {
            if (_trajectories.Remove(t))
            {
                Destroy(t.gameObject);
                return true;
            }
            return false;
        }
    }
}