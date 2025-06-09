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
        _foldoutState[""] = true; // root object should be unfolded
    }

    public override void OnInspectorGUI()
    {
        DataObjectImporter importer = (DataObjectImporter)target;
        DataObject data = importer.data;

        EditorGUILayout.LabelField("Contained Data:", EditorStyles.boldLabel);
        RenderDataNode(data.root);
    }

    private void RenderDataNode(DataNode node)
    {
        switch (node.Type)
        {
            case DataNodeType.Object:
                RenderObject(node);
                break;
            case DataNodeType.Array:
                RenderArray(node);
                break;
            default:
                RenderValue(node);
                break;
        }
    }

    private void RenderObject(DataNode node)
    {
        bool isExpanded = false;
        _foldoutState.TryGetValue(node.Path, out isExpanded);
        isExpanded = EditorGUILayout.Foldout(isExpanded, $"Object ({node.Count} properties)");
        if (!(_foldoutState[node.Path] = isExpanded)) return;

        EditorGUI.indentLevel++;
        foreach (var property in node.Properties)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(property.Name);
            EditorGUILayout.BeginVertical();
            RenderDataNode(property.Value);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;
    }
    private void RenderArray(DataNode node)
    {
        bool isExpanded = false;
        _foldoutState.TryGetValue(node.Path, out isExpanded);
        isExpanded = EditorGUILayout.Foldout(isExpanded, $"Array ({node.Count})");
        if (!(_foldoutState[node.Path] = isExpanded)) return;

        EditorGUI.indentLevel++;
        for (int i = 0; i < node.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel($"[{i}]");
            EditorGUILayout.BeginVertical();
            RenderDataNode(node[i]);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;
    }
    private void RenderValue(DataNode node)
    {
        EditorGUILayout.LabelField(node.ToString(), EditorStyles.wordWrappedLabel);
    }
}
