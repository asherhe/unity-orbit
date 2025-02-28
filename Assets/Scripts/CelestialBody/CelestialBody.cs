using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CelestialBody : MonoBehaviour
{
    public CelestialBodyData data;

    private GameObject displayObject;
    private GameObject surfaceObject;
    private GameObject atmObject;
    private GameObject trajectoriesObject;

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
    public double day { get; private set; }

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

    private void Awake()
    {
        /* read from data */
        radius = data.radius * 1000.0;
        GM = data.surfaceG * radius * radius;
        mass = GM / Universe.Instance.G;
        day = data.dayLength * 86400.0;

        if ((hasAtmosphere = data.hasAtmosphere))
        {
            atmHeight = data.atmosphereHeight * 1000.0;
            atmSeaLevelPressure = data.atmosphereSeaLevelPressure;
            atmScaleHeight = data.atmosphereScaleHeight * 1000.0;
        }

        displayObject = new GameObject("Display");
        displayObject.transform.parent = transform;
        displayObject.transform.localPosition = Vector3.zero;

        MakeDisplay(displayObject);
    }

    public void MakeDisplay(GameObject displayObject) {
        LinkedList<Material> materials = new LinkedList<Material>();

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

        surfaceObject = new GameObject("Surface");
        surfaceObject.transform.parent = displayObject.transform;
        surfaceObject.transform.localPosition = Vector3.forward * 550.0f;
        surfaceObject.transform.localScale = Vector3.one * (float)radius;
        MeshFilter surfaceMeshFilter = surfaceObject.AddComponent<MeshFilter>();
        surfaceMeshFilter.mesh = quadMesh;
        MeshRenderer surfaceMeshRenderer = surfaceObject.AddComponent<MeshRenderer>();
        surfaceMeshRenderer.material = data.surfaceMaterial;
        materials.AddLast(surfaceMeshRenderer.material);

        if (data.hasAtmosphere)
        {
            atmObject = new GameObject("Atmosphere");
            atmObject.transform.parent = displayObject.transform;
            atmObject.transform.localPosition = Vector3.forward * 500.0f;
            atmObject.transform.localScale = Vector3.one * (float)(radius + atmHeight);
            MeshFilter atmMeshFilter = atmObject.AddComponent<MeshFilter>();
            atmMeshFilter.mesh = quadMesh;
            MeshRenderer atmMeshRenderer = atmObject.AddComponent<MeshRenderer>();
            atmMeshRenderer.material = data.atmMaterial;
            materials.AddLast(atmMeshRenderer.material);
        }

        foreach (Material m in materials) {
            m.SetFloat("_PlanetRad", (float)radius);
            m.SetFloat("_AtmHeight", (float)atmHeight);
            m.SetFloat("_AtmSeaLevelPressure", (float)atmSeaLevelPressure);
            m.SetFloat("_AtmScaleHeight", (float)atmScaleHeight);
            m.SetVector("_RayleighScatteringCoeff", data.rayleighScattering);
        }
    }

    private void FixedUpdate()
    {
        transform.position = -ActiveCraftController.Instance.craft.pos;
        displayObject.transform.rotation *= Quaternion.Euler(0.0f, 0.0f, (float)(-360.0 * Universe.Instance.fixedDeltaTime / day));
    }

    public Trajectory AddTrajectory(IHasOrbit o)
    {
        if (trajectoriesObject == null)
        {
            trajectoriesObject = new GameObject("Trajectories");
            trajectoriesObject.transform.parent = transform;
            trajectoriesObject.transform.localPosition = Vector3.zero;
        }

        GameObject newTrajectoryObject = new GameObject("Trajectory");
        newTrajectoryObject.transform.parent = trajectoriesObject.transform;
        newTrajectoryObject.transform.localPosition = Vector3.zero;
        newTrajectoryObject.layer = LayerMask.NameToLayer("Show in Map");
        newTrajectoryObject.AddComponent<MeshFilter>();
        newTrajectoryObject.AddComponent<MeshRenderer>();
        Trajectory trajectory = newTrajectoryObject.AddComponent<Trajectory>();
        trajectory.o = o;

        return trajectory;
    }
}
