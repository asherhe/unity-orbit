using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DataObjectImporter))]
public class DataObjectImporterInspector : Editor
{
    private Dictionary<string, bool> _foldoutState = new Dictionary<string, bool>();

    private void OnEnable()
    {
        _foldoutState["$"] = true; // root object should be unfolded
    }

    public override void OnInspectorGUI()
    {
        DataObjectImporter importer = (DataObjectImporter)target;
        DataObject data = importer.data;

        EditorGUILayout.LabelField("Contained Data:", EditorStyles.boldLabel);
        RenderDataNode(data.root, "$"); // keep track of path as we go
    }

    private void RenderDataNode(DataNode node, string path)
    {
        switch (node.Type)
        {
            case DataNodeType.Mapping:
                RenderObject(node, path);
                break;
            case DataNodeType.Sequence:
                RenderArray(node, path);
                break;
            default:
                RenderValue(node, path);
                break;
        }
    }

    private void RenderObject(DataNode node, string path)
    {
        bool isExpanded = false;
        _foldoutState.TryGetValue(path, out isExpanded);
        isExpanded = EditorGUILayout.Foldout(isExpanded, $"Object ({node.Count} properties)");
        if (!(_foldoutState[path] = isExpanded)) return;

        EditorGUI.indentLevel++;
        foreach (var property in node.KeyValuePairs)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(property.key);
            EditorGUILayout.BeginVertical();
            RenderDataNode(property.value, $"{path}.{property.key}");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;
    }
    private void RenderArray(DataNode node, string path)
    {
        bool isExpanded = false;
        _foldoutState.TryGetValue(path, out isExpanded);
        isExpanded = EditorGUILayout.Foldout(isExpanded, $"Array ({node.Count})");
        if (!(_foldoutState[path] = isExpanded)) return;

        EditorGUI.indentLevel++;
        for (int i = 0; i < node.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel($"[{i}]");
            EditorGUILayout.BeginVertical();
            RenderDataNode(node[i], $"{path}[{i}]");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;
    }
    private void RenderValue(DataNode node, string path)
    {
        EditorGUILayout.LabelField(node.ToString(), EditorStyles.wordWrappedLabel);
    }
}
