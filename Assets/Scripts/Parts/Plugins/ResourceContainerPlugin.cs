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
            public Dictionary<string, double> amount, maxAmount;
            public Dictionary<string, int> priority;
        }

        public class Resource
        {
            /// <summary>
            /// internal name of resource that this Resource describes
            /// </summary>
            public string type;

            public double amount = 0.0;
            public double maxAmount;

            /// <summary>
            /// resource containers with a higher priority on a certain resource will be drained faster than 
            /// </summary>
            public int priority = 0;
        }
        public Dictionary<string, Resource> resources = new();

        public double GetAmount(string type) => resources[type].amount;
        public double GetMaxAmount(string type) => resources[type].maxAmount;
        public int GetPriority(string type) => resources[type].priority;

        /// <summary>
        /// drain some resource from this tank
        /// </summary>
        /// <param name="type">internal name of the resource type</param>
        /// <param name="amt">amount to drain</param>
        public void Drain(string type, double amt)
        {
            var res = resources[type];
            amt = Math.Min(res.amount, amt);
            res.amount -= amt;

            Mass -= amt * ResourceManager.GetDensity(type);
        }

        public override void OnLoad(DataNode config)
        {
            _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);

            double mass = 0.0;
            resources.Clear();
            foreach (var kvp in _config.maxAmount)
            {
                var r = new Resource();
                r.type = kvp.Key;
                r.amount = _config.amount[kvp.Key];
                r.maxAmount = kvp.Value;
                r.priority = _config.priority[kvp.Key];
                resources.Add(r.type, r);

                mass += r.amount * ResourceManager.GetDensity(r.type);
            }
            Mass = mass;
        }
    }
}