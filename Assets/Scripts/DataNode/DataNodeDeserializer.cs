using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Serialization
{
    /// <summary>
    /// a deserializer for DataNodes, with support for custom deserializers
    /// </summary>
    public class DataNodeDeserializer
    {
        private static Dictionary<Type, Func<DataNode, object>> _defaultDeserializers;
        private Dictionary<Type, Func<DataNode, object>> _deserializers;

        static DataNodeDeserializer()
        {
            _defaultDeserializers = new Dictionary<Type, Func<DataNode, object>>();

            // scan all types in all assemblies for implementations of IDataNodeDeserializer that have the GlobalDeserializer attribute
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.GetTypes())
                {
                    if (!type.IsDefined(typeof(GlobalDeserializerAttribute))) continue;
                    var iface = type.GetInterfaces().FirstOrDefault(
                        i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDataNodeDeserializer<>
                    ));
                    if (iface == null) continue;

                    var targetType = iface.GetGenericArguments()[0];
                    var instance = Activator.CreateInstance(type);
                    var deserializeMethod = iface.GetMethod("Deserialize");
                    _defaultDeserializers[targetType] = node => deserializeMethod.Invoke(instance, new object[] { node });
                }
            }
        }

        public DataNodeDeserializer()
        {
            _deserializers = new(_defaultDeserializers);
        }

        /// <summary>
        /// register a custom deserialization type
        /// </summary>
        /// <param name="deserializerType">deserializer type to add, implements <c>Serialization.IDataNodeDeserializer&lt;T&gt;</c></param>
        public void AddDeserializer(Type deserializerType)
        {
            var iface = deserializerType.GetInterfaces().FirstOrDefault(
                        i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDataNodeDeserializer<>
                    ));
            if (iface == null)
                throw new ArgumentException($"Custom deserialization type {deserializerType} must implement Serialization.IDataNodeDeserializer<T>.");

            var targetType = iface.GetGenericArguments()[0];
            var instance = Activator.CreateInstance(targetType);
            var deserializeMethod = iface.GetMethod("Deserialize");
            _defaultDeserializers[targetType] = node => deserializeMethod.Invoke(instance, new object[] { node });
        }
        /// <summary>
        /// register a custom deserialization function
        /// </summary>
        /// <param name="type">type this deserializer is intended for</param>
        /// <param name="deserializer">function that deserializes a DataNode into the given type</param>
        public void AddDeserializer(Type type, Func<DataNode, object> deserializer)
        {
            _deserializers[type] = deserializer;
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
        public T Deserialize<T>(DataNode node)
        {
            return (T)Deserialize(typeof(T), node);
        }

        /// <summary>
        /// see Deserialize&lt;T&gt;()
        /// </summary>
        public object Deserialize(Type type, DataNode node)
        {
            if (type == typeof(DataNode)) return node;

            if (_deserializers.TryGetValue(type, out var deserializer))
                return deserializer(node);

            if (!DataNodeSerialization.IsTypeSerializable(type))
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

        private object DeserializeSeq(Type type, DataNode node)
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
        private object DeserializeMap(Type type, DataNode node)
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
                if (!(DataNodeSerialization.IsFieldSerializable(field) || _deserializers.ContainsKey(field.FieldType)))
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
}