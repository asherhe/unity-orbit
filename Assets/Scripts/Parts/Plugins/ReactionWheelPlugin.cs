using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parts
{
    public class ReactionWheelPlugin : PartPlugin
    {
        private Config _config;
        [Serializable]
        private class Config
        {
            /// <summary>
            /// maximum torque provided by reaction wheel, in kN m
            /// </summary>
            public double torque;
        }

        private double _torque;

        public override void OnLoad(DataNode config)
        {
            base.OnLoad(config);

            _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);
            _torque = _config.torque * 1000.0;

            // TODO: this is a bit of a hack, implement a more complete system for tallying mass, torque, thrust, etc.
            craft.Control.maxTorque += _torque;
        }

        protected override void OnFixedUpdate()
        {
            if (craft.Control.SteeringControl == 0) return;
            var torque = _torque * craft.Control.SteeringControl;
            craft.Newtonian.ApplyTorque(torque);
        }
    }
}
