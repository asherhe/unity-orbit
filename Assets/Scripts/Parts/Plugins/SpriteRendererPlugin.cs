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
            var sunIntensity = (float)(1.7e22 / heliocentric.Magnitude2); // slightly over 2 at earth orbit
            var sunDirection = -heliocentric.Normalized;
            // set z to 0.3 so that parts are better illuminated
            Vector4 sunDir = new((float)sunDirection.x, (float)sunDirection.y, 0.3f, 0.0f);
            _spriteRenderer.material.SetFloat("_SunIntensity", sunIntensity);
            _spriteRenderer.material.SetVector("_SunDir", sunDir);

            // find closest point along sun ray to parent body
            var t = Math.Max(-Vector2d.Dot(sunDirection, craft.Position), 0.0);
            var closest = craft.Position + t * sunDirection;
            // altitude of closest point
            var alt = closest.Magnitude - craft.body.radius;
            // apparent radius of the sun, if we projected it to the closest point
            var apparentRad = CelestialBodyManager.Instance.celestialBodies["Sun"].radius / heliocentric.Magnitude * t;
            // how much of the sun is occluded, used for soft shadows
            // not completely physically accurate but good enough to give convincing results
            var occlusion = 0.5 * (1.0 + alt / apparentRad);
            var shade = Mathf.Clamp01((float)occlusion);
            // deal with atmospheric scattering at a later date
            _spriteRenderer.material.SetColor("_SunColor", new Color(shade, shade, shade, 1f));
        }
    }
}