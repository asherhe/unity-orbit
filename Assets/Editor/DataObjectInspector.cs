using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DataObject))]
public class DataObjectInspector : Editor
{
    private Dictionary<string, bool> _foldoutState = new Dictionary<string, bool>();

    public override void OnInspectorGUI()
    {
        // we dont want it to be greyed out
        GUI.enabled = true;

        DataObject data = (DataObject)target;

        EditorGUILayout.LabelField("Contained Data:", EditorStyles.boldLabel);
        RenderDataNode(data.root, "$"); // keep track of path as we go
    }

    private void RenderDataNode(DataNode node, string path)
    {
        switch (node.Type)
        {
            case DataNodeType.Mapping:
                RenderMapping(node, path);
                break;
            case DataNodeType.Sequence:
                RenderSequence(node, path);
                break;
            default:
                RenderScalar(node, path);
                break;
        }
    }

    private void RenderMapping(DataNode node, string path)
    {
        foreach (var kvp in node.KeyValuePairs)
        {
            RenderKVP(kvp.key, kvp.value, $"{path}.{kvp.key}");
        }
    }
    private void RenderSequence(DataNode node, string path)
    {
        for (int i = 0; i < node.Count; i++)
        {
            RenderKVP($"Element {i}", node[i], $"{path}[{i}]");
        }
    }
    private void RenderKVP(string key, DataNode value, string path)
    {
        var guiLabelContent = new GUIContent(key, path);
        if (value.Type == DataNodeType.Scalar)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(guiLabelContent);
            RenderScalar(value, path);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            guiLabelContent.text = $"{guiLabelContent.text} ({value.Type})";
            bool isExpanded;
            if (!_foldoutState.TryGetValue(path, out isExpanded))
                isExpanded = true;
            isExpanded = EditorGUILayout.Foldout(isExpanded, guiLabelContent);
            if (!(_foldoutState[path] = isExpanded)) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.indentLevel++;
            RenderDataNode(value, path);
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }
    }
    private void RenderScalar(DataNode node, string path)
    {
        EditorGUILayout.LabelField(node.ToString(), EditorStyles.wordWrappedLabel);
    }
}
