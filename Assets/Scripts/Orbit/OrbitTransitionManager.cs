using System.Collections;
using System.Collections.Generic;
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

        /// <summary>
        /// rechecks all orbit transitions
        /// </summary>
        public void CheckTransitions()
        {
            NextTransition = null;
            foreach (var transition in _transitions)
            {
                transition.CheckTransition();
                if (transition.HasTransition && (NextTransition == null || transition.TransitionTime < NextTransition.TransitionTime))
                    NextTransition = transition;
            }

            _nextTrajectory.gameObject.SetActive(HasTransition);
            if (HasTransition) _nextOrbit.CopyFrom(NextTransition.NextOrbit);
        }

        /// <summary>
        /// updates orbit state if it is time for the next transition
        /// </summary>
        public void UpdateOrbit()
        {
            if (NextTransition != null && Universe.Instance.UT >= NextTransition.TransitionTime)
            {
                _orbit.CopyFrom(NextTransition.NextOrbit);
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
        /// time of transition. NaN if no transition will occur
        /// </summary>
        public double TransitionTime { get; protected set; }
        public bool HasTransition { get => !double.IsNaN(TransitionTime); }

        /// <summary>
        /// new orbit state once transition occurs
        /// </summary>
        public OrbitState NextOrbit { get; protected set; }

        protected readonly struct TransitionResult
        {
            public readonly double time;
            public readonly OrbitState orbit;
            public TransitionResult(double time, OrbitState orbit)
            {
                this.time = time;
                this.orbit = orbit;
            }
            
            public static TransitionResult None = new(double.NaN, null);
        }

        /// <summary>
        /// override this to implement the actual computation of orbit transition
        /// </summary>
        protected abstract TransitionResult CalcTransitionResult();

        /// <summary>
        /// updates the predicted transition time for this orbit transition.
        /// should be run any time the state of the orbit changes
        /// (this is handled if it is registered in an OrbitTransitionManager)
        /// </summary>
        public void CheckTransition()
        {
            var result = CalcTransitionResult();
            TransitionTime = result.time;
            NextOrbit = result.orbit;
        }
    }
}