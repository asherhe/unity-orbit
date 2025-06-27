using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        private PartDefinition _partDefinitionConfig;
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
            /// whether crossfeed is enabled
            /// </summary>
            public bool crossfeed = true;

            /// <summary>
            /// info about this part's attachment nodes
            /// </summary>
            public AttachmentNode[] attachmentNodes;

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
        /// unique identifier for this part within the spacecraft
        /// </summary>
        public string id { get; private set; }
        /// <summary>
        /// display name of this part
        /// </summary>
        public string displayName { get; private set; }

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
        /// <summary>
        /// whether this part has crossfeed.
        /// in parts that have crossfeed, resources with the Stage flow mode will be allowed to flow through this part.
        /// </summary>
        public bool hasCrossfeed { get; private set; }

        [Serializable]
        public class AttachmentNode
        {
            public string name;
            public Vector2d pos;
        }
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
            public Part atchPart;
            /// <summary>
            /// the name of the attached node on the OTHER part
            /// </summary>
            public string atchNode;
        }
        public AttachmentNode[] attachNodes { get; private set; }
        public PartAttachment[] attachments { get; private set; }

        public List<PartPlugin> plugins { get; private set; }

        /// <summary>
        /// gets the first plugin with type T (subclasses count too)
        /// </summary>
        /// <returns>
        /// a PartPlugin of type T if one exists on this part, otherwise null
        /// </returns>
        public T GetPlugin<T>() where T : PartPlugin
        {
            int i = plugins.FindIndex(p => typeof(T).IsAssignableFrom(p.GetType()));
            if (i == -1) return null;
            return (T)(plugins[i]);
        }

        /// <summary>
        /// gets a list of all plugins with type T (subclass count too)
        /// </summary>
        /// <returns>
        /// a list of plugins of type T, can be empty is none are found
        /// </returns>
        public List<T> GetPluginsAll<T>() where T : PartPlugin
        {
            return plugins.FindAll(p => typeof(T).IsAssignableFrom(p.GetType())) as List<T>;
        }

        /// <summary>
        /// get all containers for a resource that are accessible based on the resource's flow rate
        /// </summary>
        /// <param name="type">internal name of resource to check</param>
        public List<ResourceContainerPlugin> GetAccessibleResourceContainers(string type)
        {
            var containers = new List<ResourceContainerPlugin>();
            var flow = ResourceManager.GetFlow(type);
            switch (flow)
            {
                case ResourceManager.FlowMode.None:
                    containers.Add(GetPlugin<ResourceContainerPlugin>());
                    break;
                case ResourceManager.FlowMode.Stage:
                    var q = new Queue<Part>();
                    q.Enqueue(this);

                    var visited = new HashSet<Part>();
                    while (q.Count > 0)
                    {
                        var part = q.Dequeue();
                        if (visited.Contains(part)) continue;
                        visited.Add(part);
                        var container = part.GetPlugin<ResourceContainerPlugin>();
                        if (container != null) containers.Add(container);
                        foreach (var attachment in part.attachments)
                            if (attachment.atchPart.hasCrossfeed)
                                q.Enqueue(attachment.atchPart);
                    }
                    break;
                case ResourceManager.FlowMode.Craft:
                    foreach (var part in craft.parts)
                        foreach (var plugin in part.plugins)
                            if (typeof(ResourceContainerPlugin).IsAssignableFrom(plugin.GetType()))
                                containers.Add((ResourceContainerPlugin)plugin);
                    break;
                default:
                    throw new NotSupportedException("Resource flow is currently only supported for None, Stage, and Craft flow modes");
            }
            return containers;
        }

        /// <summary>
        /// calculate the amount of resource available to 
        /// </summary>
        /// <param name="type">internal name of resource</param>
        public double GetResourceAvailable(string type)
        {
            double resources = 0.0;
            foreach (var container in GetAccessibleResourceContainers(type))
                resources += container.GetAmount(type);
            return resources;
        }

        /// <summary>
        /// drains a resource based on container priority the resource's flow mode
        /// </summary>
        /// <param name="type">internal name for resource to drain</param>
        /// <param name="amt">amount of resource to drain</param>
        /// <returns>
        /// how much of the originally requested amount was NOT drained. if resource was completely drained, return 0
        /// </returns>
        public double DrainResource(string type, double amt)
        {
            var prioritized = GetAccessibleResourceContainers(type)
                .GroupBy(c => c.GetPriority(type))
                .OrderByDescending(c => c.First().GetPriority(type));
            foreach (var priorityGroup in prioritized)
            {
                double groupAmt = 0.0, groupMaxAmt = 0.0;
                foreach (var container in priorityGroup)
                {
                    var res = container.resources[type];
                    groupAmt += res.amount;
                    groupMaxAmt += res.maxAmount;
                }
                var drainAmt = Math.Min(amt, groupAmt);
                amt -= drainAmt;

                drainAmt /= groupMaxAmt; // fraction of full capacity to drain for each container
                foreach (var container in priorityGroup)
                    container.Drain(type, drainAmt * container.GetMaxAmount(type));

                if (amt == 0.0) break;
            }
            return amt;
        }

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
            _craftPartConfig = Serialization.DataNodeSerialization.Deserialize<CraftConfig>(config);

            var defHandle = Addressables.LoadAssetAsync<DataObject>($"Assets/GameData/Parts/{_craftPartConfig.type}/{_craftPartConfig.type}.data");
            await defHandle.Task;
            _partDefinitionConfig = Serialization.DataNodeSerialization.Deserialize<PartDefinition>(defHandle.Result.root);

            id = _craftPartConfig.id;
            name = _craftPartConfig.name;

            transform.localPosition = craftPos = _craftPartConfig.transform.pos;
            transform.localEulerAngles = new Vector3(0.0f, 0.0f, (float)(craftRot = _craftPartConfig.transform.rot));

            mass = _partDefinitionConfig.mass * 1000.0; // mt -> kg
            hasCrossfeed = _partDefinitionConfig.crossfeed;

            attachNodes = _partDefinitionConfig.attachmentNodes;
            attachments = new PartAttachment[_craftPartConfig.attachments.Length];

            var pluginTasks = new List<Task>();

            // load plugins and set configs
            foreach (var partDefKVP in _partDefinitionConfig.plugins.KeyValuePairs)
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

        /// <summary>
        /// called when all parts on the craft are fully loaded.
        /// used for operations that need to reference other parts that cannot be run in OnLoadAsync while not all parts are loaded.
        /// </summary>
        public void OnCraftPartsLoaded()
        {
            Debug.Log($"OnCraftPartsLoaded {id} - {_craftPartConfig.attachments.Length}");
            for (int i = 0; i < attachments.Length; i++)
            {
                attachments[i] = new();
                attachments[i].mode = _craftPartConfig.attachments[i].mode;
                attachments[i].node = _craftPartConfig.attachments[i].node;
                attachments[i].atchPart = craft.GetPartByID(_craftPartConfig.attachments[i].atchPart);
                attachments[i].atchNode = _craftPartConfig.attachments[i].atchNode;
                Debug.Log(attachments[i].atchPart.id);
            }
        }
    }

    public enum PartAttachMode
    {
        Edge, // parts are "connected" because they are placed on touching/over each other
        Node // parts are connected via part attachment nodes
    }
}
