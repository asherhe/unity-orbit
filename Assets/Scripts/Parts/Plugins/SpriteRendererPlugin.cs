using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Parts
{
    public class SpriteRendererPlugin : PartPlugin
    {
        private Config _config;
        [Serializable]
        private class Config
        {
            /// <summary>
            /// addressable path to the display sprite for this part
            /// </summary>
            public string sprite;

            /// <summary>
            /// material to use on the SpriteRenderer
            /// </summary>
            public MaterialProperties material;
        }

        private SpriteRenderer _spriteRenderer;

        protected override void OnAwake()
        {
            base.OnAwake();

            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        public override void OnLoad(DataNode config)
        {
            base.OnLoad(config);

            _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);

            _config.material.LoadMaterial(m =>
            {
                _spriteRenderer.material = m;
                _config.material.SetMaterialProperties(_spriteRenderer.material);
            });
        }

        public override async Task OnLoadAsync(DataNode config)
        {
            await base.OnLoadAsync(config);

            var sprite = Addressables.LoadAssetAsync<Sprite>(_config.sprite);
            await sprite.Task;
            _spriteRenderer.sprite = sprite.Result;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // assume position within craft has negligible effect
            var heliocentric = craft.GetHeliocentricPosition();
            // we scale by 2 because that makes the lighting look better
            var sunIntensity = 2f * (float)CelestialLightingUtils.SunIntensity(heliocentric);
            var sunDirection = -heliocentric.Normalized;
            // set z to 0.3 so that parts are better illuminated
            Vector4 sunDir = new((float)sunDirection.x, (float)sunDirection.y, 0.3f, 0.0f);
            _spriteRenderer.material.SetFloat("_SunIntensity", sunIntensity);
            _spriteRenderer.material.SetVector("_SunDir", sunDir);

            float shade = (float)CelestialLightingUtils.CastBodySoftShadow(craft);
            // deal with atmospheric scattering at a later date
            _spriteRenderer.material.SetColor("_SunColor", new Color(shade, shade, shade, 1f));

            var planetShineProperties = CelestialLightingUtils.ComputePlanetShine(craft);
            _spriteRenderer.material.SetColor("_PlanetShineColor", planetShineProperties.color);
            _spriteRenderer.material.SetFloat("_PlanetShineIntensity", planetShineProperties.intensity);
            _spriteRenderer.material.SetVector("_PlanetShineDir", planetShineProperties.direction);
            _spriteRenderer.material.SetFloat("_PlanetShineSpread", planetShineProperties.spread);
        }
    }
}