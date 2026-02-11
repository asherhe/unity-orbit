using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Parts
{
    public class CommandPlugin : PartPlugin
    {
        private Config _config;
        [Serializable]
        private class Config
        {
            /// <summary>
            /// whether or not this CommandPlugin instance is allowed to autosteer (analogous to kerbal's SAS)
            /// </summary>
            public bool autoSteerable = false;

            // PID parameters
            public float Kp, Ki, Kd;
        }

        /// <summary>
        /// whether or not this command pod can be autosteered
        /// </summary>
        public bool autoSteerable { get; private set; }

        private bool _isAutoSteerEnabled = false;
        public bool IsAutoSteerEnabled
        {
            get => _isAutoSteerEnabled;
            set
            {
                if (!autoSteerable) return;
                if (_isAutoSteerEnabled == value) return;
                _isAutoSteerEnabled = value;
                OnAutoSteerToggled?.Invoke();
            }
        }

        public event Action OnAutoSteerToggled;

        /// <summary>
        /// requested steering direction
        /// </summary>
        public float SteeringInput;
        /// <summary>
        /// requested throttling direction. changes are applied to control throttle on every FixedUpdate
        /// </summary>
        public float ThrottleInput;

        /// <summary>
        /// target direction for autosteer in radians
        /// </summary>
        public float autosteerTarget;

        // autosteer PID parameters
        public float Kp, Ki, Kd;

        /// <summary>
        /// previous PID error
        /// </summary>
        private float _prevError;
        /// <summary>
        /// accumulated error since the last time we reached target
        /// </summary>
        private float _integralError;

        public override void OnLoad(DataNode config)
        {
            base.OnLoad(config);

            _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);

            autoSteerable = _config.autoSteerable;
        }

        protected override void OnFixedUpdate()
        {
            // no control allowed in high timewarp
            if (Universe.Instance.Timewarp.TimewarpScale > 50.0)
            {
                craft.Control.SteeringControl = 0;
                craft.Control.Throttle = 0;
                return;
            }

            craft.Control.SteeringControl = SteeringInput;
            craft.Control.Throttle += ThrottleInput * Time.fixedDeltaTime;

            // autosteer override
            if (IsAutoSteerEnabled && SteeringInput == 0)
            {
                var error = Mathf.Deg2Rad * Mathf.DeltaAngle((float)craft.Newtonian.angle * Mathf.Rad2Deg, autosteerTarget * Mathf.Rad2Deg);
                var derivative = (error - _prevError) / Time.fixedDeltaTime;
                // reset integral if we've reached target
                if (error * _prevError < 0.0f) _integralError = 0.0f;
                _integralError += error * Time.fixedDeltaTime;

                var output = Kp * error + Ki * _integralError + Kd * derivative;

                // adjust steering output to moment of inertia so that the same parameters work regardless of craft properties
                // TODO: adjust for spacecraft's usable torque too
                craft.Control.SteeringControl = (float)craft.Newtonian.momentOfInertia * output;

                _prevError = error;
            }
        }

        public void CutThrottle() => craft.Control.Throttle = 0.0f;
        public void FullThrottle() => craft.Control.Throttle = 1.0f;
    }
}