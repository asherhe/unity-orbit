using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class CelestialBody : MonoBehaviour
{
    public DataObject configData;

    private CelestialBodyConfig _config;
    private class CelestialBodyConfig
    {
        public string name;

        // radius of sea level, ignores terrain
        public double radius; // km
        // gravitational acceleration at sea level
        public double surfaceG; // m/s^2
        // celestial body's rotational period
        public double dayLength; // julian days

        // asset to use for rendering the planet's surface
        public string surfaceMaterial; // addressable

        // orbital info, optional for the sun
        [OptionalValueField]
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

        // atmosphere info
        [OptionalValueField]
        public AtmInfo atmosphere;
        [Serializable]
        public class AtmInfo
        {
            // altitude above sea level to the top of atmosphere
            // physical, optical effects are not simulated past this altitude
            public double height; // km

            public double seaLevelPressure; // atm
            // how quickly the atmospheric pressure drops as altitude increases
            // this is the altitude at which air pressure is 1/e of sea level
            public double scaleHeight; // km

            // rendering config

            // material for rendering atmospheric scattering
            public string material; // addressable
            // atmospheric scattering parameters
            public Vector4 rayleighScattering;
            public Vector4 mieScattering;
            public float miePhaseG;
        }
    }

    private GameObject _displayObject;
    private GameObject _surfaceObject;
    private GameObject _atmObject;
    private GameObject _trajectoriesObject;

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

    private void Awake()
    {
        /* read from config */
        _config = DataNodeSerialization.Deserialize<CelestialBodyConfig>(configData.root);
        radius = _config.radius * 1000.0;
        GM = _config.surfaceG * radius * radius;
        mass = GM / Universe.Instance.G;
        dayLength = _config.dayLength * 86400.0;

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

    // configure a shader's values to match the celestial body's
    private void SetShaderValues(Material m)
    {
        m.SetFloat("_PlanetRad", (float)radius);
        m.SetFloat("_AtmHeight", (float)atmHeight);
        m.SetFloat("_AtmSeaLevelPressure", (float)atmSeaLevelPressure);
        m.SetFloat("_AtmScaleHeight", (float)atmScaleHeight);
        if (_config.atmosphere != null)
        {
            m.SetVector("_RayleighScatteringCoeff", _config.atmosphere.rayleighScattering);
            m.SetVector("_MieScatteringCoeff", _config.atmosphere.mieScattering);
            m.SetFloat("_MiePhaseG", _config.atmosphere.miePhaseG);
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

        _surfaceObject = new GameObject("Surface");
        _surfaceObject.transform.parent = displayObject.transform;
        _surfaceObject.transform.localPosition = Vector3.forward * 550.0f;
        _surfaceObject.transform.localScale = Vector3.one * (float)radius;
        MeshFilter surfaceMeshFilter = _surfaceObject.AddComponent<MeshFilter>();
        surfaceMeshFilter.mesh = quadMesh;
        MeshRenderer surfaceMeshRenderer = _surfaceObject.AddComponent<MeshRenderer>();
        Addressables.LoadAssetAsync<Material>(_config.surfaceMaterial).Completed += m =>
        {
            surfaceMeshRenderer.material = surfaceMaterial = m.Result;
            SetShaderValues(surfaceMeshRenderer.material);
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
            Addressables.LoadAssetAsync<Material>(_config.atmosphere.material).Completed += m =>
            {
                atmMeshRenderer.material = atmMaterial = m.Result;
                SetShaderValues(atmMeshRenderer.material);
            };
        }
    }

    private void FixedUpdate()
    {
        transform.position = -ActiveCraftController.Instance.craft.pos;
        _displayObject.transform.rotation *= Quaternion.Euler(0.0f, 0.0f, (float)(-360.0 * Universe.Instance.fixedDeltaTime / dayLength));
    }

    public Trajectory AddTrajectory(IOrbitingObject o)
    {
        if (_trajectoriesObject == null)
        {
            _trajectoriesObject = new GameObject("Trajectories");
            _trajectoriesObject.transform.parent = transform;
            _trajectoriesObject.transform.localPosition = Vector3.zero;
        }

        GameObject newTrajectoryObject = new GameObject("Trajectory");
        newTrajectoryObject.transform.parent = _trajectoriesObject.transform;
        newTrajectoryObject.transform.localPosition = Vector3.zero;
        newTrajectoryObject.layer = LayerMask.NameToLayer("Show in Map");
        newTrajectoryObject.AddComponent<MeshFilter>();
        newTrajectoryObject.AddComponent<MeshRenderer>();
        Trajectory trajectory = newTrajectoryObject.AddComponent<Trajectory>();
        trajectory.o = o;

        return trajectory;
    }
}
