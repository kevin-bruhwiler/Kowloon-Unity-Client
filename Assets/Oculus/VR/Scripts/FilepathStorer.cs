using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilepathStorer : MonoBehaviour
{

    private string filepath;
    private string prefabName;
    private string filename;
    private string id;

    public void SetFilepath(string fp)
    {
        filepath = fp;
    }

    public string GetFilepath()
    {
        return filepath;
    }

    public void SetFilename(string fn)
    {
        filename = fn;
    }

    public string GetFilename()
    {
        return filename;
    }

    public void SetPrefabName(string pn)
    {
        prefabName = pn;
    }

    public string GetPrefabName()
    {
        return prefabName;
    }

    public void SetID(string ID)
    {
        id = ID;
    }

    public string GetID()
    {
        return id;
    }
}
