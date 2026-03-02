using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parts
{
    /// <summary>
    /// an object that has mass. NOT a part plugin that's really big lol
    /// </summary>
    public interface IMassive
    {
        public MassProperty MassProperty { get; }
    }

    public class MassProperty
    {
        private double _mass = 0.0;
        public double Mass
        {
            get => _mass;
            set
            {
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

        private List<MassProperty> children = new();

        private void UpdateMass(double massChange) => Mass += massChange;

        /// <summary>
        /// register a child MassProperty that comprises a part of this MassProperty
        /// </summary>
        public void AddMassProperty(MassProperty prop)
        {
            children.Add(prop);
            prop.OnMassChanged += UpdateMass;
        }

        /// <summary>
        /// register a child MassProperty that comprises a part of this MassProperty
        /// </summary>
        public void RemoveMassProperty(MassProperty prop)
        {
            children.Remove(prop);
            prop.OnMassChanged -= UpdateMass;
        }
    }
}