using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// defines handling for changes in orbit state
    /// </summary>
    public abstract class OrbitTransitionHandler
    {
        /// <summary>
        /// reference to OrbitState object this OrbitTransitionHandler answers to
        /// </summary>
        protected OrbitState orbit { get; private set; }

        public OrbitTransitionHandler(OrbitState orbit)
        {
            this.orbit = orbit;
            _result = TransitionResult.None;
        }

        /// <summary>
        /// stored result of next predicted transition
        /// </summary>
        private TransitionResult _result;

        /// <summary>
        /// state vectors at the time of transition
        /// </summary>
        public StateVectors State { get => _result.state; }
        /// <summary>
        /// time of transition. NaN if no transition will occur
        /// </summary>
        public double Time { get => State.time; }
        public bool HasTransition { get => !double.IsNaN(Time); }
        /// <summary>
        /// if HasTransition is false, this is the time at which this transition possibly becomes invalid.
        /// equal to double.PositiveInfinity if HasTransition is true or if it is certain that there will never be a transition event.
        /// </summary>
        public double ExpiryDate { get => _result.expiryDate; }

        /// <summary>
        /// new orbit state once transition occurs
        /// </summary>
        public OrbitState NextOrbit { get => _result.nextOrbit; }
        /// <summary>
        /// state vectors at the other end at the start of the next orbit
        /// </summary>
        public StateVectors NextState { get => _result.nextState; }

        protected struct TransitionResult
        {
            public StateVectors state, nextState;
            public OrbitState nextOrbit;
            /// <summary>
            /// time at which this prediction becomes invalid and require a recalculation
            /// (i.e. we didn't bother predicting ahead of this point)
            /// </summary>
            public double expiryDate;
            public double Time { get => state.time; }
            public TransitionResult(StateVectors state, CelestialBody nextBody, StateVectors nextState, double expiryDate = double.PositiveInfinity)
            {
                this.state = state;
                this.nextState = nextState;
                nextOrbit = new OrbitState(nextState, nextBody);
                this.expiryDate = expiryDate;
            }

            public static TransitionResult None = new(StateVectors.None, null, StateVectors.None);
            /// <summary>
            /// no transition predicted, but this prediction expires at time UT
            /// </summary>
            /// <returns>newly constructed TransitionResult</returns>
            public static TransitionResult ExpiresAt(double UT) => new(StateVectors.None, null, StateVectors.None, expiryDate: UT);
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
            _result = CalcTransitionResult(UT);
        }

        /// <summary>
        /// clear stored transition prediction
        /// </summary>
        public void ClearResults()
        {
            _result = TransitionResult.None;
        }
    }
}