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
        var dobjs = new List<DataObject>();
        using (var reader = new StreamReader(ctx.assetPath))
        {
            var yaml = new YamlStream();
            yaml.Load(reader);

            foreach (var doc in yaml.Documents)
                dobjs.Add(DataObject.LoadDocument(doc));
        }

        // add all DataObjects
        for (int i = 0; i < dobjs.Count; i++)
            ctx.AddObjectToAsset($"Data Object {i + 1}", dobjs[i]);
        ctx.SetMainObject(dobjs[0]);

        // register extension if necessary
        if (!EditorSettings.projectGenerationUserExtensions.Contains("data"))
        {
            var list = EditorSettings.projectGenerationUserExtensions.ToList();
            list.Add("data");
            EditorSettings.projectGenerationUserExtensions = list.ToArray();
        }
    }
}
