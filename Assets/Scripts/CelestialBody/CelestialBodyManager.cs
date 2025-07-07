using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CelestialBodyManager : SingletonBehaviour<CelestialBodyManager>
{
    /// <summary>
    /// list of config files, storing data on each celestial body, in order of when they should be load.
    /// the first body is the root body of the entire planetary system, should be the sun.
    /// </summary>
    [SerializeField]
    private List<DataObject> _celestialBodyConfigs;

    /// <summary>
    /// all celestial bodies, accessible by name
    /// </summary>
    [HideInInspector]
    public Dictionary<string, CelestialBody> celestialBodies;

    protected override void Awake()
    {
        base.Awake();

        celestialBodies = new();
        foreach (var config in _celestialBodyConfigs)
        {
            var name = config.root["name"].As<string>();
            var bodyGameObject = new GameObject(name);
            bodyGameObject.transform.parent = transform;
            var body = bodyGameObject.AddComponent<CelestialBody>();
            body.LoadConfig(config.root);
            celestialBodies.Add(name, body);
        }
    }
}
