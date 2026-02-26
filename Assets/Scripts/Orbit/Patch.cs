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
        public readonly PatchedConicManager manager;

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
        /// the OrbitState (of the first patch) that this patch is tied to
        /// </summary>
        private readonly OrbitState rootOrbit;

        /// <summary>
        /// all transition handlers that can update this orbit patch from the previous one
        /// </summary>
        public readonly List<OrbitTransitionHandler> transitions = new();

        // specific orbit transition handlers
        public SOIEscapeTransition soiEscape;
        public SOIInterceptTransition soiIntercept;


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
        /// invoked when transition data is updated
        /// </summary>
        public event Action OnTransitionUpdate;

        /// <summary>
        /// trajectory display for this patch.
        /// </summary>
        public UI.Trajectory trajectory;
        /// <summary>
        /// map view labels for patch apses
        /// </summary>
        private UI.ApsisLabel periapsisLabel, apoapsisLabel;
        private UI.SOITransitionLabel soiEnterLabel, soiExitLabel;

        /// <summary>
        /// how far removed this Patch is from the original, current OrbitState.
        /// equal to zero if this Patch represents the current orbit.
        /// </summary>
        private readonly int _patchStep;

        private bool _isActive = false;
        /// <summary>
        /// the active/inactive state of this patch
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                trajectory.gameObject.SetActive(_isActive);
                periapsisLabel.IsActive = _isActive;
                apoapsisLabel.IsActive = _isActive;
                soiEnterLabel.IsActive = _isActive;
                soiExitLabel.IsActive = _isActive;
            }
        }

        /// <summary>
        /// construct a patch based on the orbit state of an orbiting body.
        /// the resulting Patch is directly linked to the current state of the body.
        /// </summary>
        public Patch(OrbitState srcOrbit, PatchedConicManager manager)
        {
            this.manager = manager;
            patchOrbit = srcOrbit;
            nextOrbit = new(patchOrbit);
            prevPatch = null;
            rootOrbit = srcOrbit;

            _patchStep = 0;

            SetupPatch();
        }

        /// <summary>
        /// construct a new Patch that follows the given patch
        /// </summary>
        public Patch(Patch patch, PatchedConicManager manager)
        {
            this.manager = manager;
            patchOrbit = patch.nextOrbit;
            nextOrbit = new(patchOrbit);
            prevPatch = patch;
            if (patch.nextPatch != null) throw new InvalidOperationException("provided preceding patch already has a subsequent patch.");
            patch.nextPatch = this;
            rootOrbit = patch.rootOrbit;

            _patchStep = patch._patchStep + 1;

            SetupPatch();

            trajectory.AlphaRange = new Vector2(0.1f, 0.5f);
        }

        private void SetupPatch()
        {
            soiEscape = new(patchOrbit);
            soiIntercept = new(patchOrbit, soiEscape);

            transitions.Add(soiEscape);
            transitions.Add(soiIntercept);

            NextTransition = null;
            ExpiryDate = double.PositiveInfinity;

            trajectory = UI.TrajectoryManager.Instance.AddTrajectory(patchOrbit);
            trajectory.name = $"Trajectory {rootOrbit.owner} (Patch {_patchStep})";

            UI.MapLabelManager.WhenInstantiated(() =>
            {
                periapsisLabel = UI.MapLabelManager.Instance.AddApsis(patchOrbit, UI.ApsisLabel.DisplayMode.Periapsis);
                apoapsisLabel = UI.MapLabelManager.Instance.AddApsis(patchOrbit, UI.ApsisLabel.DisplayMode.Apoapsis);
                periapsisLabel.trajectory = apoapsisLabel.trajectory = trajectory;

                soiEnterLabel = UI.MapLabelManager.Instance.AddSOITransition(this, UI.SOITransitionLabel.DisplayMode.Enter);
                soiExitLabel = UI.MapLabelManager.Instance.AddSOITransition(this, UI.SOITransitionLabel.DisplayMode.Exit);

                manager.OnTransition += () =>
                {
                    bool hasNext = nextPatch != null;
                    periapsisLabel.IsTextActive = hasNext && nextPatch.periapsisLabel.IsTextActive;
                    apoapsisLabel.IsTextActive = hasNext && nextPatch.apoapsisLabel.IsTextActive;
                    soiEnterLabel.IsTextActive = hasNext && nextPatch.soiEnterLabel.IsTextActive;
                    soiExitLabel.IsTextActive = hasNext && nextPatch.soiExitLabel.IsTextActive;
                };
            });

            OnTransitionUpdate += UpdateTrajectoryPrefs;
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

            if (HasTransition)
            {
                // if we've confirmed that there are no hidden transitions, set the expiry date to infinity
                ExpiryDate = double.PositiveInfinity;

                // update our own copy of the next orbit state
                nextOrbit.CopyFrom(NextTransition.NextOrbit);
            }

            OnTransitionUpdate?.Invoke();
        }

        /// <summary>
        /// reset the display preferences (clipping bounds, etc.) on the currently displayed conic patch based on encounters and SOI captures
        /// </summary>
        public void UpdateTrajectoryPrefs()
        {
            trajectory.clipToCurrent = false;

            // orbit direction
            var dir = Math.Sign(patchOrbit.h);
            // true anomalies, timewise - nu1 happens first, then nu2
            double nu1 = dir * double.NegativeInfinity, nu2 = dir * double.PositiveInfinity;

            // adjust nu bounds, accounting for orbit direction
            double SetNu1(double nu) => nu1 = dir == 1 ? Math.Max(nu1, nu) : Math.Min(nu1, nu);
            double SetNu2(double nu) => nu2 = dir == 1 ? Math.Min(nu2, nu) : Math.Max(nu2, nu);

            if (HasTransition)
            {
                // clip to SOI capture
                if (soiEscape.HasTransition)
                {
                    var nuCapt = patchOrbit.CalcNu(soiEscape.SOICapture?.pos);
                    SetNu1(nuCapt);
                }
                // clip to entry point of this patch
                if (prevPatch != null)
                {
                    var nuEnter = patchOrbit.CalcNu(prevPatch.NextTransition.NextState.pos);
                    SetNu1(nuEnter);
                }
                // this is the first patch: clip to the current position
                if (prevPatch == null && NextTransition != soiEscape)
                {
                    trajectory.clipToCurrent = true;
                }
                // clip to exit point
                var nuTrans = patchOrbit.CalcNu(NextTransition.State.pos);
                SetNu2(nuTrans);
            }

            // normalize nu2 to be on the correct side of nu1
            // if nu1 was never assigned, use the orbit's nu0 as a reference point
            // (throughout the game we assume that state at epoch represents a real state on the patch
            // and not some random point that is on a different period)
            var nuRef = nu1;
            if (double.IsInfinity(nuRef)) nuRef = patchOrbit.nu0;
            if (dir * nu2 < dir * nuRef) nu2 += dir * 2 * Math.PI;

            trajectory.nu1 = nu1; trajectory.nu2 = nu2;

            trajectory.UpdateVisuals();
        }

        public override string ToString()
        {
            return $"[{rootOrbit}: Patch {_patchStep + 1}]";
        }
    }
}