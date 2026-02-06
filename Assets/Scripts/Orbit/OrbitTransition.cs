using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// defines handling for changes in orbit state
    /// </summary>
    public abstract class OrbitTransition
    {
        protected OrbitState orbit { get; private set; }

        public OrbitTransition(OrbitState orbit)
        {
            this.orbit = orbit;
        }

        /// <summary>
        /// state vectors at the time of transition
        /// </summary>
        public StateVectors State { get; private set; }
        /// <summary>
        /// time of transition. NaN if no transition will occur
        /// </summary>
        public double Time { get => State.time; }
        public bool HasTransition { get => !double.IsNaN(Time); }
        /// <summary>
        /// if HasTransition is false, this is the time at which this transition possibly becomes invalid.
        /// equal to double.PositiveInfinity if HasTransition is true or if it is certain that there will never be a transition event.
        /// </summary>
        public double ExpiryDate { get; private set; }

        /// <summary>
        /// new orbit state once transition occurs
        /// </summary>
        public OrbitState NextOrbit { get; private set; }

        protected struct TransitionResult
        {
            public StateVectors state;
            public OrbitState orbit;
            /// <summary>
            /// time at which this prediction becomes invalid and require a recalculation
            /// (i.e. we didn't bother predicting ahead of this point)
            /// </summary>
            public double expiryDate;
            public double Time { get => state.time; }
            public TransitionResult(StateVectors state, OrbitState orbit, double expiryDate = double.PositiveInfinity)
            {
                this.state = state;
                this.orbit = orbit;
                this.expiryDate = expiryDate;
            }

            public static TransitionResult None = new(new StateVectors(double.NaN, null, null), null);
            /// <summary>
            /// no transition predicted, but this prediction expires at time UT
            /// </summary>
            /// <returns>newly constructed TransitionResult</returns>
            public static TransitionResult ExpiresAt(double UT) => new(new StateVectors(double.NaN, null, null), null, expiryDate: UT);
        }

        /// <summary>
        /// override this to implement the actual computation of orbit transition
        /// </summary>
        protected abstract TransitionResult CalcTransitionResult(double UT);

        /// <summary>
        /// updates the predicted transition time for this orbit transition.
        /// should be run any time the state of the orbit changes
        /// (this is handled if it is registered in an OrbitTransitionManager)
        /// </summary>
        public void CheckTransition(double UT)
        {
            var result = CalcTransitionResult(UT);
            State = result.state;
            NextOrbit = result.orbit;
            ExpiryDate = result.expiryDate;
        }
    }
}