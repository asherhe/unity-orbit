using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class ManeuverPlanner : MonoBehaviour
    {
        private InputActions _inputActions;

        /// <summary>
        /// maneuver that is currently being planned
        /// </summary>
        private Orbit.Maneuver _maneuver;

        private void Awake()
        {
            // init inputactions
        }
    }
}