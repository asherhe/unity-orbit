using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class CelestialBody : MonoBehaviour, IOrbitingObject
{
    private CelestialBodyConfig _config;
    [Serializable]
    private class CelestialBodyConfig
    {
        /// <summary>
        /// name of this body
        /// </summary>
        public string name;
        /// <summary>
        /// display color for rendering things related to this body
        /// </summary>
        public Color color;

        /// <summary>
        /// radius of the body's sea level, in kilometers
        /// </summary>
        public double radius;
        /// <summary>
        /// gravitational acceleration at sea level, in m/s^2
        /// </summary>
        public double surfaceGravity;
        /// <summary>
        /// celestial body's rotational period, in hours
        /// </summary>
        public double dayLength;

        /// <summary>
        /// material config for the body's surface
        /// </summary>
        public MaterialUtils.MaterialProperties surfaceMaterial;

        /// <summary>
        /// orbital info, optional for the sun
        /// </summary>
        [Serialization.OptionalValueField]
        public OrbitInfo orbit;
        [Serializable]
        public class OrbitInfo
        {
            public string parent;
            public double semimajorAxis; // km
            public double eccentricity;
            public double longitudePeriapsis; // deg
            public double epochMeanAnom; // deg
            public double epochTime; // sec
        }

        /// <summary>
        /// config for atmosphere, if applicable
        /// </summary>
        [Serialization.OptionalValueField]
        public AtmInfo atmosphere;
        [Serializable]
        public class AtmInfo
        {
            /// <summary>
            /// altitude above sea level to the top of atmosphere.
            /// physical, optical effects are not simulated past this altitude.
            /// in kilometers.
            /// </summary>
            public double height;
            /// <summary>
            /// atmospheric pressure at sea level. in atm
            /// </summary>
            public double seaLevelPressure;
            /// <summary>
            /// how quickly the atmospheric pressure drops as altitude increases.
            /// this is the altitude at which air pressure is 1/e of sea level.
            /// in kilometers.
            /// </summary>
            public double scaleHeight;
            /// <summary>
            /// material config for the atmospheric material
            /// </summary>
            public MaterialUtils.MaterialProperties material;
        }
    }

    private GameObject _displayObject;
    private GameObject _surfaceObject;
    private GameObject _atmObject;
    private GameObject _trajectoriesObject;

    [SerializeField]
    private GameObject _trajectoryPrefab;
    [SerializeField]
    private GameObject _SOIPrefab;

    /// <summary>
    /// name of this celestial body, used for display and referencing
    /// </summary>
    public string bodyName { get; private set; }

    /// <summary>
    /// orbit of this body
    /// </summary>
    public Orbit orbit { get; private set; }
    /// <summary>
    /// celestial body that this body is a satellite of
    /// </summary>
    public CelestialBody parent { get => orbit.body; }
    /// <summary>
    /// radius of this celestial body's SOI
    /// </summary>
    public double soiRadius { get; private set; }

    /// <summary>
    /// mass of the celestial body, in kg
    /// </summary>
    public double mass { get; private set; }
    /// <summary>
    /// standard gravitational parameter of the celestial body, in <c>m^3 / s^2</c>
    /// </summary>
    public double GM { get; private set; }
    /// <summary>
    /// radius of this celestial body, in m
    /// </summary>
    public double radius { get; private set; }
    /// <summary>
    /// length of a day, in seconds
    /// </summary>
    public double dayLength { get; private set; }

    /// <summary>
    /// whether this celestial body has an atmosphere
    /// </summary>
    public bool hasAtmosphere { get; private set; }
    /// <summary>
    /// height of atmosphere (m)
    /// </summary>
    public double atmHeight { get; private set; }
    /// <summary>
    /// pressure at sea level (atm)
    /// </summary>
    public double atmSeaLevelPressure { get; private set; }
    /// <summary>
    /// scale height of atmosphere (m)
    /// </summary>
    public double atmScaleHeight { get; private set; }

    /* rendering parameters */
    public Material surfaceMaterial { get; private set; }
    public Material atmMaterial { get; private set; }
    private List<Material> _dynamicMaterials;

    public void LoadConfig(DataNode config)
    {
        /* read from config */
        _config = Serialization.DataNodeSerialization.Deserialize<CelestialBodyConfig>(config);
        bodyName = _config.name;

        radius = _config.radius * 1000.0;
        GM = _config.surfaceGravity * radius * radius;
        mass = GM / Universe.Instance.G;
        dayLength = _config.dayLength * 3600.0;

        if (_config.orbit != null)
        {
            var parent = CelestialBodyManager.Instance.celestialBodies[_config.orbit.parent];
            var a = _config.orbit.semimajorAxis * 1000.0;
            orbit = new Orbit(
                -Math.Sqrt(a * parent.GM * (1 - _config.orbit.eccentricity * _config.orbit.eccentricity)),
                _config.orbit.eccentricity,
                _config.orbit.longitudePeriapsis * Math.PI / 180.0,
                _config.orbit.epochMeanAnom * Math.PI / 180.0,
                _config.orbit.epochTime,
                parent
            );
            soiRadius = a * Math.Pow(mass / parent.mass, 0.4);

            Trajectory traj = parent.AddTrajectory(this);
            traj.GetComponent<MeshRenderer>().material.color = _config.color;
        }

        if ((hasAtmosphere = _config.atmosphere != null))
        {
            atmHeight = _config.atmosphere.height * 1000.0;
            atmSeaLevelPressure = _config.atmosphere.seaLevelPressure;
            atmScaleHeight = _config.atmosphere.scaleHeight * 1000.0;
        }

        _displayObject = new GameObject("Display");
        _displayObject.transform.parent = transform;
        _displayObject.transform.localPosition = Vector3.zero;

        MakeDisplay(_displayObject);
    }

    /// <summary>
    /// set celestial body-wide shader properties
    /// </summary>
    private void SetMaterialProperties(Material m)
    {
        m.SetFloat("_PlanetRad", (float)radius);
        m.SetFloat("_AtmHeight", (float)atmHeight);
        m.SetFloat("_AtmSeaLevelPressure", (float)atmSeaLevelPressure);
    }
    /// <summary>
    /// set shader properties that change every frame
    /// </summary>
    private void SetDynamicMaterialProperties()
    {
        // some placeholder values for these or else we get CS0165
        float sunIntensity = 20.0f;
        Vector4 sunDirection = Vector4.zero;
        if (orbit != null)
        {
            var heliocentric = orbit.GetHeliocentricPosition();
            sunIntensity = (float)(1.7e23 / heliocentric.Magnitude2);
            sunDirection = -heliocentric.Normalized;
            sunDirection.z = 0.2f; sunDirection.w = 0.0f;
        }
        foreach (var m in _dynamicMaterials)
        {
            if (orbit != null)
            {
                m.SetFloat("_SunIntensity", sunIntensity);
                m.SetVector("_SunDir", sunDirection);
            }
        }
    }

    public void MakeDisplay(GameObject displayObject)
    {
        Vector3[] verts = new Vector3[]
        {
            new Vector3(-1.0f, -1.0f),
            new Vector3(1.0f, -1.0f),
            new Vector3(1.0f, 1.0f),
            new Vector3(-1.0f, 1.0f),
        };
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 1.0f),
        };
        int[] tris = new int[]
        {
            0, 1, 2,
            0, 2, 3,
        };
        Mesh quadMesh = new Mesh();
        quadMesh.vertices = verts;
        quadMesh.triangles = tris;
        quadMesh.uv = uvs;

        _dynamicMaterials = new();

        _surfaceObject = new GameObject("Surface");
        _surfaceObject.transform.parent = displayObject.transform;
        _surfaceObject.transform.localPosition = Vector3.forward * 550.0f;
        _surfaceObject.transform.localScale = Vector3.one * (float)radius;
        MeshFilter surfaceMeshFilter = _surfaceObject.AddComponent<MeshFilter>();
        surfaceMeshFilter.mesh = quadMesh;
        MeshRenderer surfaceMeshRenderer = _surfaceObject.AddComponent<MeshRenderer>();
        Addressables.LoadAssetAsync<Material>(_config.surfaceMaterial.path).Completed += m =>
        {
            surfaceMeshRenderer.material = surfaceMaterial = m.Result;
            MaterialUtils.SetMaterialProperties(surfaceMeshRenderer.material, _config.surfaceMaterial.properties);
            if (hasAtmosphere)
                MaterialUtils.SetMaterialProperties(surfaceMeshRenderer.material, _config.atmosphere.material.properties);
            SetMaterialProperties(surfaceMeshRenderer.material);
            _dynamicMaterials.Add(surfaceMeshRenderer.material);
        };

        if (hasAtmosphere)
        {
            _atmObject = new GameObject("Atmosphere");
            _atmObject.transform.parent = displayObject.transform;
            _atmObject.transform.localPosition = Vector3.forward * 500.0f;
            _atmObject.transform.localScale = Vector3.one * (float)(radius + atmHeight);
            MeshFilter atmMeshFilter = _atmObject.AddComponent<MeshFilter>();
            atmMeshFilter.mesh = quadMesh;
            MeshRenderer atmMeshRenderer = _atmObject.AddComponent<MeshRenderer>();
            Addressables.LoadAssetAsync<Material>(_config.atmosphere.material.path).Completed += m =>
            {
                atmMeshRenderer.material = atmMaterial = m.Result;
                MaterialUtils.SetMaterialProperties(atmMeshRenderer.material, _config.atmosphere.material.properties);
                SetMaterialProperties(atmMeshRenderer.material);
                _dynamicMaterials.Add(atmMeshRenderer.material);
            };
        }

        if (orbit != null)
        {
            var soiObject = Instantiate(_SOIPrefab, _displayObject.transform);
            soiObject.transform.localScale = (2.0f * (float)soiRadius) * Vector3.one;
        }

        SetDynamicMaterialProperties();
    }

    private void FixedUpdate()
    {
        _displayObject.transform.rotation *= Quaternion.Euler(0.0f, 0.0f, (float)(-360.0 * Universe.Instance.fixedDeltaTime / dayLength));
    }

    private void Update()
    {
        transform.position = CameraFocus.Instance.GetRelativePosition(this);

        SetDynamicMaterialProperties();
    }

    /// <summary>
    /// add a new trajectory display for a satellite to this body
    /// </summary>
    /// <returns>newly created trajectory</returns>
    public Trajectory AddTrajectory(IOrbitingObject o)
    {
        if (o.orbit != null && o.orbit.body != this)
            throw new ArgumentException("Expected a direct satellite of celestial body.");

        if (_trajectoriesObject == null)
        {
            _trajectoriesObject = new GameObject("Trajectories");
            _trajectoriesObject.transform.parent = transform;
            _trajectoriesObject.transform.localPosition = Vector3.zero;
        }

        var trajObject = Instantiate(_trajectoryPrefab, _trajectoriesObject.transform);
        trajObject.name = $"Trajectory {o.ToString()}";
        trajObject.transform.localPosition = Vector3.zero;
        var trajectory = trajObject.GetComponent<Trajectory>();
        trajectory.Orbit = o;
        return trajectory;
    }

    /// <summary>
    /// transfer a preexisting Trajectory object to this celestial body
    /// </summary>
    /// <returns>the trajectory that was transferred</returns>
    public Trajectory AddTrajectory(Trajectory t)
    {
        if (t.Orbit.orbit != null && t.Orbit.orbit.body != this)
            throw new ArgumentException("Expected a direct satellite of celestial body.");

        t.transform.parent = _trajectoriesObject.transform;
        t.transform.localPosition = Vector3.zero;
        return t;
    }

    public override string ToString()
    {
        return $"[CelestialBody {bodyName}]";
    }
}
