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
        /// velocity change at maneuver
        /// </summary>
        public Vector2d dv;

        /// <summary>
        /// patch manager that this Maneuver is operating on
        /// </summary>
        public readonly PatchedConicManager sourcePatchManager;

        /// <summary>
        /// patch that this maneuver lies on
        /// </summary>
        public Patch SourcePatch { get; private set; }

        /// <summary>
        /// new orbital trajectory after burn takes place
        /// </summary>
        public readonly OrbitState resultOrbit;
        
        /// <summary>
        /// new patch manager after burn takes place
        /// </summary>
        public readonly PatchedConicManager resultPatches;

        /// <summary>
        /// construct a new maneuver
        /// </summary>
        /// <param name="source">original PatchedConicManager this maneuver is based on</param>
        /// <param name="UT">time at which the maneuver occurs. if left blank, set to 1 minute ahead of current UT</param>
        /// <param name="dv">velocity change at maneuver, in body space. set to zero if left blank</param>
        public Maneuver(PatchedConicManager source, double UT = double.NaN, Vector2d dv = null)
        {
            sourcePatchManager = source;

            resultOrbit = new(source.SrcOrbit);
            resultPatches = new PatchedConicManager(resultOrbit);

            this.UT = double.IsNaN(UT) ? Universe.Instance.UT + 60 : UT;
            this.dv = dv is null ? Vector2d.zero : dv;
        }
    }
}