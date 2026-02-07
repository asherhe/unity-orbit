using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// a single conic patch of an orbit
    /// </summary>
    public class Patch
    {
        /// <summary>
        /// reference to the orbit state this Patch encapsulates
        /// </summary>
        public readonly OrbitState patchOrbit;
        /// <summary>
        /// stores the orbital state of this conic patch
        /// </summary>
        public readonly OrbitState nextOrbit;

        public readonly Patch prevPatch;
        public Patch nextPatch { get; private set; } = null;

        /// <summary>
        /// all transition handlers that can update this orbit patch from the previous one
        /// </summary>
        public readonly List<OrbitTransitionHandler> transitions;

        // specific orbit transition handlers
        public readonly SOIEscapeTransition soiEscape;
        public readonly SOIInterceptTransition soiIntercept;


        /// <summary>
        /// predicted next transition to happen on this patch. <c>null</c> if no transition will happen
        /// </summary>
        public OrbitTransitionHandler NextTransition { get; private set; }
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
        /// trajectory display for this patch.
        /// </summary>
        public readonly Trajectory trajectory;

        /// <summary>
        /// how far removed this Patch is from the original, current OrbitState.
        /// equal to zero if this Patch represents the current orbit.
        /// </summary>
        private readonly int _patchStep;
        
        /// <summary>
        /// construct a patch based on the orbit state of an orbiting body.
        /// the resulting Patch is directly linked to the current state of the body.
        /// </summary>
        public Patch(OrbitState srcOrbit)
        {
            patchOrbit = srcOrbit;
            nextOrbit = new(patchOrbit);

            prevPatch = null;

            transitions = new()
            {
                (soiEscape = new(nextOrbit)),
                (soiIntercept = new(nextOrbit))
            };

            NextTransition = null;
            ExpiryDate = double.PositiveInfinity;

            trajectory = TrajectoryManager.Instance.AddTrajectory(patchOrbit);

            _patchStep = 0;
        }

        /// <summary>
        /// construct a new Patch that follows the given patch
        /// </summary>
        public Patch(Patch patch)
        {
            patchOrbit = patch.nextOrbit;
            nextOrbit = new(patchOrbit);

            prevPatch = patch;
            if (patch.nextPatch != null) throw new InvalidOperationException("provided preceding patch already has a subsequent patch.");
            patch.nextPatch = this;

            transitions = new()
            {
                (soiEscape = new(nextOrbit)),
                (soiIntercept = new(nextOrbit))
            };

            NextTransition = null;
            ExpiryDate = double.PositiveInfinity;

            trajectory = TrajectoryManager.Instance.AddTrajectory(patchOrbit);
            trajectory.DoAnimation = false;
            trajectory.AlphaRange = new Vector2(0.5f, 0.0f);
            trajectory.gameObject.SetActive(false);

            _patchStep = patch._patchStep + 1;
        }

        /// <summary>
        /// designate this patch as inactive
        /// </summary>
        public void Disable()
        {
            // TODO
            trajectory.gameObject.SetActive(false);
        }

        public void CheckTransitions() => CheckTransitions(Universe.Instance.UT);

        /// <summary>
        /// rechecks all orbit transitions at time UT.
        /// if possible, determines the next predicted transition. otherwise, yields an "expiry date"
        /// before which the orbit is guarenteed not to undergo transition.
        /// </summary>
        public void CheckTransitions(double UT)
        {
            double t = UT;
            // it is entirely possible that the 'no transition guarentee' of one OrbitTransitionHandler
            // will expire before the first predicted transition arrives. in this case, we must 
            do
            {
                // reset transition prediction memory
                NextTransition = null;
                ExpiryDate = double.PositiveInfinity;

                foreach (var transition in transitions)
                {
                    transition.CheckTransition(t);
                    // keep track of most imminent transition
                    if (transition.HasTransition && (NextTransition == null || transition.Time < NextTransition.Time))
                        NextTransition = transition;

                    // keep track of most imminent expiry date
                    ExpiryDate = Math.Min(ExpiryDate, transition.ExpiryDate);
                }

                // feed expiry date as starting point for next iteration
                t = ExpiryDate;
            } while (HasTransition && ExpiryDate < NextTransition.Time);
            
            if (HasTransition) {
                // if we've confirmed that there are no hidden transitions, set the expiry date to infinity
                ExpiryDate = double.PositiveInfinity;
                
                // update our own copy of the next orbit state
                nextOrbit.CopyFrom(NextTransition.NextOrbit);
            }

            trajectory.gameObject.SetActive(HasTransition);
        }

        /// <summary>
        /// copy the state of another patch. relationships between patches (e.g. patchOrbit, prevPatch, nextPatch) are left preserved, as are 
        /// (i.e. nextOrbit, NextTransition, and HasTransition are the only fields affected)
        /// </summary>
        public void CopyStateFrom(Patch p)
        {

        }
    }
}