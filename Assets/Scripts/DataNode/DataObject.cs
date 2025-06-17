using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YamlDotNet.RepresentationModel;

public class DataObject : ScriptableObject
{
    [SerializeField] private DataNode _root;
    public DataNode root { get => _root; }

    /// <summary>
    /// load a yaml document as a DataObject
    /// </summary>
    public static DataObject LoadDocument(YamlDocument doc)
    {
        var yamlRoot = doc.RootNode;
        DataObject dobject = ScriptableObject.CreateInstance<DataObject>();
        dobject._root = DataNodeConverter.FromYamlNode(yamlRoot);
        return dobject;
    }

    public override string ToString()
    {
        return root.ToString();
    }
}
