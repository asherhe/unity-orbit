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

        public override void OnLoad(DataNode config)
        {
            base.OnLoad(config);

            _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);

            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            _config.material.LoadMaterial(m =>
            {
                _spriteRenderer.material = m;
                _config.material.SetMaterialProperties(_spriteRenderer.material);
            });
        }

        public override async Task OnLoadAsync(DataNode config)
        {
            var sprite = Addressables.LoadAssetAsync<Sprite>(_config.sprite);
            await sprite.Task;
            _spriteRenderer.sprite = sprite.Result;
        }
    }
}