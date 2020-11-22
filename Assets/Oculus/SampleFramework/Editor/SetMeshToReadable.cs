using UnityEngine;
using UnityEditor;
using System;

public class SetMeshToReadable : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        ModelImporter importer = assetImporter as ModelImporter;
        importer.isReadable = false;
    }
}
