using UnityEditor;
using System.IO;
public class CreateAssetBundles
{
    [MenuItem("Assets/Build Standalone AssetBundles")]
    static void BuildStandaloneAssetBundles()
    {
        BuildAssetBundles("Assets/AssetBundles/Standalone");
    }

    [MenuItem("Assets/Build Android AssetBundles")]
    static void BuildAllAndroidAssetBundles()
    {
        BuildAssetBundles("Assets/AssetBundles/Android", BuildTarget.Android);
    }

    [MenuItem("Assets/Build iOS AssetBundles")]
    static void BuildAllIOSAssetBundles()
    {
        BuildAssetBundles("Assets/AssetBundles/iOS", BuildTarget.iOS);
    }

    [MenuItem("Assets/Build Mac AssetBundles")]
    static void BuildAllMacAssetBundles()
    {
        BuildAssetBundles("Assets/AssetBundles/Mac", BuildTarget.StandaloneOSX);
    }

    static void BuildAssetBundles(string path, BuildTarget platform = BuildTarget.StandaloneWindows64)
    {
        // ensure the directory exists and build the bundle there
        PreBuildDirectoryCheck(path);
        BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.ChunkBasedCompression, platform);
    }


    static void PreBuildDirectoryCheck(string directory)
    {
        // if the directory doesn't exist, create it
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}