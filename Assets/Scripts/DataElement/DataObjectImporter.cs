using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, "data")]
public class DataObjectImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var data = DataObject.LoadFile(ctx.assetPath);
        ctx.AddObjectToAsset("Data Object", data);
        ctx.SetMainObject(data);

        if (!EditorSettings.projectGenerationUserExtensions.Contains("data"))
        {
            var list = EditorSettings.projectGenerationUserExtensions.ToList();
            list.Add("data");
            EditorSettings.projectGenerationUserExtensions = list.ToArray();
        }
    }
}
