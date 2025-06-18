using Parts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
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

        public RotationInfo rotation;
        [Serializable]
        public class RotationInfo
        {
            public double angle, momentum;
        }

        public List<DataNode> parts;
    }

    public SpacecraftNewtonian Newtonian { get; private set; }
    private Action _massChangeHandler;

    public List<Part> parts;


    // TODO: remove this once we have celestial body system
    [SerializeField]
    private CelestialBody _body;

    public CelestialBody body { get => orbit.body; }
    public Orbit orbit { get; set; }
    private Trajectory _trajectory;

    public Vector2d pos { get; private set; }
    public Vector2d vel { get; private set; }
    public double altitude { get => pos.magnitude - body.radius; }


    private GameObject _partsGameObject;


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
    private float _steeringControl = 0.0f;
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
        Newtonian = gameObject.AddComponent<SpacecraftNewtonian>();

        IEnumerator OnLoadCoroutine(DataNode config)
        {
            var task = OnLoad(config);
            yield return new WaitUntil(() => task.IsCompleted);
        }
        StartCoroutine(OnLoadCoroutine(craftConfig.root));

        pos = orbit.GetPosition(); vel = orbit.GetVelocity();
    }

    public async Task OnLoad(DataNode config)
    {
        _config = DataNodeSerialization.Deserialize<Config>(config);

        // TODO: initialize fields
        Newtonian.angle = _config.rotation.angle;
        Newtonian.angularMomentum = _config.rotation.momentum;

        orbit = new Orbit(
            _config.orbit.h,
            _config.orbit.e,
            _config.orbit.omega,
            _config.orbit.M0,
            _config.orbit.t0,
            _body // TODO: still using serialized inspector field, change this once we upgrade celestial bodies
        );

        // starts asynchronous part loading
        // we need this because we need to do operations on the part after loading is complete,
        // so we wrap Part.OnLoadAsync in a function that returns the part itself
        async Task<Part> LoadPartAsync(Part part, DataNode partConfig)
        {
            await part.OnLoadAsync(partConfig);
            return part;
        }

        // initialize part gameobjects and begin loading
        parts = new List<Part>();
        _partsGameObject = new GameObject("Parts"); _partsGameObject.transform.parent = transform;
        var tasks = new List<Task<Part>>();
        foreach (var partConfig in _config.parts)
        {
            var partGO = new GameObject(partConfig["name"].Value);
            partGO.transform.parent = _partsGameObject.transform;
            var part = partGO.AddComponent<Part>();
            parts.Add(part);
            part.craft = this;

            tasks.Add(LoadPartAsync(part, partConfig));
        }

        // load parts, calculate COM
        Newtonian.OnMassChanged -= RecalcMomentOfInertia;
        Newtonian.ZeroMass();
        while (tasks.Count > 0)
        {
            var finished = await Task.WhenAny(tasks);
            tasks.Remove(finished);
            var part = finished.Result;

            Newtonian.AddPointMass(part.craftPos, part.mass);
            foreach (var plugin in part.plugins)
            {
                if (typeof(MassivePartPlugin).IsAssignableFrom(plugin.GetType()))
                {
                    var massivePlugin = (MassivePartPlugin)plugin;
                    Newtonian.AddPointMass(part.craftPos, massivePlugin.Mass);
                    massivePlugin.OnMassChanged += massChange =>
                    {
                        Newtonian.AddPointMass(part.craftPos, massChange);
                    };
                }
            }
        }
        RecalcMomentOfInertia();
        Newtonian.OnMassChanged += RecalcMomentOfInertia;
    }

    /// <summary>
    /// recalculate <c>momentOfInertia</c>, automatically called when the craft's mass / center of mass changes
    /// </summary>
    public void RecalcMomentOfInertia()
    {
        Newtonian.momentOfInertia = 0.0;
        foreach (var part in parts)
        {
            double mass = part.mass;
            foreach (var plugin in part.plugins)
                if (typeof(MassivePartPlugin).IsAssignableFrom(plugin.GetType()))
                    mass += ((MassivePartPlugin)plugin).Mass;

            Newtonian.momentOfInertia += mass * (part.craftPos - Newtonian.CenterOfMass).magnitude;
        }
    }

    private void Start()
    {
        _trajectory = body.AddTrajectory(this);
    }

    private void FixedUpdate()
    {
        pos = orbit.GetPosition(); vel = orbit.GetVelocity();
    }

    private void Update()
    {
        transform.position = pos - CameraFocus.Instance.FocusPos;
        transform.eulerAngles = new Vector3(0, 0, (float)(Newtonian.angle * 180.0 / Math.PI));

        _partsGameObject.transform.localPosition = -Newtonian.CenterOfMass;
    }
}
