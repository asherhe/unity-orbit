using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    public class Maneuver
    {
        /// <summary>
        /// time of maneuver
        /// </summary>
        public double UT;

        /// <summary>
        /// 
        /// </summary>
        public Patch patch;

        /// <summary>
        /// new orbital trajectory after burn takes place
        /// </summary>
        public PatchedConicManager resultingPatches;
    }
}