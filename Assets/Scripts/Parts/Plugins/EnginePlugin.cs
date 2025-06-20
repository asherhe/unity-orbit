using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Parts
{
    public class EnginePlugin : PartPlugin
    {
        private Config _config;
        [Serializable]
        private class Config
        {
            /// <summary>
            /// engine isp, seconds
            /// </summary>
            public double isp;
            /// <summary>
            /// engine thrust, kN
            /// </summary>
            public double thrust;
            /// <summary>
            /// relative ratio of propellants used in the engine.
            /// scaled so that the total propellant mass matches the expected propellant consumption from the engine.
            /// </summary>
            public Dictionary<string, double> propellantRatio;

            public PlumeConfig plume;
            [Serializable]
            public class PlumeConfig
            {
                public Vector2d nozzlePos;
                public float nozzleSize;
                public float spreadAngle;

                // below are particle settings

                public ParticleSystem.MinMaxCurve lifetime;
                public ParticleSystem.MinMaxCurve rate;

                public StartConfig start;
                [Serializable]
                public class StartConfig
                {
                    public ParticleSystem.MinMaxCurve size;
                    public ParticleSystem.MinMaxCurve speed;
                }

                public LifeConfig life;
                [Serializable]
                public class LifeConfig
                {
                    public Gradient color;
                    public AnimationCurve size;
                }
            }
        }

        private GameObject _particleGameObject;
        private ParticleSystem _particleSystem;

        /// <summary>
        /// specific impulse, in seconds
        /// </summary>
        private double _isp;
        /// <summary>
        /// engine thrust, in newtons
        /// </summary>
        private double _thrust;
        /// <summary>
        /// direction for engine thrust in craft space, which is the local up vector of this part
        /// </summary>
        private Vector2d _thrustDir;
        /// <summary>
        /// relative ratio of propellants used in the engine.
        /// scaled so that the total propellant mass matches the expected propellant consumption from the engine.
        /// </summary>
        private Dictionary<string, double> _propRatio;
        /// <summary>
        /// total mass of propellant if _propRatio is consumed literally
        /// </summary>
        private double _propRatioMass;

        private void Awake()
        {
            _particleGameObject = new GameObject("Engine Plume");
            _particleGameObject.transform.parent = transform;
            _particleSystem = _particleGameObject.AddComponent<ParticleSystem>();
            var emission = _particleSystem.emission; emission.enabled = false;
        }

        public override void OnLoad(DataNode config)
        {
            _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);

            _isp = _config.isp;
            _thrust = _config.thrust * 1000.0; // kN -> N
            _thrustDir = Vector2d.up.Rotate(part.craftRot * 180.0 / Math.PI);

            _propRatio = new Dictionary<string, double>(_config.propellantRatio);
            _propRatioMass = 0.0;
            foreach (var kvp in _propRatio)
                _propRatioMass += kvp.Value * ResourceManager.Instance.resources[kvp.Key].density;

            _particleGameObject.transform.localPosition = _config.plume.nozzlePos;
            _particleGameObject.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);

            var main = _particleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = _config.plume.lifetime;
            main.startSpeed = _config.plume.start.speed;
            main.startSize = _config.plume.start.size;
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0.0f, 2.0f * Mathf.PI);

            var emission = _particleSystem.emission;
            emission.rateOverTime = _config.plume.rate;

            var shape = _particleSystem.shape;
            shape.enabled = true;
            shape.radius = 0.5f * _config.plume.nozzleSize;
            shape.angle = _config.plume.spreadAngle;

            var colorOverLifetime = _particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = _config.plume.life.color;

            var sizeOverLifetime = _particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, _config.plume.life.size);
        }

        public override async Task OnLoadAsync(DataNode config)
        {
            var plumeMat = Addressables.LoadAssetAsync<Material>("Assets/GameData/PartPlugins/EnginePlugin/PlumeMaterial.mat");
            await plumeMat.Task;
            var renderer = _particleGameObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = plumeMat.Result;
        }

        private void FixedUpdate()
        {
            if (craft.Control.Throttle > 0.0)
            {
                var thrust = _thrust * craft.Control.Throttle;
                craft.Newtonian.ApplyForce(thrust * _thrustDir, part.craftPos);

                var propFlow = thrust / (_isp * 9.8); // mass of propellant flow into engine
                var propFactor = propFlow / _propRatioMass; // multiplier for propellant count to drain
            }
        }

        private void Update()
        {
            var emission = _particleSystem.emission;
            if (craft.Control.Throttle > 0.0)
            {
                emission.enabled = true;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(
                    craft.Control.Throttle * _config.plume.rate.constantMin,
                    craft.Control.Throttle * _config.plume.rate.constantMax
                );
            }
            else
            {
                emission.enabled = false;
            }
        }
    }
}
