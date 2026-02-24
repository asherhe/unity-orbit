using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public class EncounterLabelGroup : MonoBehaviour
    {
        /// <summary>
        /// labels for the active craft and the targeted object
        /// </summary>
        [SerializeField]
        private EncounterLabel _activeLabel, _targetLabel;

        [HideInInspector]
        public RectTransform rectTransform;

        private TargetingSystem.TargetEncounter _encounter;
        public TargetingSystem.TargetEncounter Encounter
        {
            get => _encounter;
            set
            {
                _encounter = value;
                UpdateEncounterLabels();
            }
        }

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private void UpdateEncounterLabels()
        {
            _activeLabel.SetEncounter(Encounter, TargetingSystem.EncounterObject.Active);
            _targetLabel.SetEncounter(Encounter, TargetingSystem.EncounterObject.Target);
        }
    }
}