using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// manages patched conic trajectory state and trajectory
    /// </summary>
    public class PatchedConicManager
    {
        /// <summary>
        /// internally stored orbit state of the orbiting object we are managing
        /// </summary>
        public readonly OrbitState SrcOrbit;

        /// <summary>
        /// internal storage of all orbit patches.
        /// stores both active and inactive patches - inactive patches are enabled on demand.
        /// </summary>
        private readonly List<Patch> _patches;
        /// <summary>
        /// all patches of this orbit, whether active or inactive
        /// </summary>
        public IEnumerable<Patch> AllPatches { get => _patches.AsReadOnly(); }
        /// <summary>
        /// all active patches of this orbit
        /// </summary>
        public IEnumerable<Patch> ActivePatches
        {
            get
            {
                for (int i = 0; i < numActivePatches; i++)
                    yield return _patches[i];
            }
        }

        /// <summary>
        /// number of patches actually predicted to exist
        /// </summary>
        public int numActivePatches;

        /// <summary>
        /// invoked when the current orbit transitions from one patch to another
        /// </summary>
        public event Action OnTransition;
        /// <summary>
        /// invoked when all orbit transition states have been recalculated
        /// </summary>
        public event Action OnRecalculated;

        /// <summary>
        /// configures a PatchedConicManager linked to an object with orbit state orbit
        /// </summary>
        /// <param name="orbit">OrbitState of the source object that this PatchedConicManager answers to</param>
        /// <param name="maxPatches">maximum number of conic patches to predict (including current patch)</param>
        public PatchedConicManager(OrbitState orbit, int maxPatches = 6)
        {
            SrcOrbit = orbit;

            // bind this before we initialize patches so that the trajectory update code within
            // Patch executes AFTER new transition points are established
            SrcOrbit.OnStateChanged += RecalculatePatches;

            // construct linked patches
            _patches = new List<Patch>(maxPatches) { new Patch(SrcOrbit, this) };
            for (int i = 1; i < maxPatches; i++)
                _patches.Add(new Patch(_patches[i - 1], this));
        }

        /// <summary>
        /// recalculate all future trajectory path predictions at the current UT.
        /// automatically called when SrcOrbit changes state.
        /// </summary>
        public void RecalculatePatches() => RecalculatePatches(Universe.Instance.UT);

        /// <summary>
        /// recalculate all future trajectory patch predictions
        /// </summary>
        /// <param name="UT">current time along starting patch</param>
        /// <param name="startPatch">id of the patch to start recalculating from (0 is current orbit). all patches before this point are left untouched</param>
        public void RecalculatePatches(double UT, int startPatch = 0)
        {
            // current "time" as we step through patches
            double t = UT;

            int i = startPatch;
            for (; i < _patches.Count; i++)
            {
                var patch = _patches[i];
                _patches[i].SetActive(true);

                patch.CheckTransitions(t);
                if (!patch.HasTransition) { i++; break; }

                // advance to next patch
                t = patch.NextTransition.Time;
            }

            numActivePatches = i;
            // disable all inactive patches
            for (; i < _patches.Count; i++)
            {
                _patches[i].SetActive(false);
            }

            OnRecalculated?.Invoke();
        }

        /// <summary>
        /// updates orbit state if it is time for the next transition.
        /// should run in unity's Update or FixedUpdate loop.
        /// </summary>
        public void Update(double UT)
        {
            // repeatedly advance through future conic patches until there are no more
            while (!_patches[0].HasTransition || _patches[0].NextTransition.Time <= UT)
            {
                // ensure that no hidden transitions exist
                while (_patches[0].ExpiryDate < UT)
                    RecalculatePatches(_patches[0].ExpiryDate);

                if (_patches[0].HasTransition && _patches[0].NextTransition.Time <= UT)
                {
                    // advance orbits of all conic sections forward by one

                    // NOTE: this triggers a reevaluation of all patch transition handlers. this is not ideal
                    // because the state of all patches save the last can actually be transferred to the preceding one.
                    // however, implementing the logic to copy state of all fields in a patch, including the internal
                    // state of all OrbitTransitionHandler subclasses, is very much a hassle, so for now we use the
                    // quick and dirty solution

                    SrcOrbit.CopyFrom(_patches[0].nextOrbit);

                    OnTransition?.Invoke();
                }
                else break;
            }
        }
    }
}