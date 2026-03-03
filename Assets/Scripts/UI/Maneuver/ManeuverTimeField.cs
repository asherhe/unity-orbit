using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ManeuverTimeField : ManeuverField
    {
        [SerializeField]
        private ManeuverPlanner _planner;

        [SerializeField]
        private Button _buttonNextOrbit, _buttonPrevOrbit;

        /// <summary>
        /// whether or not the source orbit of the maneuver has a relevant orbital period
        /// </summary>
        private bool HasPeriod => !_planner.maneuver.SourcePatch.soiEscape.HasTransition;

        /// <summary>
        /// orbital period of current maneuver orbit
        /// </summary>
        private float Period => (float)_planner.maneuver.SourcePatch.patchOrbit.period;

        protected override void Awake()
        {
            base.Awake();

            _planner.OnManeuverStateUpdate += UpdateOrbitButtonInteractability;
        }

        private void UpdateOrbitButtonInteractability()
        {
            var interactable = HasPeriod;
            _buttonNextOrbit.interactable = interactable;
            _buttonPrevOrbit.interactable = interactable;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _buttonNextOrbit.onClick.AddListener(NextOrbit);
            _buttonPrevOrbit.onClick.AddListener(PrevOrbit);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _buttonNextOrbit.onClick.RemoveListener(NextOrbit);
            _buttonPrevOrbit.onClick.RemoveListener(PrevOrbit);
        }

        private void NextOrbit() => IncrementBy(Period);
        private void PrevOrbit() => IncrementBy(-Period);
    }
}