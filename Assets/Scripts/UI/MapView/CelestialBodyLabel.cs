using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class CelestialBodyLabel : MapLabel
    {
        [SerializeField]
        private Image _icon;
        [SerializeField]
        private TMP_Text _labelText;

        /// <summary>
        /// Owner as a CelestialBody
        /// </summary>
        private CelestialBody _body;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void UpdateVisuals()
        {
            base.UpdateVisuals();
            _body = (CelestialBody)Owner;

            _icon.color = _body.color;

            _labelText.text = _body.bodyName;
            _labelText.color = _body.color;
        }
    }
}