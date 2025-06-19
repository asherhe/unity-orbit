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
        || type == typeof(DataNode)
        || type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) || type == typeof(Color) // default unity types
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
    public static bool IsFieldSerializable(FieldInfo field)
        => (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
        && !field.IsStatic
        && !field.IsLiteral // is this field hardcoded in compile time? (consts)
        && !field.IsInitOnly
        && IsTypeSerializable(field.FieldType);

    public static DataNode Serialize<T>(T obj) => new DataNodeSerializer().Serialize<T>(obj);
    public static DataNode Serialize(Type type, object obj) => new DataNodeSerializer().Serialize(type, obj);

    public static T Deserialize<T>(DataNode node) => new DataNodeDeserializer().Deserialize<T>(node);
    public static object Deserialize(Type type, DataNode node) => new DataNodeDeserializer().Deserialize(type, node);
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
