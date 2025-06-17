using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class DataNodeSerialization
{
    public static bool IsTypeSerializable(Type type)
        => type.IsPrimitive
        || type == typeof(string)
        || type.IsEnum
        || type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) // vector types are chill
        || (type.IsArray && IsTypeSerializable(type.GetElementType()))
        || (type.IsGenericType && (
                 (typeof(IList).IsAssignableFrom(type) && IsTypeSerializable(type.GetGenericArguments()[0]))
              || (typeof(IDictionary).IsAssignableFrom(type) && IsTypeSerializable(type.GetGenericArguments()[1]))
            ))
        || Attribute.IsDefined(type, typeof(SerializableAttribute)); // all other classes

    /// <summary>
    /// checks if a field can be serialized
    /// <para>serializable fields must be:</para>
    /// <list type="bullet">
    ///   <item>public OR have the <c>UnityEngine.SerializeField</c> attribute</item>
    ///   <item>not static</item>
    ///   <item>not const or readonly</item>
    /// </list>
    /// </summary>
    private static bool IsFieldSerializable(FieldInfo field)
        => (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
        && !field.IsStatic
        && !field.IsLiteral // is this field hardcoded in compile time? (consts)
        && !field.IsInitOnly
        && IsTypeSerializable(field.FieldType);

    /// <summary>
    /// summary TODO
    /// </summary>
    public static DataNode Serialize<T>(T obj)
    {
        return Serialize(typeof(T), obj);
    }

    public static DataNode Serialize(Type type, object obj)
    {
        if (!IsTypeSerializable(type))
            throw new NotSupportedException($"Serialization of {type} is currently not supported. If you are serializing a custom class, please add the [System.Serializable] attribute");

        if (typeof(ISerializationCallbackReceiver).IsAssignableFrom(type))
            ((ISerializationCallbackReceiver)obj).OnBeforeSerialize();

        if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            return new DataNode(obj.ToString());

        DataNode node;
        if (type.IsArray)
        {
            node = new DataNode(DataNodeType.Sequence);
            var elementType = type.GetElementType();
            foreach (var e in (Array)obj)
                node.Add(Serialize(elementType, e));
        }
        // lists
        else if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType)
        {
            node = new DataNode(DataNodeType.Sequence);
            var elementType = type.GetGenericArguments()[0];
            foreach (var e in (IList)obj)
                node.Add(Serialize(elementType, e));
        }
        else if (typeof(IDictionary).IsAssignableFrom(type) && type.IsGenericType)
        {
            node = new DataNode(DataNodeType.Mapping);
            var generics = type.GetGenericArguments();
            var keyType = generics[0]; var valType = generics[1];
            foreach (DictionaryEntry kvp in (IDictionary)obj)
                node.Add(
                    kvp.Key.ToString(),
                    Serialize(valType, kvp.Value)
                );
        }
        else
        {
            node = new DataNode(DataNodeType.Mapping);
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!IsFieldSerializable(field))
                    continue;

                var key = field.Name;
                // check for custom key
                var keyAttr = field.GetCustomAttribute<SerializationKeyAttribute>();
                if (keyAttr != null) key = keyAttr.Key;

                var val = field.GetValue(obj);
                if (!(field.GetCustomAttribute<OptionalValueFieldAttribute>() != null && val == null))
                    node[key] = Serialize(field.FieldType, val);
                else
                    node[key] = new DataNode("false"); // serialize as "false" for null OptionalValueFields
            }
        }
        return node;
    }

    /// <summary>
    /// deserializes a DataNode into an object. currently supports:
    /// <list type="bullet">
    ///   <item>
    ///     <term>objects with <c>[System.Serializable]</c></term>
    ///     <description>key-value pairs of a mapping DataNode fill in the properties of the object</description>
    ///   </item>
    ///   <item>
    ///     <term>arrays, lists</term>
    ///     <description>deserialized from the entires of a sequence DataNode</description>
    ///   </item>
    ///   <item>
    ///     <term>all types supported by <c>DataNode.As&lt;T&gt;()</c></term>
    ///     <description>scalar DataNodes will directly be converted to an object if possible</description>
    ///   </item>
    /// </list>
    /// 
    /// <para>only fields that meet the following requirements are serialized:</para>
    /// <list type="bullet">
    ///   <item>public OR have the <c>UnityEngine.SerializeField</c> attribute</item>
    ///   <item>not static</item>
    ///   <item>not const or readonly</item>
    /// </list>
    /// 
    /// <para>
    ///   for types that implement the <c>UnityEngine.ISerializationCallbackReceiver</c> interface,
    ///   OnAfterDeserialize() will be called after deserialization is complete
    /// </para>
    /// </summary>
    public static T Deserialize<T>(DataNode node)
    {
        return (T)Deserialize(typeof(T), node);
    }

    /// <summary>
    /// see Deserialize&lt;T&gt;()
    /// </summary>
    public static object Deserialize(Type type, DataNode node)
    {
        if (!IsTypeSerializable(type))
            throw new NotSupportedException($"Deserialization of {type} is currently not supported. If you are serializing a custom class, please add the [System.Serializable] attribute");

        object obj;
        switch (node.Type)
        {
            case DataNodeType.Scalar:
                obj = node.As(type);
                break;
            case DataNodeType.Sequence:
                obj = DeserializeSeq(type, node);
                break;
            case DataNodeType.Mapping:
                obj = DeserializeMap(type, node);
                break;
            default:
                throw new NotSupportedException($"Deserialization of a {node.Type} DataNode into a {type} is not supported.");
        }

        if (typeof(ISerializationCallbackReceiver).IsAssignableFrom(type))
            ((ISerializationCallbackReceiver)obj).OnAfterDeserialize();

        return obj;
    }

    private static object DeserializeSeq(Type type, DataNode node)
    {
        // arrays
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            var array = Array.CreateInstance(elementType, node.Count);
            for (int i = 0; i < node.Count; i++)
                array.SetValue(Deserialize(elementType, node[i]), i);
            return array;
        }
        // lists
        if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType)
        {
            var list = (IList)Activator.CreateInstance(type);
            var elementType = type.GetGenericArguments()[0];
            foreach (var entry in node)
                list.Add(Deserialize(elementType, entry));
            return list;
        }
        throw new InvalidOperationException($"Cannot deserialize a sequence DataNode to a {type}.");
    }
    private static object DeserializeMap(Type type, DataNode node)
    {
        var obj = Activator.CreateInstance(type);

        if (typeof(IDictionary).IsAssignableFrom(type) && type.IsGenericType)
        {
            var dictObj = (IDictionary)obj;
            var generics = type.GetGenericArguments();
            var keyType = generics[0]; var valType = generics[1];
            foreach (var kvp in node.KeyValuePairs)
                dictObj.Add(
                    new DataNode(kvp.Key).As(keyType), // also parses enums, vectors, etc. which is nice
                    Deserialize(valType, kvp.Value)
                );
            return dictObj;
        }

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!IsFieldSerializable(field))
                continue;

            var key = field.Name;
            // check for custom key
            var keyAttr = field.GetCustomAttribute<SerializationKeyAttribute>();
            if (keyAttr != null) key = keyAttr.Key;

            if (!node.ContainsKey(key)) continue;
            var valNode = node[key];
            object valObj = null;
            if (!(field.GetCustomAttribute<OptionalValueFieldAttribute>() != null
                && valNode.Type == DataNodeType.Scalar && valNode.Value == "false"))
                valObj = Deserialize(field.FieldType, valNode);
            field.SetValue(obj, valObj);
        }
        return obj;
    }
}

/// <summary>
/// define a custom key for serializing a field
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SerializationKeyAttribute : Attribute
{
    public string Key { get; }
    public SerializationKeyAttribute(string key) => Key = key;
}

/// <summary>
/// attribute for serialized fields whose values can possibly be null.
/// null fields are serialized as a scalar value "false"
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class OptionalValueFieldAttribute : Attribute { }
