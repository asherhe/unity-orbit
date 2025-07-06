using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DataNodeType
{
    None,
    Scalar,
    Sequence,
    Mapping,
};

[System.Serializable]
public class DataNode : IEnumerable<DataNode>, ISerializationCallbackReceiver
{
    [SerializeField] private DataNodeType _type;

    // mapping data
    private Dictionary<string, DataNode> _mapKVPs; // key-value pairs
    // the below are for serialization
    [SerializeReference] private List<string> _mapKeys;
    [SerializeReference] private List<DataNode> _mapVals;

    // sequence data
    [SerializeReference] private List<DataNode> _seqEntries;

    // scalar data
    [SerializeField] private string _scalarValue;

    /* constructors */

    public DataNode(DataNode other)
    {
        switch (_type = other.Type)
        {
            case DataNodeType.Scalar:
                _scalarValue = other._scalarValue;
                break;
            case DataNodeType.Sequence:
                _seqEntries = other._seqEntries;
                break;
            case DataNodeType.Mapping:
                _mapKVPs = other._mapKVPs;
                break;
            default:
                break;
        }
    }

    public DataNode(DataNodeType type)
    {
        _type = type;
        switch (Type)
        {
            case DataNodeType.Mapping:
                _mapKVPs = new Dictionary<string, DataNode>();
                break;
            case DataNodeType.Sequence:
                _seqEntries = new List<DataNode>();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// construct a mapping DataNode
    /// </summary>
    public DataNode(Dictionary<string, DataNode> keyValuePairs)
    {
        _type = DataNodeType.Mapping;
        _mapKVPs = new Dictionary<string, DataNode>(keyValuePairs); // make a copy
    }

    /// <summary>
    /// construct a sequence DataNode
    /// </summary>
    public DataNode(List<DataNode> entries)
    {
        _type = DataNodeType.Sequence;
        _seqEntries = new List<DataNode>(entries); // make a copy
    }
    /// <summary>
    /// construct a sequence DataNode
    /// </summary>
    public DataNode(DataNode[] sequence)
    {
        _type = DataNodeType.Sequence;
        _seqEntries = sequence.ToList();
    }

    /// <summary>
    /// construct a scalar DataNode
    /// </summary>
    public DataNode(string scalarValue)
    {
        _type = DataNodeType.Scalar;
        Value = scalarValue;
    }

    public DataNodeType Type { get => _type; }

    /// <summary>
    /// checks to see if this node is of a required type and throws an error if it isn't
    /// </summary>
    /// <exception cref="InvalidOperationException">thrown if this node's type does not match <c>type</c></exception>
    public void AssertNodeType(DataNodeType type)
    {
        if (Type != type)
            throw new InvalidOperationException($"This operation is only valid for {type} DataNodes.");
    }

    /// <summary>
    /// number of children in a datanode
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
    /// the value of a scalar DataNode
    /// </summary>
    public string Value
    {
        get
        {
            AssertNodeType(DataNodeType.Scalar);
            return _scalarValue;
        }
        set
        {
            AssertNodeType(DataNodeType.Scalar);
            _scalarValue = value;
        }
    }

    /* sequence methods */

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
        set
        {
            AssertNodeType(DataNodeType.Sequence);
            _seqEntries[i] = value;
        }
    }

    /// <summary>
    /// add a new entry to a sequence DataNode
    /// </summary>
    public void Add(DataNode entry)
    {
        AssertNodeType(DataNodeType.Sequence);
        _seqEntries.Add(entry);
    }

    /* mapping methods */

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
        set
        {
            AssertNodeType(DataNodeType.Mapping);
            _mapKVPs[key] = value;
        }
    }

    /// <summary>
    /// add a key-value pair to a mapping DataNode
    /// </summary>
    public void Add(string key, DataNode value)
    {
        AssertNodeType(DataNodeType.Mapping);
        _mapKVPs.Add(key, value);
    }

