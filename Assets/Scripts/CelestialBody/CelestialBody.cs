using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CelestialBody : OrbitingObject
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
        public MaterialProperties surfaceMaterial;

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
            public MaterialProperties material;
        }
    }

    private GameObject _displayObject;
    private GameObject _surfaceObject;
    private GameObject _atmObject;
    private GameObject _soiObject;

    private UI.CelestialBodyLabel _mapLabel;

    [SerializeField]
    private GameObject _SOIPrefab;

    /// <summary>
    /// name of this celestial body, used for display and referencing
    /// </summary>
    public string bodyName { get; private set; }

    /// <summary>
    /// celestial body that this body is a satellite of
    /// </summary>
    public CelestialBody parent { get => orbit.body; }

    /// <summary>
    /// natural satellites of this body
    /// </summary>
    public List<CelestialBody> satellites;

    /// <summary>
    /// radius of this celestial body's SOI
    /// </summary>
    public double soiRadius { get; private set; } = double.PositiveInfinity;


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
    public Color color { get; private set; }

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

        satellites = new();

        color = _config.color;

        if (_config.orbit != null)
        {
            var parent = CelestialBodyManager.Instance.celestialBodies[_config.orbit.parent];
            var a = _config.orbit.semimajorAxis * 1000.0;
            orbit = new OrbitState(
                Math.Sqrt(a * parent.GM * (1 - _config.orbit.eccentricity * _config.orbit.eccentricity)),
                _config.orbit.eccentricity,
                _config.orbit.longitudePeriapsis * Math.PI / 180.0,
                _config.orbit.epochMeanAnom * Math.PI / 180.0,
                _config.orbit.epochTime,
                parent
            );
            orbit.owner = this;
            prop = new UniversalPropagator(orbit);
            parent.satellites.Add(this);
            soiRadius = a * Math.Pow(mass / parent.mass, 0.4);
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
            var heliocentric = GetHeliocentricPosition();
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
        _config.surfaceMaterial.LoadMaterial(m =>
        {
            surfaceMeshRenderer.material = m;
            _config.surfaceMaterial.SetMaterialProperties(surfaceMeshRenderer.material);
            if (hasAtmosphere)
                _config.atmosphere.material.SetMaterialProperties(surfaceMeshRenderer.material);
            SetMaterialProperties(surfaceMeshRenderer.material);
            _dynamicMaterials.Add(surfaceMeshRenderer.material);
        });

        if (hasAtmosphere)
        {
            _atmObject = new GameObject("Atmosphere");
            _atmObject.transform.parent = displayObject.transform;
            _atmObject.transform.localPosition = Vector3.forward * 500.0f;
            _atmObject.transform.localScale = Vector3.one * (float)(radius + atmHeight);
            MeshFilter atmMeshFilter = _atmObject.AddComponent<MeshFilter>();
            atmMeshFilter.mesh = quadMesh;
            MeshRenderer atmMeshRenderer = _atmObject.AddComponent<MeshRenderer>();
            _config.atmosphere.material.LoadMaterial(m =>
            {
                atmMeshRenderer.material = m;
                _config.atmosphere.material.SetMaterialProperties(atmMeshRenderer.material);
                SetMaterialProperties(atmMeshRenderer.material);
                _dynamicMaterials.Add(atmMeshRenderer.material);
            });
        }

        if (orbit != null)
        {
            _soiObject = Instantiate(_SOIPrefab, _displayObject.transform);
            _soiObject.transform.localScale = (2.0f * (float)soiRadius) * Vector3.one;
        }

        SetDynamicMaterialProperties();

        UI.MapLabelManager.WhenInstantiated(() =>
        {
            _mapLabel = UI.MapLabelManager.Instance.AddCelestialBody(this);
        });

        if (orbit != null)
        {
            UI.TrajectoryManager.WhenInstantiated(() =>
            {
                var trajectory = UI.TrajectoryManager.Instance.AddTrajectory(orbit);
                trajectory.name = $"Trajectory {this}";
                trajectory.Color = color;
                // we can afford to do high quality for celestial trajectories because they are static
                trajectory.quality = 1e-6;
            });
        }
    }

    /// <summary>
    /// get the current position of this body, with the sun at the origin
    /// </summary>
    public Vector2d GetHeliocentricPosition()
    {
        var parentPos = Vector2d.zero;
        if (parent.orbit != null) parentPos = parent.GetHeliocentricPosition();
        return parentPos + Position;
    }

    private void FixedUpdate()
    {
        _displayObject.transform.rotation *= Quaternion.Euler(0.0f, 0.0f, (float)(360.0 * Universe.Instance.fixedDeltaTime / dayLength));
    }

    private void Update()
    {
        transform.position = CameraFocus.Instance.GetRelativePosition(this);

        SetDynamicMaterialProperties();
    }

    public override string ToString() => $"CelestialBody {bodyName}";
}