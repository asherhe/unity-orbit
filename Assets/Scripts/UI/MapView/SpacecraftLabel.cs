using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class SpacecraftLabel : ObjectLabel
    {
        /// <summary>
        /// Owner as a CelestialBody
        /// </summary>
        private Spacecraft _craft;

        public override string Name => _craft.craftName;

        [SerializeField]
        private RectTransform _headingIndicator;

        protected override void Awake()
        {
            // run this before base.awake so that colors are updated before UpdateVisuals()
            OnOwnerUpdated += () =>
            {
                _craft = (Spacecraft)Owner;
                labelText.text = _craft.craftName;
            };
            base.Awake();
        }

        private void Update()
        {
            if (_craft == null) return;
            var heading = (float)_craft.Newtonian.angle * Mathf.Rad2Deg;
            // heading texture is offset by 45 degrees
            _headingIndicator.localEulerAngles = Vector3.forward * (heading + 45.0f);
        }
    }
}
