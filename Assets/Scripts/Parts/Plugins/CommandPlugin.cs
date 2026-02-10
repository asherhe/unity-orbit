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
            public bool autoSteer;
        }

        private bool _autoSteer;

        /// <summary>
        /// requested steering direction
        /// </summary>
        public float SteeringInput;
        /// <summary>
        /// requested throttling direction. changes are applied to control throttle on every FixedUpdate
        /// </summary>
        public float ThrottleInput;

        public override void OnLoad(DataNode config)
        {
            base.OnLoad(config);

            _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);

            _autoSteer = _config.autoSteer;
        }

        protected override void OnFixedUpdate()
        {
            craft.Control.SteeringControl = SteeringInput;
            craft.Control.Throttle += ThrottleInput * Time.fixedDeltaTime;
        }

        public void CutThrottle() => craft.Control.Throttle = 0.0f;
        public void FullThrottle() => craft.Control.Throttle = 1.0f;
    }
}