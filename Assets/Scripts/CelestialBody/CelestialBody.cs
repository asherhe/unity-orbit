using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CelestialBody : MonoBehaviour
{
    public CelestialBodyData data;

    private GameObject displayObject;
    private GameObject surfaceObject;
    private GameObject atmosphereObject;
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

        surfaceObject = new GameObject("Surface");
        surfaceObject.transform.parent = displayObject.transform;
        surfaceObject.transform.localPosition = Vector3.forward * 550.0f;
        surfaceObject.transform.localScale = Vector3.one * 2.0f * (float)radius;
        SpriteRenderer displaySpriteRenderer = surfaceObject.AddComponent<SpriteRenderer>();
        displaySpriteRenderer.sprite = data.baseSprite;
        displaySpriteRenderer.material = (Material)AssetDatabase.LoadAssetAtPath("Assets/Materials/Planet.mat", typeof(Material));
        displaySpriteRenderer.material.SetTexture("_SpecularTex", data.specularTex);

        if (data.hasAtmosphere)
        {
            atmosphereObject = new GameObject("Atmosphere");
            atmosphereObject.transform.parent = displayObject.transform;
            atmosphereObject.transform.localPosition = Vector3.forward * 500.0f;
            atmosphereObject.transform.localScale = Vector3.one * (float)(radius + atmHeight);

            Vector3[] verts = new Vector3[]
            {
                new Vector3(-1.0f, -1.0f),
                new Vector3(1.0f, -1.0f),
                new Vector3(1.0f, 1.0f),
                new Vector3(-1.0f, 1.0f),
            };
            int[] tris = new int[]
            {
                0, 1, 2,
                0, 2, 3,
            };
            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = tris;

            MeshFilter meshFilter = atmosphereObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            MeshRenderer meshRenderer = atmosphereObject.AddComponent<MeshRenderer>();
            meshRenderer.material = (Material)AssetDatabase.LoadAssetAtPath("Assets/Materials/Atmosphere.mat", typeof(Material));
            meshRenderer.material.SetFloat("_PlanetRad", (float)radius);
            meshRenderer.material.SetFloat("_AtmHeight", (float)atmHeight);
            meshRenderer.material.SetFloat("_AtmSeaLevelPressure", (float)atmSeaLevelPressure);
            meshRenderer.material.SetFloat("_AtmScaleHeight", (float)atmScaleHeight);
            meshRenderer.material.SetColor("_AtmColor", data.atmosphereColor);
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
        Trajectory trajectory = newTrajectoryObject.AddComponent<Trajectory>();
        trajectory.o = o;

        return trajectory;
    }
}
