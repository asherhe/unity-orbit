using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;

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
                foreach (var enc in encs) _encounters.AddLast(new TargetEncounter(patch, i++, enc));
            }
        }

        OnEncounterUpdate?.Invoke();
    }

    private void FixedUpdate()
    {
        // eliminate patches we've already passed
        var t = Universe.Instance.UT;
        bool hasPassedEncounter = false;
        while (_encounters.Count > 0 && Universe.Instance.UT >= (t = _encounters.First.Value.encounter.state.time))
        {
            _encounters.RemoveFirst();
            hasPassedEncounter = true;
        }
        if (hasPassedEncounter)
        {
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
                var tStart = Universe.Instance.UT;
                var tEnd = Universe.Instance.UT + 1.5 * currPatch.patchOrbit.period; // often times the next approach is just over one period past the previous one, the 1.5 accounts for this

                if (tEnd > currPatch.ExpiryDate)
                {
                    // we can't be certain that there are no transitions between now and the ending time bound
                    // nudge the prediction a little further to see if it really does work
                    patchManager.Update(Universe.Instance.UT);
                    // this automatically triggers a recalculation of target encounters so we don't need to invoke OnEncounterUpdate
                }
                else
                {
                    // encounter time bound is guarenteed to be transition-free
                    // append new transitions
                    var encCalc = new EncounterCalculator(currPatch.patchOrbit);
                    var encs = encCalc.GetEncounters(Target.orbit, tStart, tEnd);
                    foreach (var enc in encs) _encounters.AddLast(new TargetEncounter(currPatch, i++, enc));
                    OnEncounterUpdate?.Invoke();
                }
            }
        }
    }
}