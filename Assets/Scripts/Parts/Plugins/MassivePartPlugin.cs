using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parts
{
    /// <summary>
    /// a PartPlugin that has mass. NOT a part plugin that's really big lol
    /// </summary>
    public class MassivePartPlugin : PartPlugin
    {
        private double _mass = 0.0;
        public double Mass
        {
            get => _mass;
            set {
                var massChange = value - _mass;
                _mass = value;
                OnMassChanged?.Invoke(massChange);
            }
        }
        /// <summary>
        /// invoked when the mass of this MassivePartPlugin is changed.
        /// event argument is the change in mass
        /// </summary>
        public event Action<double> OnMassChanged;
    }
}