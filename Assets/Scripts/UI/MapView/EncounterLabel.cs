using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Orbit.EncounterCalculator;

namespace UI
{
    public class EncounterLabel : OrbitPOILabel
    {
        [Serializable]
        private struct DisplayPrefs
        {
            public Sprite icon;
            public Color color;
        }
        [SerializeField]
        private List<DisplayPrefs> _displayPrefs = new();

        private TargetingSystem.EncounterObject mode;
        private TargetingSystem.TargetEncounter enc;
        private StateVectors state;

        public void SetEncounter(TargetingSystem.TargetEncounter enc, TargetingSystem.EncounterObject mode)
        {
            DisplayPrefs prefs = _displayPrefs[enc.number];

            iconImage.sprite = prefs.icon;
            SetColors(iconColor, prefs.color);

            this.mode = mode;
            this.enc = enc;
            if (mode == TargetingSystem.EncounterObject.Active)
            {
                state = enc.encounter.state;
                Orbit = enc.encounter.orbit;
            }
            else
            {
                state = enc.encounter.otherState;
                Orbit = enc.encounter.other;
            }
            RefreshLabel();
        }

        protected override Vector2d GetPosition()
        {
            return state.pos;
        }
        protected override string GetLabelText()
        {
            var name = $"Encounter {enc.number + 1}";
            if (mode == TargetingSystem.EncounterObject.Active) return $"{name}:{TextDisplay.AddMetricPrefix(enc.encounter.Distance)}m";
            else return $"{name}:Target";
        }
    }
}