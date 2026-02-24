using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI
{
    public class EncounterLabel : OrbitPOILabel
    {
        private TargetingSystem.EncounterObject mode;
        private TargetingSystem.TargetEncounter enc;
        private StateVectors state;

        private TMP_Text _iconText;

        protected override void Awake()
        {
            base.Awake();
            _iconText = icon.iconGraphic as TMP_Text;
        }

        public void SetEncounter(TargetingSystem.TargetEncounter enc, TargetingSystem.EncounterObject mode)
        {
            _iconText.text = (enc.number + 1).ToString();

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
            MatchTrajectoryColor();

            var name = $"Encounter {enc.number + 1}";
            if (mode == TargetingSystem.EncounterObject.Active) return $"{name}={TextDisplay.AddMetricPrefix(enc.encounter.Distance)}m";
            else return $"{name}:Target";
        }
    }
}