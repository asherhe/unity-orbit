using Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public class MaterialProperties
{
    public enum PropertyType
    {
        Integer,
        Float,
        Texture2D,
        Texture2DArray,
        Texture3D,
        Cubemap,
        CubemapArray,
        Color,
        Vector
    };
    [Serializable]
    public class Property
    {
        public PropertyType type;
        public DataNode value;
    }

    /// <summary>
    /// path to addressable material
    /// </summary>
    public string path;
    /// <summary>
    /// properties of the material to assign by default
    /// </summary>
    public Dictionary<string, Property> properties;

    /// <summary>
    /// the material referenced by this MaterialProperties object
    /// </summary>
    public Material Material { get; private set; }
    /// <summary>
    /// invoked when LoadMaterial() finishes loading. comes with the loaded material as an argument
    /// </summary>
    public event Action<Material> OnMaterialLoaded;

    /// <summary>
    /// load the material at the addressable path <c>path</c>
    /// </summary>
    public void LoadMaterial()
    {
        Addressables.LoadAssetAsync<Material>(path).Completed += m =>
        {
            Material = m.Result;
            OnMaterialLoaded?.Invoke(Material);
        };
    }

    /// <summary>
    /// set the shader properties on a material based on the given properties
    /// </summary>
    /// <param name="m">material instance to assign properties on (note: not the material asset)</param>
    public void SetMaterialProperties(Material m)
    {
        if (properties == null) return;
        foreach (var kvp in properties)
        {
            var name = kvp.Key;
            var type = kvp.Value.type;
            var value = kvp.Value.value;
            switch (type)
            {
                case PropertyType.Integer:
                    m.SetInteger(name, value.As<int>());
                    break;
                case PropertyType.Float:
                    m.SetFloat(name, value.As<float>());
                    break;
                case PropertyType.Texture2D:
                case PropertyType.Texture2DArray:
                case PropertyType.Cubemap:
                case PropertyType.CubemapArray:
                    Addressables.LoadAssetAsync<Texture>(value.Value).Completed += tex =>
                    {
                        m.SetTexture(name, tex.Result);
                    };
                    break;
                case PropertyType.Color:
                    m.SetColor(name, DataNodeSerialization.Deserialize<Color>(value));
                    break;
                case PropertyType.Vector:
                    m.SetVector(name, DataNodeSerialization.Deserialize<Vector4>(value));
                    break;
                default:
                    throw new NotSupportedException($"Properties of type {type} are not supported.");
            }
        }
    }
}
