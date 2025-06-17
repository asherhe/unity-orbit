using Parts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Spacecraft : MonoBehaviour, IOrbitingObject
{
    public DataObject craftConfig; // TODO: we use this until automatic vessel loading
    private Config _config;
    [Serializable]
    private class Config
    {
        public string name;

        public OrbitData orbit;
        [Serializable]
        public class OrbitData
        {
            public string parent;
            public double h, e, omega, M0, t0;
        }

        public List<DataNode> parts;
    }

    public double _dryMass = 0.0;
    public double _pluginMass = 0.0;
    public double mass { get => _dryMass + _pluginMass; }

    // TODO: remove this once we have celestial body system
    [SerializeField]
    private CelestialBody _body;

    public CelestialBody body { get => orbit.body; }
    public Orbit orbit { get; set; }
    private Trajectory _trajectory;

    public Vector2d pos { get; private set; }

    public Vector2d vel { get; private set; }

    public double altitude { get => pos.magnitude - body.radius; }

    // spacecraft control
    // TODO: move this somewhere else

    private float _throttle = 0.0f;
    /// <summary>
    /// spacecraft throttle (between 0.0 and 1.0)
    /// </summary>
    public float Throttle
    {
        get => _throttle;
        set { _throttle = Mathf.Clamp01(value); }
    }

    public float _steeringControl = 0.0f;
    /// <summary>
    /// input for spacecraft steering (between -1.0 and 1.0), positive is ccw
    /// </summary>
    public float SteeringControl
    {
        get => _steeringControl;
        set { _steeringControl = Mathf.Clamp(value, -1.0f, 1.0f); }
    }

    private void Awake()
    {
        // TODO: placeholder orbit, a 200km circular orbit
        //orbit = Orbit.MakeCircularOrbit(200.0, _body);

        OnLoad(craftConfig.root);

        pos = orbit.GetPosition(); vel = orbit.GetVelocity();
    }

    public void OnLoad(DataNode config)
    {
        _config = DataNodeSerialization.Deserialize<Config>(config);

        // TODO: initialize fields
        orbit = new Orbit(
            _config.orbit.h,
            _config.orbit.e,
            _config.orbit.omega,
            _config.orbit.M0,
            _config.orbit.t0,
            _body // TODO: still using serialized inspector field, change this once we upgrade celestial bodies
        );

        foreach (var partConfig in _config.parts)
        {
            var partGO = new GameObject(partConfig["name"].Value);
            partGO.transform.parent = transform;
            var part = partGO.AddComponent<Parts.Part>();
            part.craft = this;

            StartCoroutine(LoadPartCoroutine(part, partConfig));
        }
    }

    private IEnumerator LoadPartCoroutine(Parts.Part part, DataNode partConfig)
    {
        var task = part.OnLoadAsync(partConfig);
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted) throw task.Exception;

        _dryMass += part.mass;
        foreach (var plugin in part.plugins)
        {
            if (typeof(MassivePartPlugin).IsAssignableFrom(plugin.GetType()))
            {
                var massivePlugin = (MassivePartPlugin)plugin;
                _pluginMass += massivePlugin.Mass;
                massivePlugin.OnMassChanged += massChange =>
                {
                    _pluginMass += massChange;
                };
            }
        }
    }

    private void Start()
    {
        _trajectory = body.AddTrajectory(this);
    }

    private void FixedUpdate()
    {
        pos = orbit.GetPosition(); vel = orbit.GetVelocity();

        if (Universe.Instance.timewarpScale == 1.0)
        {
            /*transform.rotation *= Quaternion.Euler(0, 0, (float)(SteeringControl * turnRate * Universe.Instance.fixedDeltaTime));
            
            if (Throttle > 0.0)
            {
                Vector2d dv = new Vector2d(transform.up.x, transform.up.y) * (Throttle * thrust * Universe.Instance.fixedDeltaTime);
                orbit.UpdateFromStateVectors(pos, vel + dv, Universe.Instance.UT, body);
            }*/
        }

        if (ActiveCraftController.Instance.craft != this)
        {
            transform.position = pos - ActiveCraftController.Instance.craft.pos;
        }
    }
}
