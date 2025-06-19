using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class DataNodeSerializer
{
    public DataNode Serialize<T>(T obj)
    {
        return Serialize(typeof(T), obj);
    }

    public DataNode Serialize(Type type, object obj)
    {
        if (type == typeof(DataNode)) return (DataNode)obj;

        if (typeof(ISerializationCallbackReceiver).IsAssignableFrom(type))
            ((ISerializationCallbackReceiver)obj).OnBeforeSerialize();

        if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            return new DataNode(obj.ToString());

        if (!DataNodeSerialization.IsTypeSerializable(type))
            throw new NotSupportedException($"Serialization of {type} is currently not supported. If you are serializing a custom class, please add the [System.Serializable] attribute");

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
                if (!DataNodeSerialization.IsFieldSerializable(field))
                    continue;

                if (field.GetValue(obj) == null)
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
}
