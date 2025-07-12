using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Parts
{
    public class PartPlugin : MonoBehaviour
    {
        /// <summary>
        /// the Spacecraft this plugin belongs to
        /// </summary>
        public Spacecraft craft;

        /// <summary>
        /// the Part this plugin belongs to
        /// </summary>
        public Part part;

        [SerializeField]
        private bool _isEnabled = true;
        /// <summary>
        /// is this plugin enabled?
        /// code in OnFixedUpdate, OnUpdate, and OnLateUpdate will only run if the plugin is enabled.
        /// plugin enable/disable state can be set from the craft config, and will be automatically loaded,
        /// so there is no need to handle the <c>enabled</c> key in OnLoad or OnLoadAsync
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled = value) OnPluginEnable();
                else OnPluginDisable();
            }
        }

        /// <summary>
        /// called when this plugin instance is created on craft load.
        /// </summary>
        protected virtual void OnAwake() { }
        /// <summary>
        /// called just before the first OnUpdate() call of this plugin
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// called when this plugin's data is loaded.
        /// if the plugin is being loaded on part for the first time, this will run AFTER OnAwake().
        /// note that it is not guarenteed that this will run before OnStart() because
        /// the loading of config files takes some time.
        /// </summary>
        public virtual void OnLoad(DataNode config)
        {
            if (config.ContainsKey("enabled"))
            {
                _isEnabled = config["enabled"].As<bool>();
            }
        }
        /// <summary>
        /// called right after OnLoad() returns and intended for the same purpose, but asynchronous.
        /// this method is suitable for putting load operations that require loading or long computations
        /// </summary>
        public virtual async Task OnLoadAsync(DataNode config) { await Task.CompletedTask; } // asynchronously do nothing

        /// <summary>
        /// called when this plugin is enabled.
        /// note that this is not called when the plugin is first loaded in OnLoad()
        /// </summary>
        protected virtual void OnPluginEnable() { }
        /// <summary>
        /// called when this plugin is disabled
        /// note that this is not called when the plugin is first loaded in OnLoad()
        /// </summary>
        protected virtual void OnPluginDisable() { }

        /// <summary>
        /// called every fixed framerate tick, used for physics calculations
        /// </summary>
        protected virtual void OnFixedUpdate() { }
        /// <summary>
        /// called every frame if this plugin is enabled
        /// </summary>
        protected virtual void OnUpdate() { }
        /// <summary>
        /// called every frame after all Update() calls complete, if this plugin is enabled
        /// </summary>
        protected virtual void OnLateUpdate() { }

        private void Awake() { OnAwake(); }
        private void Start() { OnStart(); }
        private void FixedUpdate() { if (IsEnabled) OnFixedUpdate(); }
        private void Update() { if (IsEnabled) OnUpdate(); }
        private void LateUpdate() { if (IsEnabled) OnLateUpdate(); }
    }
}
