using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : SingletonBehaviour<ResourceManager>
{
    [SerializeField]
    private DataObject _resourceConfig;

    private Resource[] _config;
    [Serializable]
    public class Resource
    {
        /// <summary>
        /// internal name for referring to this resource
        /// </summary>
        public string type;
        /// <summary>
        /// display name of this resource
        /// </summary>
        public string name;
        /// <summary>
        /// resource mass contribution, kg/unit
        /// </summary>
        public double density;
        /// <summary>
        /// how this resource is allowed to flow
        /// </summary>
        public FlowMode flow;
    }

    /// <summary>
    /// how this resource can be used by other parts in a craft
    /// </summary>
    public enum FlowMode
    {
        None, // can only be used by the part it's in (e.g. solid fuel, ablator)
        Stage, //can be used from within one stage, defined by crossfeed rules (liquid fuel/oxidizer)
        Craft //can be used by any part within the craft (e.g. electricity)
    }

    public Dictionary<string, Resource> resources = new Dictionary<string, Resource>();

    protected override void Awake()
    {
        base.Awake();

        _config = DataNodeSerialization.Deserialize<Resource[]>(_resourceConfig.root);
        foreach (var config in _config)
            resources[config.type] = config;
    }
}
