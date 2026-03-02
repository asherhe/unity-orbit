using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ManeuverTimeField : ManeuverField
    {
        [SerializeField]
        private Button _buttonNextOrbit, _buttonPrevOrbit;

        // TODO
        /// <summary>
        /// orbital period of current maneuver orbit
        /// </summary>
        private float Period => 0f;

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