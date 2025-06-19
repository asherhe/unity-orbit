using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Parts
{
    public class Part : MonoBehaviour
    {
        /// <summary>
        /// configuration data read from the CRAFT configuration (not part definition!)
        /// </summary>
        private CraftConfig _craftPartConfig;
        [Serializable]
        private class CraftConfig
        {
            /// <summary>
            /// part type of this part, the name of a folder in Assets/GameData/Parts
            /// </summary>
            public string type;

            /// <summary>
            /// unique identifier for this Part instance
            /// </summary>
            public string id;

            /// <summary>
            /// display name for this part
            /// </summary>
            public string name;

            /// <summary>
            /// transform of this part in craft space
            /// </summary>
            public PartTransform transform;
            [Serializable]
            public class PartTransform
            {
                public Vector2d pos;
                public double rot;
            }

            /// <summary>
            /// info about the parts this part is attached to
            /// </summary>
            public PartAttachment[] attachments;
            [Serializable]
            public class PartAttachment
            {
                /// <summary>
                /// how this pair of part is attached
                /// </summary>
                public PartAttachMode mode;

                /* the below are if mode is PartAttachMode.Node */

                /// <summary>
                /// the name of the attached node on THIS part
                /// </summary>
                public string node;
                /// <summary>
                /// the id of the part we are attached to
                /// </summary>
                public string atchPart;
                /// <summary>
                /// the name of the attached node on the OTHER part
                /// </summary>
                public string atchNode;
            }

            /// <summary>
            /// configuration data for all the plugins on this part
            /// </summary>
            public DataNode plugins;
        }

        /// <summary>
        /// definition of this part
        /// </summary>
        private PartDefinition _partDefinition;
        [Serializable]
        private class PartDefinition
        {
            /// <summary>
            /// display name of this part
            /// </summary>
            public string name;
            /// <summary>
            /// dry mass of this part (without propellant and other mass-adding things, if applicable)
            /// </summary>
            public double mass;

            /// <summary>
            /// info about this part's attachment nodes
            /// </summary>
            public AttachmentNode[] attachmentNodes;
            [Serializable]
            public class AttachmentNode
            {
                public string name;
                public Vector2d pos;
            }

            /// <summary>
            /// configs for every plugin present in this part
            /// </summary>
            public DataNode plugins;
        }

        /// <summary>
        /// spacecraft this part belongs to
        /// </summary>
        public Spacecraft craft;

        /// <summary>
        /// local position in craft space
        /// </summary>
        public Vector2d craftPos { get; private set; }
        /// <summary>
        /// local rotation in craft space, in degrees
        /// </summary>
        public double craftRot { get; private set; }

        /// <summary>
        /// dry mass of this part
        /// </summary>
        public double mass { get; private set; }

        public List<PartPlugin> plugins { get; private set; }

        private void Awake()
        {
            plugins = new List<PartPlugin>();
        }

        /// <summary>
        /// called when this part's config data is loaded from the CRAFT config.
        /// note that the PART DEFINITION is not what is loaded, but information about a particular instance of a part in a craft.
        /// the loading of part definitions is handled from within OnLoad(), so don't worry
        /// </summary>
        public async Task OnLoadAsync(DataNode config)
        {
            _craftPartConfig = DataNodeSerialization.Deserialize<CraftConfig>(config);

            var defHandle = Addressables.LoadAssetAsync<DataObject>($"Assets/GameData/Parts/{_craftPartConfig.type}/{_craftPartConfig.type}.data");
            await defHandle.Task;

            _partDefinition = DataNodeSerialization.Deserialize<PartDefinition>(defHandle.Result.root);

            // TODO: set fields as necessary
            mass = _partDefinition.mass * 1000.0; // mt -> kg

            transform.localPosition = craftPos = _craftPartConfig.transform.pos;
            transform.localEulerAngles = new Vector3(0.0f, 0.0f, (float)(craftRot = _craftPartConfig.transform.rot));

            var pluginTasks = new List<Task>();

            // load plugins and set configs
            foreach (var partDefKVP in _partDefinition.plugins.KeyValuePairs)
            {
                var pluginName = partDefKVP.Key;
                var pluginClassName = "Parts." + pluginName; // part plugins must be in namespace Parts
                var pluginType = Type.GetType(pluginClassName);
                if (pluginType == null)
                    throw new InvalidOperationException($"Plugin class {pluginClassName} could not be found.");
                if (!typeof(PartPlugin).IsAssignableFrom(pluginType))
                    throw new InvalidOperationException($"Class {pluginClassName} is not a subclass of Parts.PartPlugin");

                PartPlugin plugin = (PartPlugin)gameObject.AddComponent(pluginType);
                plugin.part = this;
                plugin.craft = craft;
                plugins.Add(plugin);

                // combine part definition config with craft config
                // craft config takes priority over definition config
                var pluginConfig = new DataNode(partDefKVP.Value);
                if (_craftPartConfig.plugins != null && _craftPartConfig.plugins.ContainsKey(pluginName))
                    foreach (var craftKVP in _craftPartConfig.plugins[pluginName].KeyValuePairs)
                        pluginConfig[craftKVP.Key] = craftKVP.Value;

                plugin.OnLoad(pluginConfig);
                pluginTasks.Add(plugin.OnLoadAsync(pluginConfig));
            }

            await Task.WhenAll(pluginTasks);
        }
    }

    public enum PartAttachMode
    {
        Node, // parts are connected via part attachment nodes
        Edge // parts are "connected" because they are placed on touching/over each other
    }
}
