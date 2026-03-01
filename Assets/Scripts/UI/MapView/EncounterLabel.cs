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

        [SerializeField]
        private List<Color> _colorSeries;

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
            var encColor = _colorSeries[enc.number % _colorSeries.Count];
            SetColors(encColor, encColor);
            
            var name = $"Approach {enc.number + 1}";
            if (mode == TargetingSystem.EncounterObject.Active) return $"{name}: {TextDisplay.FormatDistance(enc.encounter.Distance)}";
            else return $"{name}:Target";
        }
    }
}