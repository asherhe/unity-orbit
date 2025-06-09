using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Newtonsoft.Json.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using YamlDotNet.RepresentationModel;

public class DataObject : ScriptableObject
{
    public DataNode root;

    /// <summary>
    /// load a yaml file as a DataObject
    /// </summary>
    public static DataObject LoadFile(string path)
    {
        using (var reader = new StreamReader(path))
        {
            var yaml = new YamlStream();
            yaml.Load(reader);

            var yamlRoot = yaml.Documents[0].RootNode;
            DataObject dobject = ScriptableObject.CreateInstance<DataObject>();
            dobject.root = DataNode.ParseYamlNode(yamlRoot);
            return dobject;
        }
    }

    public override string ToString()
    {
        return root.ToString();
    }
}

public enum DataNodeType
{
    None,
    Mapping,
    Sequence,
    Scalar
};

[System.Serializable]
public class DataNode : IEnumerable<DataNode>, ISerializationCallbackReceiver
{
    [SerializeField] private DataNodeType _type;

    // mapping data
    private Dictionary<string, DataNode> _mapKVPs; // key-value pairs
    // the below are for serialization
    [SerializeField] private List<string> _mapKeys;
    [SerializeReference] private List<DataNode> _mapVals;

    // sequence data
    [SerializeReference] private List<DataNode> _seqEntries;

    // scalar data
    [SerializeField] private string _scalarValue;

    public DataNodeType Type { get => _type; }

    /// <summary>
    /// <para>mapping nodes: the number of key: value pairs</para>
    /// <para>sequence nodes: the number of entries</para>
    /// </summary>
    public int Count
    {
        get
        {
            switch (Type)
            {
                case DataNodeType.Mapping:
                    return _mapKVPs.Count;
                case DataNodeType.Sequence:
                    return _seqEntries.Count;
                default:
                    throw new InvalidOperationException("Cannot get property Count for non-mapping or sequence nodes");
            }
        }
    }

    /// <summary>
    /// checks to see if this node is of a required type and throws an error if it isn't
    /// </summary>
    /// <exception cref="InvalidOperationException">thrown if this node's type does not match <c>type</c></exception>
    private void AssertNodeType(DataNodeType type)
    {
        if (Type != type)
            throw new InvalidOperationException($"This operation is only valid for {type} DataNodes.");
    }

    /// <summary>
    /// retrieves the value corresponding to <c>key</c> in a mapping node
    /// </summary>
    public DataNode this[string key]
    {
        get
        {
            AssertNodeType(DataNodeType.Mapping);
            return _mapKVPs[key];
        }
    }

    /// <summary>
    /// retrieve the <c>i</c>-th entry in a sequence node
    /// </summary>
    public DataNode this[int i]
    {
        get
        {
            AssertNodeType(DataNodeType.Sequence);
            return _seqEntries[i];
        }
    }

    /* sequence enumeration */
    private class SeqEnumerator : IEnumerator<DataNode>
    {
        private IEnumerator<DataNode> _enumerator;
        public SeqEnumerator(IEnumerator<DataNode> enumerator) { _enumerator = enumerator; }
        public DataNode Current { get => _enumerator.Current; }
        object IEnumerator.Current => Current;
        public bool MoveNext() => _enumerator.MoveNext();
        public void Reset() => _enumerator.Reset();
        public void Dispose() => _enumerator.Dispose();
    }
    public IEnumerator<DataNode> GetEnumerator() => new SeqEnumerator(_seqEntries.GetEnumerator());
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /* object enumeration */
    public struct KeyValuePair
    {
        public readonly string key;
        public readonly DataNode value;
        public KeyValuePair(string key, DataNode value) { this.key = key; this.value = value; }
    }
    private class PropertiesEnumerator : IEnumerator<KeyValuePair>
    {
        private Dictionary<string, DataNode>.Enumerator _enumerator;
        public PropertiesEnumerator(Dictionary<string, DataNode>.Enumerator enumerator) { _enumerator = enumerator; }
        public KeyValuePair Current
        {
            get
            {
                var kvp = _enumerator.Current;
                return new KeyValuePair(kvp.Key, kvp.Value);
            }
        }
        object IEnumerator.Current => Current;
        public bool MoveNext() => _enumerator.MoveNext();
        public void Reset() => throw new NotSupportedException();
        public void Dispose() => _enumerator.Dispose();
    }
    private class PropertiesEnumerable : IEnumerable<KeyValuePair>
    {
        private readonly DataNode _objectData;
        public PropertiesEnumerable(DataNode data) { _objectData = data; }
        public IEnumerator<KeyValuePair> GetEnumerator() => new PropertiesEnumerator(_objectData._mapKVPs.GetEnumerator());
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    /// <summary>
    /// get an <c>IEnumerable</c> for key-value pairs
    /// </summary>
    public IEnumerable<KeyValuePair> KeyValuePairs { get => new PropertiesEnumerable(this); }

