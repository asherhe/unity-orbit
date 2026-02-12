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

        protected override void UpdateVisuals()
        {
            base.UpdateVisuals();
        }
    }
}
