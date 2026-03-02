using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parts
{
    /// <summary>
    /// a PartPlugin that supplies thrust or torque based on craft input
    /// </summary>
    public interface IActuator
    {
        public ActuatorProperties ActuatorProperties { get; }
    }

    public class ActuatorProperties
    {
        /// <summary>
        /// thrust achieved when all thrusters are firing
        /// </summary>
        public Vector2d maxThrust = Vector2d.zero;
        /// <summary>
        /// specific impulse equivalent when all thrusters are firing
        /// </summary>
        public double isp = 0.0;

        /// <summary>
        /// maximum torque that can be generated with current control parameters, varying only steering control
        /// (note that this is not the torque at max throttle for thrust-vectored engines!)
        /// </summary>
        public double maxTorque = 0.0;

        public event Action OnPropertiesUpdated;

        /// <summary>
        /// whether this represents the combined effective of several actuators and not just a single one
        /// </summary>
        public bool IsCollection { get; private set; } = false;

        private List<ActuatorProperties> children = new();

        public void AddActuator(ActuatorProperties actuator)
        {
            IsCollection = true;
            children.Add(actuator);
            actuator.OnPropertiesUpdated += UpdateState;
            UpdateState();
        }

        public void RemoveActuator(ActuatorProperties actuator)
        {
            children.Remove(actuator);
            actuator.OnPropertiesUpdated -= UpdateState;
            UpdateState();
        }

        public void UpdateState()
        {
            if (IsCollection)
            {
                maxThrust = Vector2d.zero;
                isp = 0.0;
                maxTorque = 0.0;

                foreach (var child in children)
                {
                    if (child.maxThrust != Vector2d.zero)
                    {
                        maxThrust += child.maxThrust;
                        isp += child.maxThrust.Magnitude / child.isp;
                    }
                    maxTorque += child.maxTorque;
                }

                // convert from net weight flow (F / Isp) back to Isp
                isp = maxThrust.Magnitude / isp;
            }

            OnPropertiesUpdated?.Invoke();
        }
    }
}