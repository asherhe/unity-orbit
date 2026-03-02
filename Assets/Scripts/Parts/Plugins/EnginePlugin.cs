using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.Universal;

namespace Parts
{
    public class EnginePlugin : PartPlugin, IActuator
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
            /// thrust direction in part space
            /// </summary>
            public Vector2d thrustDir;
            /// <summary>
            /// relative ratio of propellants used in the engine.
            /// scaled so that the total propellant mass matches the expected propellant consumption from the engine.
            /// </summary>
            public Dictionary<string, double> propellantRatio;

            /// <summary>
            /// engine plume particle system configuration
            /// </summary>
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

            /// <summary>
            /// engine scene light configuration
            /// </summary>
            public LightConfig light;
            [Serializable]
            public class LightConfig
            {
                /// <summary>
                /// engine light color, should roughly correspond to plume color
                /// </summary>
                public Color color;

                /// <summary>
                /// intensity of engine light at max throttle
                /// </summary>
                public float intensity;

                /// <summary>
                /// how far this light should shine (m)
                /// </summary>
                public float radius;

                /// <summary>
                /// adjust the shape of the intensity-distance curve. 0 is softer, 1 is sharper
                /// </summary>
                public float falloff;

                /// <summary>
                /// frequency at which light intensity flickers
                /// </summary>
                public float flickerFrequency;

                /// <summary>
                /// minimum and maximum light flicker intensity, as a multiplier of intensity
                /// </summary>
                public float flickerMin, flickerMax;
            }
        }

        private GameObject _particleObject;
        private ParticleSystem _particleSystem;
        private ParticleSystem.EmissionModule _psEmission;

        private GameObject _lightObject;
        private Light2D _light2D;
        private float _maxLightIntensity;
        private float _flickerFrequency;
        private float _flickerMin, _flickerMax;

        /// <summary>
        /// specific impulse, in seconds
        /// </summary>
        private double _isp;
        /// <summary>
        /// engine thrust, in newtons
        /// </summary>
        private double _thrust;
        /// <summary>
        /// direction for engine thrust in craft space
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

        public ActuatorProperties ActuatorProperties { get; private set; } = new();

        protected override void OnAwake()
        {
            base.OnAwake();

            _particleObject = new GameObject("Engine Plume");
            _particleObject.transform.parent = transform;
            _particleSystem = _particleObject.AddComponent<ParticleSystem>();
            _psEmission = _particleSystem.emission; _psEmission.enabled = false;
        }

        public override void OnLoad(DataNode config)
        {
            base.OnLoad(config);

            _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);

            _isp = _config.isp;
            _thrust = _config.thrust * 1000.0; // kN -> N
            _thrustDir = _config.thrustDir.Rotate(part.craftRot * 180.0 / Math.PI);

            ActuatorProperties.maxThrust = _thrust * _thrustDir;
            ActuatorProperties.isp = _isp;
            ActuatorProperties.UpdateState();

            _propRatio = new Dictionary<string, double>(_config.propellantRatio);
            _propRatioMass = 0.0;
            foreach (var kvp in _propRatio)
                _propRatioMass += kvp.Value * ResourceManager.GetDensity(kvp.Key);

            _particleObject.transform.localPosition = _config.plume.nozzlePos;
            _particleObject.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);

            var main = _particleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = _config.plume.lifetime;
            main.startSpeed = _config.plume.start.speed;
            main.startSize = _config.plume.start.size;
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0.0f, 2.0f * Mathf.PI);

            _psEmission.enabled = IsEnabled;
            _psEmission.rateOverTime = 0;

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

            _maxLightIntensity = _config.light.intensity;
            _flickerFrequency = _config.light.flickerFrequency;
            _flickerMin = _config.light.flickerMin;
            _flickerMax = _config.light.flickerMax;
        }

        public override async Task OnLoadAsync(DataNode config)
        {
            await base.OnLoadAsync(config);

            await Task.WhenAll(LoadPlumeMatAsync(), LoadLightAsync());

            async Task LoadPlumeMatAsync()
            {
                var plumeMat = Addressables.LoadAssetAsync<Material>("Assets/GameData/PartPlugins/EnginePlugin/PlumeMaterial.mat");
                await plumeMat.Task;
                var renderer = _particleObject.GetComponent<ParticleSystemRenderer>();
                renderer.material = plumeMat.Result;
            }

            async Task LoadLightAsync()
            {
                // TODO: use LightPlugin for this
                var lightPrefab = Addressables.LoadAssetAsync<GameObject>("Assets/GameData/PartPlugins/LightPlugin/LightSource.prefab");
                await lightPrefab.Task;
                _lightObject = Instantiate(lightPrefab.Result, transform);
                _lightObject.transform.localPosition = _config.plume.nozzlePos;
                _light2D = _lightObject.GetComponent<Light2D>();
                _light2D.color = _config.light.color;
                _light2D.intensity = 0.0f;
                _light2D.pointLightInnerRadius = 0.0f;
                _light2D.pointLightOuterRadius = _config.light.radius;
                _light2D.falloffIntensity = _config.light.falloff;
            }
        }

        protected override void OnFixedUpdate()
        {
            if (craft.Control.Throttle > 0.0)
            {
                var thrust = _thrust * craft.Control.Throttle;
                var propFlow = Universe.Instance.fixedDeltaTime * thrust / (_isp * 9.8); // mass of propellant flow into engine
                var propCoeff = propFlow / _propRatioMass; // kg per propellant ratio mass

                // scale thrust by how much propellant we are actually able to pump
                var propFactor = 1.0; // fraction of the expected propellant we are actually able to drain
                foreach (var kvp in _propRatio)
                {
                    var available = part.GetResourceAvailable(kvp.Key) * ResourceManager.GetDensity(kvp.Key);
                    var required = propCoeff * kvp.Value;
                    propFactor = Math.Min(available / required, propFactor);
                }
                thrust *= propFactor; propCoeff *= propFactor;

                if (propFactor > 0.0)
                {
                    // drain propellant
                    foreach (var kvp in _propRatio)
                        part.DrainResource(kvp.Key, propCoeff * kvp.Value / ResourceManager.GetDensity(kvp.Key));

                    // apply thrust
                    craft.Newtonian.ApplyForce(thrust * _thrustDir, part.craftPos);
                }
                else
                {
                    // shut down engine
                    IsEnabled = false;
                }
            }
        }

        protected override void OnUpdate()
        {
            _psEmission.enabled = craft.Control.Throttle > 0.0;
            if (_psEmission.enabled)
            {
                _psEmission.rateOverTime = new ParticleSystem.MinMaxCurve(
                    craft.Control.Throttle * _config.plume.rate.constantMin,
                    craft.Control.Throttle * _config.plume.rate.constantMax
                );
            }

            if (_light2D != null)
            {
                var baseIntensity = craft.Control.Throttle * _maxLightIntensity;
                var flicker = Mathf.Lerp(_flickerMin, _flickerMax, Mathf.PerlinNoise1D(_flickerFrequency * (float)MathUtils.Mod(Universe.Instance.UT, 1e4)));
                _light2D.intensity = baseIntensity * flicker;
            }
        }

        protected override void OnPluginEnable()
        {
            _psEmission.enabled = true;
            _light2D.enabled = true;
        }
        protected override void OnPluginDisable()
        {
            _psEmission.enabled = false;
            _light2D.enabled = false;
        }
    }
}
