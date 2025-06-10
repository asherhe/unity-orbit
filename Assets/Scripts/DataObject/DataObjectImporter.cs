using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using YamlDotNet.RepresentationModel;

[ScriptedImporter(1, "data")]
public class DataObjectImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        // read all documents
        var dobjects = new List<DataObject>();
        using (var reader = new StreamReader(ctx.assetPath))
        {
            var yaml = new YamlStream();
            yaml.Load(reader);

            foreach (var doc in yaml.Documents)
                dobjects.Add(DataObject.LoadDocument(doc));
        }

        // add all DataObjects
        for (int i = 0; i < dobjects.Count; i++)
            ctx.AddObjectToAsset($"Data Object {i + 1}", dobjects[i]);
        ctx.SetMainObject(dobjects[0]);

        // register extension if necessary
        if (!EditorSettings.projectGenerationUserExtensions.Contains("data"))
        {
            var list = EditorSettings.projectGenerationUserExtensions.ToList();
            list.Add("data");
            EditorSettings.projectGenerationUserExtensions = list.ToArray();
        }
    }
}
