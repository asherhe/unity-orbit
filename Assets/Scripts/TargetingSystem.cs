using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;
using static Orbit.EncounterCalculator;

public class TargetingSystem : SingletonBehaviour<TargetingSystem>
{
    private OrbitingObject _target;
    /// <summary>
    /// currently active targeted object. null if no target is active
    /// </summary>
    public OrbitingObject Target
    {
        get => _target;
        set
        {
            if (_target == value) return;
            _target = value;
            OnTargetChanged?.Invoke();
        }
    }
    /// <summary>
    /// invoked when the targeted object changes
    /// </summary>
    public event Action OnTargetChanged;

    private PatchedConicManager patchManager;

    public struct TargetEncounter
    {
        /// <summary>
        /// conic patch in which encounter occurs
        /// </summary>
        public Patch patch;
        /// <summary>
        /// which encounter of the patch is this? (0 is first encounter to happen)
        /// </summary>
        public int number;
        /// <summary>
        /// data about the encounter from EncounterCalculator
        /// </summary>
        public EncounterCalculator.Encounter encounter;

        public TargetEncounter(Patch patch, int number, EncounterCalculator.Encounter encounter)
        {
            this.patch = patch;
            this.number = number;
            this.encounter = encounter;
        }
    }

    public enum EncounterObject { Active, Target }

    private LinkedList<TargetEncounter> _encounters = new();
    /// <summary>
    /// all encounters with the target
    /// </summary>
    public IEnumerable<TargetEncounter> Encounters { get => _encounters; }

    /// <summary>
    /// the time at which we need to recalculate target appraoches
    /// </summary>
    private double approachExpiryTime = double.PositiveInfinity;

    /// <summary>
    /// invoked once new encounter data has been determined
    /// </summary>
    public event Action OnEncounterUpdate;

    protected override void Awake()
    {
        base.Awake();

        ActiveCraftController.WhenInstantiated(() =>
        {
            var activeCraft = ActiveCraftController.Instance.craft;
            activeCraft.OnLoaded += () =>
            {
                patchManager = activeCraft.patches;
                patchManager.OnRecalculated += RecalculateEncounters;
            };
        });

        OnTargetChanged += () => AnnouncementDisplay.Instance.Announce(Target == null ? "Targeting Cancelled" : $"Targeting {Target.gameObject.name}");
        OnTargetChanged += RecalculateEncounters;

        gameObject.AddComponent<UI.TargetEncounterLabels>();
    }

    private void RecalculateEncounters()
    {
        _encounters.Clear();
        approachExpiryTime = double.PositiveInfinity;

        if (Target != null)
        {
            int i = 0;
            foreach (var patch in patchManager.ActivePatches)
            {
                if (patch.patchOrbit.body != Target.orbit.body) continue;

                var UT = patch.prevPatch == null ? Universe.Instance.UT : patch.prevPatch.NextTransition.Time;
                var (tStart, tEnd) = EncounterCalculator.CalcTBounds(patch.patchOrbit, UT, patch.soiEscape);
                if (patch.HasTransition) tEnd = Math.Min(tEnd, patch.NextTransition.Time);

                var encCalc = new EncounterCalculator(patch.patchOrbit);
                var encs = encCalc.GetEncounters(Target.orbit, tStart, tEnd);

                approachExpiryTime = Math.Min(tEnd, approachExpiryTime);
                foreach (var enc in encs) _encounters.AddLast(new TargetEncounter(patch, i++, enc));

            }

            if (_encounters.Count > 0) approachExpiryTime = _encounters.First.Value.encounter.state.time;
        }

        OnEncounterUpdate?.Invoke();
    }

    /// <summary>
    /// advance encounter data if necessary. should be called within FixedUpdate()
    /// </summary>
    private void UpdateEncounters(double UT)
    {
        if (patchManager == null) return;
        if (UT < approachExpiryTime) return;

        // eliminate patches we've already passed
        while (_encounters.Count > 0 && UT >= _encounters.First.Value.encounter.state.time)
            _encounters.RemoveFirst();

        // reassign encounter numbers
        int i = 0;
        var node = _encounters.First;
        while (node != null)
        {
            var encounter = node.Value;
            encounter.number = i++;
            node.Value = encounter;
            node = node.Next;
        }

        var currPatch = patchManager.FirstPatch;
        if (currPatch.HasTransition || currPatch.patchOrbit.Shape != OrbitShape.Ellipse)
        {
            // the current patch is not stable, we know that all approaches have been accounted for
            OnEncounterUpdate?.Invoke();
        }
        else
        {
            // this is an elliptical, noninturrupted orbit for the forseeable future
            var tStart = UT;
            var tEnd = UT + 1.5 * currPatch.patchOrbit.period; // often times the next approach is just over one period past the previous one, the 1.5 accounts for this

            // we can't be certain that there are no transitions between now and the ending time bound
            if (currPatch.ExpiryDate < tEnd)
            {
                // advance transition preduction until it is safe
                while (currPatch.ExpiryDate < tEnd)
                    currPatch.CheckTransitions(currPatch.ExpiryDate);

                if (currPatch.HasTransition)
                {
                    // recalculate transitions if we detect one to
                    // this automatically triggers a recalculation of target encounters so we don't need to invoke OnEncounterUpdate
                    patchManager.RecalculatePatches(currPatch.ExpiryDate);
                    return;
                }
            }

            // encounter time bound is guarenteed to be transition-free
            // append new transitions
            var encCalc = new EncounterCalculator(currPatch.patchOrbit);
            var encs = encCalc.GetEncounters(Target.orbit, tStart, tEnd);
            foreach (var enc in encs) _encounters.AddLast(new TargetEncounter(currPatch, i++, enc));

            approachExpiryTime = tEnd;
            if (_encounters.Count > 0) approachExpiryTime = _encounters.First.Value.encounter.state.time;

            OnEncounterUpdate?.Invoke();
        }
    }

    private void FixedUpdate()
    {
        UpdateEncounters(Universe.Instance.UT);
    }
}