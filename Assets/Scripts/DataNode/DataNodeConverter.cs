using System;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

public class DataNodeConverter
{
    /// <summary>
    /// construct a new DataNode tree from a YamlNode
    /// </summary>
    public static DataNode FromYamlNode(YamlNode yaml)
    {
        DataNode node;
        switch (yaml)
        {
            case YamlMappingNode map:
                node = new DataNode(DataNodeType.Mapping);
                foreach (var kvp in map.Children)
                {
                    node.Add(
                        ((YamlScalarNode)kvp.Key).Value,
                        FromYamlNode(kvp.Value)
                    );
                }
                return node;
            case YamlSequenceNode seq:
                node = new DataNode(DataNodeType.Sequence);
                foreach (var child in seq.Children)
                    node.Add(FromYamlNode(child));
                return node;
            case YamlScalarNode scalar:
                return new DataNode(scalar.Value);
            default:
                throw new NotSupportedException("Only mapping, sequence, and scalar YamlNodes are supported for conversion to DataNode.");
        }
    }

    /// <summary>
    /// construct a new YamlNode tree from a DataNode
    /// </summary>
    public static YamlNode ToYamlNode(DataNode node)
    {
        switch (node.Type)
        {
            case DataNodeType.Mapping:
                var mappingNode = new YamlMappingNode();
                foreach (var kvp in node.KeyValuePairs)
                {
                    mappingNode.Add(kvp.Key, ToYamlNode(kvp.Value));
                }
                return mappingNode;
            case DataNodeType.Sequence:
                var sequenceNode = new YamlSequenceNode();
                foreach (var entry in node)
                {
                    sequenceNode.Add(ToYamlNode(entry));
                }
                return sequenceNode;
            case DataNodeType.Scalar:
                return new YamlScalarNode(node.Value);
            default:
                throw new NotSupportedException("Only mapping, sequence, and scalar DataNodes are supported for conversion to YamlNode.");
        }
    }
}
