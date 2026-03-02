using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    public class Maneuver
    {
        private double _UT;
        /// <summary>
        /// time of maneuver
        /// </summary>
        public double UT
        {
            get => _UT;
            set { _UT = value; UpdateInternalState(); }
        }

        private Vector2d _Dv;
        /// <summary>
        /// velocity change at maneuver
        /// </summary>
        public Vector2d Dv
        {
            get => _Dv;
            set { _Dv = value; UpdateInternalState(); }
        }

        /// <summary>
        /// velocity change in prograde/radial out space.
        /// x-component is prograde, y-component is radial out
        /// </summary>
        public Vector2d DvPR
        {
            get => new Vector2d(
                Vector2d.Dot(Dv, SourcePrograde),
                Vector2d.Dot(Dv, SourceRadialOut)
            );
            set => Dv = value.x * SourcePrograde + value.y * SourceRadialOut;
        }

        /// <summary>
        /// position at time of maneuver
        /// </summary>
        public Vector2d Position { get; private set; }
        /// <summary>
        /// original velocity at time of maneuver
        /// </summary>
        public Vector2d SourceVelocity { get; private set; }

        public Vector2d SourcePrograde { get; private set; }
        public Vector2d SourceRadialOut { get; private set; }

        /// <summary>
        /// craft that is intended to execute this maneuver
        /// </summary>
        public readonly Spacecraft craft;

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

        public event Action OnManeuverStateUpdate;

        /// <summary>
        /// construct a new maneuver
        /// </summary>
        /// <param name="source">original PatchedConicManager this maneuver is based on</param>
        /// <param name="UT">time at which the maneuver occurs. if left blank, set to 1 minute ahead of current UT</param>
        /// <param name="dv">velocity change at maneuver, in body space. set to zero if left blank</param>
        public Maneuver(Spacecraft craft, double UT = double.NaN, Vector2d dv = null)
        {
            this.craft = craft;
            resultOrbit = new(craft.orbit);
            resultPatches = new PatchedConicManager(resultOrbit);

            _UT = double.IsNaN(UT) ? Universe.Instance.UT + 60 : UT;
            _Dv = dv is null ? Vector2d.zero : dv;

            craft.orbit.OnStateChanged += UpdateInternalState;
            UpdateInternalState();
        }

        ~Maneuver()
        {
            craft.orbit.OnStateChanged -= UpdateInternalState;
        }

        /// <summary>
        /// update internal state after orbit time changes
        /// </summary>
        private void UpdateInternalState()
        {
            // ensure we rule out the possibility for any unforseen transitions
            SourcePatch = craft.patches.FirstPatch;
            while ((SourcePatch.HasTransition && UT >= SourcePatch.NextTransition.Time) || UT >= SourcePatch.ExpiryDate)
            {
                while (UT >= SourcePatch.ExpiryDate)
                    craft.patches.RecalculatePatches(SourcePatch.ExpiryDate, SourcePatch.patchStep);

                if (SourcePatch.nextPatch == null) break;
                else SourcePatch = SourcePatch.nextPatch;
            }

            // UT goes past our furthest patch prediction: clamp it
            if (SourcePatch.nextPatch == null && SourcePatch.HasTransition)
                // set _UT so we don't trigger a recursive call to UpdateInternalState
                _UT = Math.Min(UT, SourcePatch.NextTransition.Time);

            var prop = new UniversalPropagator(SourcePatch.patchOrbit);
            var state = prop.GetStateVectors(UT);
            Position = state.pos; SourceVelocity = state.vel;

            SourcePrograde = OrbitState.GetPRDirection(SourceVelocity, SourcePatch.patchOrbit.h, PRDirection.Prograde);
            SourceRadialOut = OrbitState.GetPRDirection(SourceVelocity, SourcePatch.patchOrbit.h, PRDirection.RadialOut);

            resultOrbit.UpdateFromStateVectors(Position, SourceVelocity + Dv, UT, SourcePatch.patchOrbit.body);

            OnManeuverStateUpdate?.Invoke();
        }
    }
}