    public bool ContainsKey(string key)
    {
        AssertNodeType(DataNodeType.Mapping);
        return _mapKVPs.ContainsKey(key);
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
    public class KeyValuePair
    {
        private string _key;
        private DataNode _value;
        public string Key { get => _key; }
        public DataNode Value { get => _value; }
        public KeyValuePair(string key, DataNode value) { _key = key; _value = value; }
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
        public PropertiesEnumerable(DataNode node) { _objectData = node; }
        public IEnumerator<KeyValuePair> GetEnumerator() => new PropertiesEnumerator(_objectData._mapKVPs.GetEnumerator());
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    /// <summary>
    /// get an <c>IEnumerable</c> for key-value pairs
    /// </summary>
    public IEnumerable<KeyValuePair> KeyValuePairs { get => new PropertiesEnumerable(this); }

    /* serialization (for unity, custom datanode serialization is in DataNodeSerializer) */
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

    /* cloning */

    /// <summary>
    /// create a deep clone of this DataNode.
    /// recursively create a copy of this DataNode as well as all child DataNodes as well.
    /// for shallow cloning, use the copy constructor DataNode(DataNode);
    /// </summary>
    public DataNode DeepClone()
    {
        DataNode clone;
        switch (Type)
        {
            case DataNodeType.Scalar:
                clone = new DataNode(Value);
                break;
            case DataNodeType.Sequence:
                clone = new DataNode(DataNodeType.Sequence);
                foreach (var entry in this)
                    clone.Add(entry.DeepClone());
                break;
            case DataNodeType.Mapping:
                clone = new DataNode(DataNodeType.Mapping);
                foreach (var kvp in KeyValuePairs)
                    clone.Add(kvp.Key, kvp.Value.DeepClone());
                break;
            default:
                throw new NotSupportedException($"Deep cloning of {Type} DataNodes is not supported.");
        }
        return clone;
    }

    /* scalar conversion */

    /// <summary>
    /// convert a scalar to some type.
    /// <para>supported types:</para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>any instance of <c>IConvertible</c></term>
    ///     <description>
    ///       includes common C# types such as int, float, double, bool, string. uses
    ///       Convert.ChangeType to convert in this case.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>enums</term>
    ///     <description>
    ///       two formats are supported for enum types. if a numeric value is given, it will be converted
    ///       as the underlying value of the enum. otherwise, it will be directly parsed as an enum
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    /// <exception cref="InvalidCastException">thrown if the conversion to type <c>T</c> is not supported</exception>
    public T As<T>() => (T)As(typeof(T));

    /// <summary>
    /// see As&lt;T&gt;()
    /// </summary>
    public object As(Type type)
    {
        AssertNodeType(DataNodeType.Scalar);

        try
        {
            return Convert.ChangeType(Value, type); // for default types
        }
        catch (Exception e)
        {
            // if we have an InvalidCastException, then we'll try the other types. otherwise, the exception does its thing
            if (e is not InvalidCastException)
                throw e;
        }

        // two supported formats for enums
        //  - numbers - convert as the underlying value for the enum
        //  - strings - parse as a string
        if (type.IsEnum)
        {
            try
            {
                var numVal = Convert.ChangeType(Value, type.GetEnumUnderlyingType());
                return Enum.ToObject(type, numVal);
            }
            catch (FormatException)
            {
                return Enum.Parse(type, Value);
            }
        }

        throw new InvalidCastException($"Conversion of scalar node to {type} is not supported.");
    }

    public override string ToString()
    {
        string s;
        switch (Type)
        {
            case DataNodeType.Mapping:
                s = $"{{ {string.Join(", ", KeyValuePairs.Select(kvp => $"{kvp.Key}: {kvp.Value}"))} }}";
                break;
            case DataNodeType.Sequence:
                s = $"[ {string.Join(", ", this.Select(node => node.ToString()))} ]";
                break;
            case DataNodeType.Scalar:
                s = Value;
                break;
            default:
                s = base.ToString();
                break;
        }
        return s;
    }
}
