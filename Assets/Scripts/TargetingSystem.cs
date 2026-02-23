using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
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

    private PatchedConicManager activePatches;

    public struct TargetEncounter
    {
        /// <summary>
        /// conic patch in which encounter occurs
        /// </summary>
        public Patch patch;
        /// <summary>
        /// data about the encounter from EncounterCalculator
        /// </summary>
        public EncounterCalculator.Encounter encounter;

        public TargetEncounter(Patch patch, EncounterCalculator.Encounter encounter)
        {
            this.patch = patch; this.encounter = encounter;
        }
    }
    /// <summary>
    /// all encounters with the target
    /// </summary>
    List<TargetEncounter> encounters;
    /// <summary>
    /// invoked once new encounter data has been determined
    /// </summary>
    public event Action OnEncounterUpdate;

    protected override void Awake()
    {
        base.Awake();

        ActiveCraftController.WhenInstantiated(() => {
            var activeCraft = ActiveCraftController.Instance.craft;
            activeCraft.OnLoaded += () =>
            {
                activePatches = activeCraft.patches;
                activePatches.OnRecalculated += RecalculateEncounters;
            };
        });

        OnTargetChanged += () => AnnouncementDisplay.Instance.Announce(Target == null ? "Targeting Cancelled" : $"Targeting {Target.gameObject.name}");
    }

    private void RecalculateEncounters()
    {
        if (Target == null) return;
        encounters = new List<TargetEncounter>();
        foreach (var patch in activePatches.Patches)
        {
            if (patch.patchOrbit.body != Target.orbit.body) continue;

            var (tStart, tEnd) = EncounterCalculator.CalcTBounds(patch.patchOrbit, Universe.Instance.UT, patch.soiEscape);
            
            var encCalc = new EncounterCalculator(patch.patchOrbit);
            var encs = encCalc.GetEncounters(Target.orbit, tStart, tEnd);
            foreach (var encounter in encs)
                encounters.Add(new(patch, encounter));
        }

        OnEncounterUpdate?.Invoke();
    }
}
