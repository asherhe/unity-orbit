using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LensFlareConfiguration : MonoBehaviour
{
    public Sprite texture;
    public Color color = Color.white;
    public float intensity = 1.0f;
    public float axisDistance = 0.0f;
    public float radialDistortion = 0.0f;
    public bool autoRotate = false;

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private Mesh _mesh;

    private void Awake()
    {
        ApplyProperties();
    }

    private void OnValidate()
    {
        ApplyProperties();
    }

    private void ApplyProperties()
    {
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        if (_mesh == null) _mesh = GetComponent<MeshFilter>().sharedMesh;

        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e13f);

        _renderer.GetPropertyBlock(_propBlock);

        _propBlock.SetTexture("_MainTex", texture.texture);
        _propBlock.SetColor("_Color", color);
        _propBlock.SetFloat("_Intensity", intensity);
        _propBlock.SetFloat("_AxisDistance", axisDistance);
        _propBlock.SetFloat("_RadialDistortion", radialDistortion);
        _propBlock.SetInteger("_AutoRotate", autoRotate ? 1 : 0);

        // Apply the block to the renderer
        _renderer.SetPropertyBlock(_propBlock);
    }
}
