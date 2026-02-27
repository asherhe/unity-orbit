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
            var heliocentric = craft.orbit.body.GetHeliocentricPosition() + craft.Position;
            var sunIntensity = (float)(1.7e22 / heliocentric.Magnitude2); // slightly over 2 at earth orbit
            Vector4 sunDirection = -heliocentric.Normalized;
            sunDirection.z = 0.3f; sunDirection.w = 0.0f;
            _spriteRenderer.material.SetFloat("_SunIntensity", sunIntensity);
            _spriteRenderer.material.SetVector("_SunDir", sunDirection);
        }
    }
}