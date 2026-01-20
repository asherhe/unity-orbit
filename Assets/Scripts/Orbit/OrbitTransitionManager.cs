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
        /// predicted next transition to happen
        /// </summary>
        public OrbitTransition NextTransition { get; private set; }

        public OrbitTransitionManager(OrbitState orbit)
        {
            _orbit = orbit;
            _transitions = new();
            NextTransition = null;
            orbit.OnStateChanged += CheckTransitions;
        }

        /// <summary>
        /// run the transition check for a transition and update internal stuff if necessary
        /// </summary>
        public void CheckTransition(OrbitTransition transition)
        {
            transition.CheckTransition();
            if (NextTransition == null || transition.TransitionTime < NextTransition.TransitionTime)
                NextTransition = transition;
        }

        /// <summary>
        /// register a new OrbitTransition for checking
        /// </summary>
        public void Add(OrbitTransition transition)
        {
            _transitions.Add(transition);
            CheckTransition(transition);
        }
        /// <summary>
        /// rechecks all orbit transitions
        /// </summary>
        public void CheckTransitions()
        {
            NextTransition = null;
            foreach (var transition in _transitions) CheckTransition(transition);
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