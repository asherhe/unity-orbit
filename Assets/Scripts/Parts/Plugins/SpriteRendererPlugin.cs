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
        }

        private SpriteRenderer _spriteRenderer;

        public override async Task OnLoadAsync(DataNode config)
        {
            _config = DataNodeSerialization.Deserialize<Config>(config);

            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            var sprite = Addressables.LoadAssetAsync<Sprite>(_config.sprite);
            await sprite.Task;
            _spriteRenderer.sprite = sprite.Result;
        }
    }
}