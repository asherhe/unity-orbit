using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parts
{
    public class ResourceContainerPlugin : MassivePartPlugin
    {
        private Config _config;
        [Serializable]
        private class Config
        {
            public Dictionary<string, double> amount, maxAmount, priority;
        }

        public override void OnLoad(DataNode config)
        {
            _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);

            double mass = 0.0;
            foreach (var kvp in _config.amount)
                mass += kvp.Value * ResourceManager.Instance.resources[kvp.Key].density;
            Mass = mass;
        }
    }
}