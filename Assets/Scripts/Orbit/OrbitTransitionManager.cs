using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Orbit
{
    public class OrbitTransitionManager
    {
        private readonly OrbitState _orbit;

        /// <summary>
        /// all transitions
        /// </summary>
        private readonly List<OrbitTransition> _transitions;

        /// <summary>
        /// predicted next transition to happen. <c>null</c> if no transition will happen
        /// </summary>
        public OrbitTransition NextTransition { get; private set; }
        /// <summary>
        /// whether a next transition will happen
        /// </summary>
        public bool HasTransition { get => NextTransition != null; }

        /// <summary>
        /// if there is no transition predicted, this is the UT time at which our prediction expires and
        /// we recalculate the next trajectory. guarenteed to be double.PositiveInfinity if HasTransition is true.
        /// </summary>
        public double ExpiryDate { get; private set; }

        /// <summary>
        /// keep track of next orbit for display purposes
        /// </summary>
        private readonly OrbitState _nextOrbit;

        /// <summary>
        /// trajectory display for next orbit
        /// </summary>
        private readonly Trajectory _nextTrajectory;

        public OrbitTransitionManager(OrbitState orbit)
        {
            _orbit = orbit;
            _transitions = new();
            NextTransition = null;

            _nextOrbit = new(orbit);
            _nextTrajectory = TrajectoryManager.Instance.AddTrajectory(_nextOrbit);
            _nextTrajectory.Width = 2;
            _nextTrajectory.DoAnimation = false;
            _nextTrajectory.AlphaRange = new Vector2(0.5f, 0.0f);
            _nextTrajectory.gameObject.SetActive(false);

            orbit.OnStateChanged += CheckTransitions;
        }

        /// <summary>
        /// register a new OrbitTransition for checking (does not actually run checks)
        /// </summary>
        public void Add(OrbitTransition transition)
        {
            _transitions.Add(transition);
        }

        public void CheckTransitions() => CheckTransitions(Universe.Instance.UT);

        /// <summary>
        /// rechecks all orbit transitions at time UT
        /// </summary>
        public void CheckTransitions(double UT)
        {
            double t = UT;
            OrbitTransition nextExpiry = null;
            do
            {
                if (nextExpiry != null)
                {
                    t = ExpiryDate;
                }

                // reset transition memory
                NextTransition = null;
                ExpiryDate = double.PositiveInfinity;

                foreach (var transition in _transitions)
                {
                    transition.CheckTransition(t);
                    if (transition.HasTransition && (NextTransition == null || transition.Time < NextTransition.Time))
                        NextTransition = transition;

                    // keep track of most imminent expiry
                    if (transition.ExpiryDate < ExpiryDate)
                    {
                        ExpiryDate = transition.ExpiryDate;
                        nextExpiry = transition;
                    }
                }
            } while (HasTransition && ExpiryDate < NextTransition.Time);

            _nextTrajectory.gameObject.SetActive(HasTransition);
            if (HasTransition) _nextOrbit.CopyFrom(NextTransition.NextOrbit);
        }

        /// <summary>
        /// updates orbit state if it is time for the next transition.
        /// should run in unity's Update or FixedUpdate loop.
        /// </summary>
        public void UpdateOrbit()
        {
            // there is the possibility with large warp rates that we will step over several orbital
            // transititons at once. we do all this to ensure that no transition is overlooked
            double nextEvent = Math.Min(HasTransition ? NextTransition.Time : double.PositiveInfinity, ExpiryDate);
            while (nextEvent < Universe.Instance.UT)
            {
                // ensure that we are using the most up-to-date prediction
                if (nextEvent >= ExpiryDate)
                    CheckTransitions(nextEvent);
                // handle transition if necessary
                if (HasTransition && nextEvent >= NextTransition.Time)
                    _orbit.CopyFrom(NextTransition.NextOrbit);

                // both CheckTransitions and OrbitState.CopyFrom should have worked their magic and
                // mutated the state of this object by now
                nextEvent = Math.Min(HasTransition ? NextTransition.Time : double.PositiveInfinity, ExpiryDate);
            } 
        }
    }

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