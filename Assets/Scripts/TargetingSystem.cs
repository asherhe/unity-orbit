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

    private List<TargetEncounter> _encounters = new();
    /// <summary>
    /// all encounters with the target
    /// </summary>
    public IEnumerable<TargetEncounter> Encounters { get => _encounters.AsReadOnly(); }
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
                activePatches = activeCraft.patches;
                activePatches.OnRecalculated += RecalculateEncounters;
            };
        });

        OnTargetChanged += () => AnnouncementDisplay.Instance.Announce(Target == null ? "Targeting Cancelled" : $"Targeting {Target.gameObject.name}");
        OnTargetChanged += RecalculateEncounters;

        gameObject.AddComponent<UI.TargetEncounterLabels>();
    }

    private void RecalculateEncounters()
    {
        if (Target == null) return;

        _encounters.Clear();
        foreach (var patch in activePatches.ActivePatches)
        {
            if (patch.patchOrbit.body != Target.orbit.body) continue;

            var UT = patch.prevPatch == null ? Universe.Instance.UT : patch.prevPatch.NextTransition.Time;
            var (tStart, tEnd) = EncounterCalculator.CalcTBounds(patch.patchOrbit, UT, patch.soiEscape);
            var encCalc = new EncounterCalculator(patch.patchOrbit);
            var encs = encCalc.GetEncounters(Target.orbit, tStart, tEnd);
            for (int i = 0; i < encs.Count; i++)
                _encounters.Add(new(patch, i, encs[i]));
        }

        OnEncounterUpdate?.Invoke();
    }
}
