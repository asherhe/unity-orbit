using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serialization
{
    /// <summary>
    /// interface for deserialization
    /// </summary>
    /// <typeparam name="T">target deserialization type</typeparam>
    public interface IDataNodeDeserializer<T>
    {
        /// <summary>
        /// deserialize a datanode into type <c>T</c>
        /// </summary>
        public T Deserialize(DataNode node);
    }

    /// <summary>
    /// use this deserializer for all deserializations
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GlobalDeserializerAttribute : Attribute { }

    [GlobalDeserializer]
    public class AnimationCurveDeserializer : IDataNodeDeserializer<AnimationCurve>
    {
        public AnimationCurve Deserialize(DataNode node)
        {
            node.AssertNodeType(DataNodeType.Mapping);
            var curve = new AnimationCurve();
            foreach (var kvp in node.KeyValuePairs)
            {
                float time, value = kvp.Value.As<float>();
                if (!float.TryParse(kvp.Key, out time))
                    throw new FormatException($"Could not parse AnimationCurve time {kvp.Value} as a float.");
                curve.AddKey(time, value);
            }
            return curve;
        }
    }

    [GlobalDeserializer]
    public class ColorDeserializer : IDataNodeDeserializer<Color>
    {
        public Color Deserialize(DataNode node)
        {
            // TODO: mapping and sequence if i feel like it
            node.AssertNodeType(DataNodeType.Scalar);

            Color c;
            if (!ColorUtility.TryParseHtmlString(node.Value, out c))
                throw new FormatException($"Scalar node {node.Value} could not be converted to Color");
            return c;
        }
    }

    [GlobalDeserializer]
    public class GradientDeserializer : IDataNodeDeserializer<Gradient>
    {
        public Gradient Deserialize(DataNode node)
        {
            node.AssertNodeType(DataNodeType.Mapping);

            var colorNodes = node["colorKeys"];
            colorNodes.AssertNodeType(DataNodeType.Mapping);

            var colorKeys = new GradientColorKey[colorNodes.Count];
            var colorDeserializer = new ColorDeserializer();
            var i = 0;
            foreach (var kvp in colorNodes.KeyValuePairs)
            {
                if (!float.TryParse(kvp.Key, out colorKeys[i].time))
                    throw new FormatException($"Could not parse AnimationCurve time {kvp.Value} as a float.");
                colorKeys[i].color = colorDeserializer.Deserialize(kvp.Value);
                i++;
            }

            var alphaNodes = node["alphaKeys"];
            alphaNodes.AssertNodeType(DataNodeType.Mapping);

            var alphaKeys = new GradientAlphaKey[alphaNodes.Count];
            i = 0;
            foreach (var kvp in alphaNodes.KeyValuePairs)
            {
                if (!float.TryParse(kvp.Key, out alphaKeys[i].time))
                    throw new FormatException($"Could not parse AnimationCurve time {kvp.Value} as a float.");
                alphaKeys[i].alpha = kvp.Value.As<float>();
                i++;
            }

            var gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }
    }

    [GlobalDeserializer]
    public class MinMaxCurveDeserializer : IDataNodeDeserializer<ParticleSystem.MinMaxCurve>
    {
        public ParticleSystem.MinMaxCurve Deserialize(DataNode node)
        {
            node.AssertNodeType(DataNodeType.Sequence);
            if (node.Count != 2)
                throw new ArgumentException($"Expected 2 sequence entries in conversion to MinMaxCurve, got {node.Count}.");
            return new ParticleSystem.MinMaxCurve(node[0].As<float>(), node[1].As<float>());
        }
    }

    /// <summary>
    /// base class for vector deserializers, contains helper functions for deserializing vectors
    /// </summary>
    /// <typeparam name="TComponent">type of vector components</typeparam>
    public class VectorDeserializer<TComponent>
    {
        /// <summary>
        /// reads a DataNode and converts it into a vector of the desired format
        /// </summary>
        /// <param name="n">number of vector components</param>
        /// <param name="keys">mapping keys for the n components of the vector, in order</param>
        /// <param name="ctor">vector constructor, takes in several vector components and produces a vector</param>
        public static object ParseVector(DataNode node, int n, string[] keys, Func<TComponent[], object> ctor)
        {
            TComponent[] args = new TComponent[n];
            switch (node.Type)
            {
                case DataNodeType.Mapping:
                    for (int i = 0; i < n; i++)
                        args[i] = node[keys[i]].As<TComponent>();
                    break;
                case DataNodeType.Sequence:
                    for (int i = 0; i < n; i++)
                        args[i] = node[i].As<TComponent>();
                    break;
                default:
                    throw new ArgumentException($"Expected a mapping or sequence node for deserialization, got {node.Type}");
            }
            return ctor(args);
        }
    }
    [GlobalDeserializer]
    public class Vector2dDeserializer : VectorDeserializer<double>, IDataNodeDeserializer<Vector2d>
    {
        public Vector2d Deserialize(DataNode node)
        {
            return (Vector2d)ParseVector(node, 2, new string[] { "x", "y" }, v => new Vector2d(v[0], v[1]));
        }
    }
    [GlobalDeserializer]
    public class Vector2Deserializer : VectorDeserializer<float>, IDataNodeDeserializer<Vector2>
    {
        public Vector2 Deserialize(DataNode node)
        {
            return (Vector2)ParseVector(node, 2, new string[] { "x", "y" }, v => new Vector2(v[0], v[1]));
        }
    }
    [GlobalDeserializer]
    public class Vector3Deserializer : VectorDeserializer<float>, IDataNodeDeserializer<Vector3>
    {
        public Vector3 Deserialize(DataNode node)
        {
            return (Vector3)ParseVector(node, 3, new string[] { "x", "y", "z" }, v => new Vector3(v[0], v[1], v[2]));
        }
    }
    [GlobalDeserializer]
    public class Vector4Deserializer : VectorDeserializer<float>, IDataNodeDeserializer<Vector4>
    {
        public Vector4 Deserialize(DataNode node)
        {
            return (Vector4)ParseVector(node, 4, new string[] { "x", "y", "z", "w" }, v => new Vector4(v[0], v[1], v[2], v[3]));
        }
    }
}
