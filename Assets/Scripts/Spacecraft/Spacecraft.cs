using Parts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Spacecraft : MonoBehaviour, IOrbitingObject
{
    /// <summary>
    /// invoked when a spacecraft instance is loaded from config
    /// </summary>
    public static event Action<Spacecraft> OnSpacecraftLoaded;

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

    /// <summary>
    /// the name of this spacecraft
    /// </summary>
    public string craftName { get; private set; }

    /// <summary>
    /// deals with newtonian physics.
    /// spacecraft properties like mass, moment of inertia, etc. are found here.
    /// also deals with torque, forces, etc.
    /// </summary>
    public SpacecraftNewtonian Newtonian { get; private set; }

    /// <summary>
    /// list of parts that make up this spacecraft
    /// </summary>
    [HideInInspector]
    public List<Part> parts;
    /// <summary>
    /// parent object for all parts
    /// </summary>
    private GameObject _partsGameObject;

    public Orbit orbit { get; private set; }
    public CelestialBody body { get => orbit.body; }
    private Trajectory _trajectory;

    public Vector2d Pos { get => orbit.GetPosition(); }
    public Vector2d Vel { get => orbit.GetVelocity(); }
    public double Altitude { get => Pos.Magnitude - body.radius; }

    /// <summary>
    /// provides events and values that can be used to control this spacecraft.
    /// exists as another component on this GameObject
    /// the SpacecraftControl component is not responsible for actuating controls, it only serves to provide an interface to manipulate the spacecraft
    /// </summary>
    public SpacecraftControl Control { get; private set; }

    /// <summary>
    /// invoked when craft is fully loaded
    /// </summary>
    public event Action OnLoaded;
    /// <summary>
    /// invoked when the amount of resource someewhere in the craft changes
    /// </summary>
    public event Action<ResourceContainerPlugin, string, double> OnResourceChanged;

    /// <summary>
    /// find the part on this craft that has the given id
    /// </summary>
    /// <returns>the part, if it exists on this craft. otherwise, null</returns>
    public Part GetPartByID(string id)
    {
        int i = parts.FindIndex(p => p.id == id);
        if (i == -1) return null;
        return parts[i];
    }

    private void Awake()
    {
        Newtonian = gameObject.AddComponent<SpacecraftNewtonian>();
        Control = gameObject.AddComponent<SpacecraftControl>();

        IEnumerator OnLoadCoroutine(DataNode config)
        {
            var task = OnLoad(config);
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
        }
        StartCoroutine(OnLoadCoroutine(craftConfig.root));
    }

    public async Task OnLoad(DataNode config)
    {
        _config = Serialization.DataNodeSerialization.Deserialize<Config>(config);

        craftName = _config.name;

        // TODO: initialize fields
        Newtonian.angle = _config.rotation.angle;
        Newtonian.angularMomentum = _config.rotation.momentum;

        var parent = CelestialBodyManager.Instance.celestialBodies[_config.orbit.parent];
        orbit = new Orbit(
            _config.orbit.h,
            _config.orbit.e,
            _config.orbit.omega,
            _config.orbit.M0,
            _config.orbit.t0,
            parent // TODO: still using serialized inspector field, change this once we upgrade celestial bodies
        );
        _trajectory = TrajectoryManager.Instance.AddTrajectory(orbit);
        _trajectory.name = $"Trajectory {this}";

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

        // load parts, do operations on them as they load
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
                // update craft mass from plugin mass
                if (typeof(MassivePartPlugin).IsAssignableFrom(plugin.GetType()))
                {
                    var massivePlugin = (MassivePartPlugin)plugin;
                    Newtonian.AddPointMass(part.craftPos, massivePlugin.Mass);
                    massivePlugin.OnMassChanged += massChange =>
                    {
                        Newtonian.AddPointMass(part.craftPos, massChange);
                    };
                }

                // forward resource change events
                if (typeof(ResourceContainerPlugin).IsAssignableFrom(plugin.GetType()))
                {
                    var resourcePlugin = ((ResourceContainerPlugin)plugin);
                    resourcePlugin.OnResourceChanged += (type, diff) => OnResourceChanged?.Invoke(resourcePlugin, type, diff);
                }
            }
        }
        RecalcMomentOfInertia();
        Newtonian.OnMassChanged += RecalcMomentOfInertia;

        foreach (var part in parts) part.OnCraftPartsLoaded();

        OnLoaded?.Invoke();
        OnSpacecraftLoaded?.Invoke(this);
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

            Newtonian.momentOfInertia += mass * (part.craftPos - Newtonian.CenterOfMass).Magnitude;
        }
    }

    private void FixedUpdate()
    {
        //if (orbit.nextCapture != null)
        //    Debug.Log($"{orbit.nextCaptureBody} (soi {orbit.nextCaptureBody.soiRadius}): distance to body {(orbit.nextCaptureBody.orbit.GetPosition() - Pos).Magnitude}; time {Universe.Instance.UT - orbit.nextCapture.time}; distance to capture {(Pos - orbit.nextCapture.pos).Magnitude}");
        Debug.DrawRay(Vector3.zero, Vel);
        orbit.CheckBodyChange();
    }

    private void Update()
    {
        transform.position = CameraFocus.Instance.GetRelativePosition(this);
        transform.eulerAngles = new Vector3(0, 0, (float)(Newtonian.angle * 180.0 / Math.PI));

        _partsGameObject.transform.localPosition = -Newtonian.CenterOfMass;
    }

    public override string ToString()
    {
        return $"[Spacecraft {craftName}]";
    }
}
