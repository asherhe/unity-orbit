using Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class MaterialUtils
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
    [Serializable]
    public class MaterialProperties
    {
        /// <summary>
        /// path to addressable material
        /// </summary>
        public string path;
        /// <summary>
        /// properties of the material to assign by default
        /// </summary>
        public Dictionary<string, Property> properties;
    }

    /// <summary>
    /// set the shader properties on a material based on the given properties
    /// </summary>
    /// <param name="m">material instance to assign properties on (note: not the material asset)</param>
    /// <param name="properties">shader properties to assign</param>
    public static void SetMaterialProperties(Material m, Dictionary<string, Property> properties)
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
