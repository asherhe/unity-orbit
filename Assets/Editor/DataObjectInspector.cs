using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DataObject))]
public class DataObjectInspector : Editor
{
    private Vector2 scrollPos;
    private string content;

    private void OnEnable()
    {
        content = File.ReadAllText(AssetDatabase.GetAssetPath(target.GetInstanceID()));
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("File Contents:", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.TextArea(content, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }
}