    /* loading from yaml */

    /// <summary>
    /// construct a new DataNode tree from a YamlNode
    /// </summary>
    public static DataNode ParseYamlNode(YamlNode yaml)
    {
        DataNode data = new DataNode();
        switch (yaml)
        {
            case YamlMappingNode map:
                data._type = DataNodeType.Mapping;
                data._mapKVPs = new Dictionary<string, DataNode>();
                foreach (var kvp in map.Children)
                {
                    data._mapKVPs.Add(
                        ((YamlScalarNode)kvp.Key).Value,
                        ParseYamlNode(kvp.Value)
                    );
                }
                break;
            case YamlSequenceNode seq:
                data._type = DataNodeType.Sequence;
                data._seqEntries = new List<DataNode>();
                foreach (var child in seq.Children)
                    data._seqEntries.Add(ParseYamlNode(child));
                break;
            case YamlScalarNode scalar:
                data._type = DataNodeType.Scalar;
                data._scalarValue = scalar.Value;
                break;
            default:
                data._type = DataNodeType.None;
                break;
        }
        return data;
    }

    /* serialization */
    public void OnBeforeSerialize()
    {
        if (Type != DataNodeType.Mapping) return;

        if (_mapKeys == null) _mapKeys = new List<string>();
        else _mapKeys.Clear();

        if (_mapVals == null) _mapVals = new List<DataNode>();
        else _mapVals.Clear();

        foreach (var kvp in _mapKVPs)
        {
            _mapKeys.Add(kvp.Key);
            _mapVals.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        if (Type != DataNodeType.Mapping) return;

        _mapKVPs = new Dictionary<string, DataNode>();

        for (int i = 0; i < _mapKeys.Count; i++)
            _mapKVPs.Add(_mapKeys[i], _mapVals[i]);
    }

    /* scalar conversion */

    /// <summary>
    /// loads an addressable asset with the same key as the content of this scalar node
    /// </summary>
    /// <returns>operation handle for the requested asset</returns>
    public AsyncOperationHandle<T> LoadAddressableAsync<T>()
    {
        AssertNodeType(DataNodeType.Scalar);
        return Addressables.LoadAssetAsync<T>(_scalarValue);
    }

    /// <summary>
    /// convert a scalar to some object
    /// </summary>
    /// <exception cref="InvalidCastException">thrown if the conversion to type <c>T</c> is not supported</exception>
    public T ToObject<T>()
    {
        AssertNodeType(DataNodeType.Scalar);
        var type = typeof(T);

        try
        {
            return (T)Convert.ChangeType(_scalarValue, type); // for default types
        }
        catch (Exception e)
        {
            // if we have an InvalidCastException, then we'll try the other types. otherwise, the exception does its thing
            if (e is not InvalidCastException)
                throw e;
        }

        if (type == typeof(Vector2d)) return (T)(object)ParseVector2d();
        if (type == typeof(Vector2)) return (T)(object)ParseVector<Vector2>(2, v => new Vector2(v[0], v[1]));
        if (type == typeof(Vector3)) return (T)(object)ParseVector<Vector3>(3, v => new Vector3(v[0], v[1], v[2]));
        if (type == typeof(Vector4)) return (T)(object)ParseVector<Vector4>(4, v => new Vector4(v[0], v[1], v[2], v[3]));

        throw new InvalidCastException($"Conversion of scalar node to {type} is not supported.");
    }

    private Vector2d ParseVector2d()
    {
        var components = _scalarValue.Split(new char[] { ' ', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (components.Length != 2)
            throw new FormatException($"Expected exactly 2 components in scalar '{_scalarValue}', found {components.Length}.");

        try
        {
            return new Vector2d(
                double.Parse(components[0]),
                double.Parse(components[1])
            );
        }
        catch (FormatException e)
        {
            throw new FormatException($"At least one component of '{_scalarValue}' could not be parsed as a double.", e);
        }
    }

    private T ParseVector<T>(int numComponents, Func<float[], T> constructor)
    {
        var components = _scalarValue.Split(new char[] { ' ', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (components.Length != numComponents)
            throw new FormatException($"Expected exactly {numComponents} components in scalar '{_scalarValue}', found {components.Length}.");

        var vals = new float[numComponents];
        for (int i = 0; i < numComponents; i++)
        {
            if (!float.TryParse(components[i], out vals[i]))
                throw new FormatException($"Could not parse component {i} in '{_scalarValue}' as a float.");
        }
        return constructor(vals);
    }

    public override string ToString()
    {
        switch (Type)
        {
            case DataNodeType.Mapping:
                return "DataNode Object";
            case DataNodeType.Sequence:
                return "DataNode Array";
            case DataNodeType.Scalar:
                return _scalarValue;
            default:
                return base.ToString();
        }
    }
}
