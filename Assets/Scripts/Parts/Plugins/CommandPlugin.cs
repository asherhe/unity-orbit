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
            public float Kp = 3.54f, Ki = 0.12f, Kd = 3.12f;
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
        private float _prevError = float.NaN;
        /// <summary>
        /// accumulated error since the last time we reached target
        /// </summary>
        private float _integralError = 0.0f;

        public override void OnLoad(DataNode config)
        {
            base.OnLoad(config);

            _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);

            autoSteerable = _config.autoSteerable;
            Kp = _config.Kp; Ki = _config.Ki; Kd = _config.Kd;

            OnAutoSteerToggled += () => { _prevError = float.NaN; _integralError = 0.0f; };
        }

        protected override void OnFixedUpdate()
        {
            // no control allowed in high timewarp
            if (Universe.Instance.Timewarp.TimewarpScale > 5.0)
            {
                craft.Control.SteeringControl = 0;
                if (Universe.Instance.Timewarp.TimewarpScale > 50.0) craft.Control.Throttle = 0;
                return;
            }

            craft.Control.SteeringControl = SteeringInput;
            craft.Control.Throttle += ThrottleInput * Time.fixedDeltaTime;

            // autosteer override
            if (IsAutoSteerEnabled && SteeringInput == 0)
            {
                var error = Mathf.Deg2Rad * Mathf.DeltaAngle((float)craft.Newtonian.angle * Mathf.Rad2Deg, autosteerTarget * Mathf.Rad2Deg);
                if (float.IsNaN(_prevError)) _prevError = error;
                var derivative = (error - _prevError) / Time.fixedDeltaTime;

                var output = Kp * error + Ki * _integralError + Kd * derivative;

                // adjust steering output to moment of inertia so that the same parameters work regardless of craft properties
                // TODO: adjust for spacecraft's usable torque too
                craft.Control.SteeringControl = (float)(craft.Newtonian.momentOfInertia / craft.Control.maxTorque) * output;

                // reset integral if we've reached target
                if (error * _prevError < 0.0f) _integralError = 0.0f;
                // only accumulate if steering is not maxed out (to prevent overcompensation of integral term)
                if (Mathf.Abs(craft.Control.SteeringControl) < 1.0f) _integralError += error * Time.fixedDeltaTime;

                _prevError = error;
                this.error = error; this.derivative = derivative; integral = _integralError;
            }
        }

        public float error, derivative, integral;

        public void CutThrottle() => craft.Control.Throttle = 0.0f;
        public void FullThrottle() => craft.Control.Throttle = 1.0f;
    }
}