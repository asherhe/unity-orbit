using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.Universal;

namespace Parts
{
    /// <summary>
    /// creates a scene light to illuminate surroundings
    /// </summary>
    public class LightPlugin : PartPlugin
    {
        private Config _config;
        [Serializable]
        private class Config
        {
            /// <summary>
            /// location of the light in local part space
            /// </summary>
            public Vector2 lightPosition;

            /// <summary>
            /// engine light color, should roughly correspond to plume color
            /// </summary>
            public Color color;

            /// <summary>
            /// intensity of engine light at max throttle
            /// </summary>
            public float intensity;

            /// <summary>
            /// inner radius of light (m)
            /// </summary>
            public float innerRadius;

            /// <summary>
            /// outer radius of light (m)
            /// </summary>
            public float outerRadius;

            /// <summary>
            /// adjust the shape of the intensity-distance curve. 0 is softer, 1 is sharper
            /// </summary>
            public float falloff;
        }

        private GameObject _lightObject;
        private Light2D _light2D;

        /// <summary>
        /// location of the light in local part space
        /// </summary>
        public Vector2 LightPosition
        {
            get => _lightObject.transform.localPosition;
            set => _lightObject.transform.localPosition = value;
        }

        /// <summary>
        /// engine light color, should roughly correspond to plume color
        /// </summary>
        public Color Color
        {
            get => _light2D.color;
            set => _light2D.color = value;
        }

        /// <summary>
        /// intensity of engine light at max throttle
        /// </summary>
        public float Intensity
        {
            get => _light2D.intensity;
            set => _light2D.intensity = value;
        }

        /// <summary>
        /// inner radius of light (m)
        /// </summary>
        public float InnerRadius
        {
            get => _light2D.pointLightInnerRadius;
            set => _light2D.pointLightInnerRadius = value;
        }

        /// <summary>
        /// outer radius of light (m)
        /// </summary>
        public float OuterRadius
        {
            get => _light2D.pointLightOuterRadius;
            set => _light2D.pointLightOuterRadius = value;
        }

        /// <summary>
        /// adjust the shape of the intensity-distance curve. 0 is softer, 1 is sharper
        /// </summary>
        public float Falloff
        {
            get => _light2D.falloffIntensity;
            set => _light2D.falloffIntensity = value;
        }

        public override void OnLoad(DataNode config)
        {
            base.OnLoad(config);

            _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);
        }

        public override async Task OnLoadAsync(DataNode config)
        {
            await base.OnLoadAsync(config);

            var lightPrefab = Addressables.LoadAssetAsync<GameObject>("Assets/GameData/PartPlugins/LightPlugin/LightSource.prefab");
            await lightPrefab.Task;
            _lightObject = Instantiate(lightPrefab.Result, transform);
            _light2D = _lightObject.GetComponent<Light2D>();

            LightPosition = _config.lightPosition;
            Color = _config.color;
            Intensity = _config.intensity;
            InnerRadius = _config.innerRadius;
            OuterRadius = _config.outerRadius;
            Falloff = _config.falloff;
        }

        protected override void OnPluginEnable()
        {
            base.OnPluginEnable();
            _light2D.gameObject.SetActive(true);
        }

        protected override void OnPluginDisable()
        {
            base.OnPluginDisable();
            _light2D.gameObject.SetActive(false);
        }
    }
}
