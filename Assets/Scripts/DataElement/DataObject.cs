using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Newtonsoft.Json.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DataObject : ScriptableObject
{
    public DataNode root;

    /// <summary>
    /// load a json file as a DataObject
    /// </summary>
    public static DataObject LoadJson(string path)
    {
        JToken t = JToken.Parse(File.ReadAllText(path));
        DataObject dobject = ScriptableObject.CreateInstance<DataObject>();
        dobject.root = DataNode.ParseToken(t);
        return dobject;
    }

    public override string ToString()
    {
        return root.ToString();
    }
}

public enum DataNodeType
{
    None,
    Object,
    Array,
    String,
    Int,
    Float,
    Bool,
    Null
};

[System.Serializable]
public class DataNode : IEnumerable<DataNode>, ISerializationCallbackReceiver
{
    [SerializeField] private DataNodeType _type;
    [SerializeField] private string _path;

    private Dictionary<string, DataNode> _properties; // objects

    // the below are for serialization
    [SerializeField] private List<string> _propertyNames;
    [SerializeReference] private List<DataNode> _propertyValues;

    [SerializeReference] private DataNode[] _elements; // arrays
    [SerializeField] private string _stringValue; // strings
    [SerializeField] private int _intValue; // ints
    [SerializeField] private double _doubleValue; // floats
    [SerializeField] private bool _boolValue; // bools

    public DataNodeType Type { get => _type; }

    /// <summary>
    /// json path from the root to this element
    /// </summary>
    /// <example>
    /// "planets[2].radius"
    /// </example>
    public string Path { get => _path; }

    /* object type */
    /// <summary>
    /// number of properties in this object
    /// </summary>
    public int Count { get => _properties.Count; }
    public DataNode this[string name] => _properties[name];

    /* array type */
    /// <summary>
    /// number of elements in this array
    /// </summary>
    public int Length { get => _elements.Length; }
    public DataNode this[int index] => _elements[index];

    /* string type */
    public string GetString() => _stringValue;

    /* int type */
    public int GetInt() => _intValue;

    /* float type */
    public float GetFloat() => (float)_doubleValue;
    public double GetDouble() => _doubleValue;

    /* boolean type */
    public bool GetBool() => _boolValue;

    /* non-primitive data, try to interpret string */

    /// <summary>
    /// parse a comma-separated string of 4 numbers as a Vector4
    /// </summary>
    public Vector4 ParseVector4()
    {
        string[] components = _stringValue.Split(new char[] { ' ', ',' }, System.StringSplitOptions.RemoveEmptyEntries);

        return new Vector4(
            float.Parse(components[0]),
            float.Parse(components[1]),
            float.Parse(components[2]),
            float.Parse(components[3])
        );
    }

    public AsyncOperationHandle<T> LoadAssetAsync<T>() => Addressables.LoadAssetAsync<T>(_stringValue);

    /* array enumeration */
    private class ArrayEnumerator : IEnumerator<DataNode>
    {
        private readonly DataNode _arrayData;
        private int _index = -1;

        public ArrayEnumerator(DataNode data) { _arrayData = data; }

        public DataNode Current { get => _arrayData[_index]; }
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_index < _arrayData.Length)
            {
                _index++;
                return true;
            }
            return false;
        }

        public void Reset() => _index = -1;

        public void Dispose() { }
    }
    public IEnumerator<DataNode> GetEnumerator() => new ArrayEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /* object enumeration */
    public class Property
    {
        public string Name { get; private set; }
        public DataNode Value { get; private set; }

        public Property(KeyValuePair<string, DataNode> kvp) { Name = kvp.Key; Value = kvp.Value; }
    }
    private class PropertiesEnumerator : IEnumerator<Property>
    {
        private Dictionary<string, DataNode>.Enumerator _dictEnumerator;
        public PropertiesEnumerator(Dictionary<string, DataNode>.Enumerator dictEnumerator) { _dictEnumerator = dictEnumerator; }

        public Property Current { get => new Property(_dictEnumerator.Current); }
        object IEnumerator.Current => Current;

        public bool MoveNext() => _dictEnumerator.MoveNext();

        public void Reset() => throw new NotSupportedException();

        public void Dispose() => _dictEnumerator.Dispose();
    }
    private class PropertiesEnumerable : IEnumerable<Property>
    {
        private readonly DataNode _objectData;
        public PropertiesEnumerable(DataNode data) { _objectData = data; }
        public IEnumerator<Property> GetEnumerator() => new PropertiesEnumerator(_objectData._properties.GetEnumerator());
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public IEnumerable<Property> Properties { get => new PropertiesEnumerable(this); }

    /* loading from json */
    public static DataNode ParseToken(JToken t)
    {
        DataNode d = new DataNode();
        d._path = t.Path;
        switch (t.Type)
        {
            case JTokenType.Object:
                d._type = DataNodeType.Object;
                d._properties = new Dictionary<string, DataNode>();
                foreach (JProperty property in ((JObject)t).Properties())
                {
                    d._properties.Add(property.Name, ParseToken(property.Value));
                }
                break;
            case JTokenType.Array:
                JArray arr = (JArray)t;
                d._elements = new DataNode[arr.Count];
                int i = 0;
                foreach (JToken element in arr)
                    d._elements[i] = ParseToken(element);
                break;
            case JTokenType.String:
                d._type = DataNodeType.String;
                d._stringValue = t.ToObject<string>();
                break;
            case JTokenType.Integer:
                d._type = DataNodeType.Int;
                d._intValue = t.ToObject<int>();
                break;
            case JTokenType.Float:
                d._type = DataNodeType.Float;
                d._doubleValue = t.ToObject<double>();
                break;
            case JTokenType.Boolean:
                d._type = DataNodeType.Bool;
                d._boolValue = t.ToObject<bool>();
                break;
            default:
                d._type = DataNodeType.None;
                break;
        }
        return d;
    }

    /* serialization */
    public void OnBeforeSerialize()
    {
        if (Type != DataNodeType.Object) return;

        if (_propertyNames == null) _propertyNames = new List<string>();
        else _propertyNames.Clear();

        if (_propertyValues == null) _propertyValues = new List<DataNode>();
        else _propertyValues.Clear();

        foreach (var kvp in _properties)
        {
            _propertyNames.Add(kvp.Key);
            _propertyValues.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        if (Type != DataNodeType.Object) return;

        _properties = new Dictionary<string, DataNode>();

        for (int i = 0; i < _propertyNames.Count; i++)
            _properties.Add(_propertyNames[i], _propertyValues[i]);
    }

    public override string ToString()
    {
        switch (Type)
        {
            case DataNodeType.Object:
                return "DataNode Object";
            case DataNodeType.Array:
                return "DataNode Array";
            case DataNodeType.String:
                return $"\"{_stringValue}\"";
            case DataNodeType.Int:
                return _intValue.ToString();
            case DataNodeType.Float:
                return _doubleValue.ToString();
            case DataNodeType.Bool:
                return _boolValue ? "true" : "false";
            case DataNodeType.Null:
                return "null";
            default:
                return base.ToString();
        }
    }
}
