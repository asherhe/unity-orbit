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
        /// relative ratio of propellants used in the engine.
        /// scaled so that the total propellant mass matches the expected propellant consumption from the engine.
        /// </summary>
        private Dictionary<string, double> _propRatio;
        /// <summary>
        /// direction for engine thrust in craft space
        /// </summary>
        private Vector2d _thrustDir;

        private void Awake()
        {
            _particleGameObject = new GameObject("Engine Plume");
            _particleGameObject.transform.parent = transform;
            _particleSystem = _particleGameObject.AddComponent<ParticleSystem>();
            var emission = _particleSystem.emission; emission.enabled = false;
        }

        public override void OnLoad(DataNode config)
        {
            var deserializer = new DataNodeDeserializer();
            deserializer.AddDeserializer(
                typeof(ParticleSystem.MinMaxCurve),
                node => new ParticleSystem.MinMaxCurve(node[0].As<float>(), node[1].As<float>())
            );
            deserializer.AddDeserializer(
                typeof(AnimationCurve),
                node =>
                {
                    var curve = new AnimationCurve();
                    foreach (var kvp in node.KeyValuePairs)
                    {
                        float time, value = kvp.Value.As<float>();
                        if (!float.TryParse(kvp.Key, out time))
                            throw new FormatException($"Could not parse AnimationCurve time {kvp.Value} as a float.");
                        curve.AddKey(time, value);
                    }
                    return curve;
                }
            );
            deserializer.AddDeserializer(
                typeof(Gradient),
                node =>
                {
                    var colorNodes = node["colorKeys"];
                    var colorKeys = new GradientColorKey[colorNodes.Count];
                    var i = 0;
                    foreach (var kvp in colorNodes.KeyValuePairs)
                    {
                        if (!float.TryParse(kvp.Key, out colorKeys[i].time))
                            throw new FormatException($"Could not parse AnimationCurve time {kvp.Value} as a float.");
                        colorKeys[i].color = kvp.Value.As<Color>();
                        i++;
                    }

                    var alphaNodes = node["alphaKeys"];
                    var alphaKeys = new GradientAlphaKey[alphaNodes.Count];
                    i = 0;
                    foreach (var kvp in alphaNodes.KeyValuePairs)
                    {
                        if (!float.TryParse(kvp.Key, out alphaKeys[i].time))
                            throw new FormatException($"Could not parse AnimationCurve time {kvp.Value} as a float.");
                        alphaKeys[i].alpha = kvp.Value.As<float>();
                        i++;
                    }

                    var gradient = new Gradient();
                    gradient.SetKeys(colorKeys, alphaKeys);
                    return gradient;
                }
            );
            _config = deserializer.Deserialize<Config>(config);

            _isp = _config.isp;
            _thrust = _config.thrust * 1000.0; // kN -> N
            _propRatio = new Dictionary<string, double>(_config.propellantRatio);
            _thrustDir = Vector2d.up.Rotate(part.craftRot * 180.0 / Math.PI);

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
