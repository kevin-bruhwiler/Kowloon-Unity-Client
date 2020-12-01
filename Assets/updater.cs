using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEditor;
using SimpleJSON;


public class updater : MonoBehaviour
{

    public VRTeleporter teleporter;
    public OVRPlayerController player;
    public Button uploadButton;
    public Button quitButton;
    public Button downloadButton;
    public Canvas menu;
    private CharacterController cc;
    private string placedObjectDir = "Assets/Resources/RecentlyPlacedObjects/";

    // Start is called before the first frame update
    void Start()
    {
        cc = player.GetComponent<CharacterController>();
        cc.enabled = true;

        Button btn = uploadButton.GetComponent<Button>();
        btn.onClick.AddListener(UploadRecentlyPlacedObjects);

        btn = quitButton.GetComponent<Button>();
        btn.onClick.AddListener(Application.Quit);

        btn = downloadButton.GetComponent<Button>();
        btn.onClick.AddListener(DownloadObjectsAtBlock);
    }

    // Update is called once per frame
    void Update()
    {
        if (!menu.enabled)
        {
            if (OVRInput.Get(OVRInput.Button.Three))
            {
                teleporter.ToggleDisplay(true);
            }
            if (OVRInput.GetUp(OVRInput.Button.Three))
            {
                cc.enabled = false;
                teleporter.Teleport();
                teleporter.ToggleDisplay(false);
                cc.enabled = true;
            }
        }
    }

    void UploadRecentlyPlacedObjects()
    {
        GameObject[] placedObjects = GameObject.FindGameObjectsWithTag("RecentlyPlaced");
        var files = JSON.Parse("{}");
        foreach (GameObject go in placedObjects)
        {
            FilepathStorer fps = go.GetComponent<FilepathStorer>();
            int id = go.GetInstanceID();
            var file = JSON.Parse("{}");
            file["filepath"] = fps.GetFilepath();
            file["prefabName"] = fps.GetPrefabName();
            file["position"] = go.transform.position;
            file["rotation"] = go.transform.rotation;
            file["bundle"] = Convert.ToBase64String(File.ReadAllBytes(fps.GetFilepath()));

            files[""+id] = file;

            //string localPath = placedObjectDir + go.GetInstanceID() + ".prefab";
            //PrefabUtility.SaveAsPrefabAssetAndConnect(go, localPath, InteractionMode.UserAction);
        }

        /*
        DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/Resources/RecentlyPlacedObjects/");
        FileInfo[] info = dir.GetFiles("*.*");

        // Read prefab and all dependencies into byte strings
        var files = JSON.Parse("{}");
        foreach (FileInfo f in info)
        {
            string[] dependencies = AssetDatabase.GetDependencies(placedObjectDir + f.Name, true);
            foreach (string dependency in dependencies)
            {
                if (files[dependency] == null)
                    files[dependency] = Convert.ToBase64String(File.ReadAllBytes(dependency));
            }
        }
        */



        StartCoroutine(Post("http://localhost:5000/transactions/new/unsigned", files.ToString()));

        //foreach (FileInfo f in info)
        //    File.Delete(f.FullName);
    }

    void DownloadObjectsAtBlock()
    {
        Vector3 pos = player.transform.position;
        var location = JSON.Parse("{index: [" + pos[0] + ", " + pos[1] + ", " + pos[2] + "],}");
        StartCoroutine(Post("http://localhost:5000/grid/index", location.ToString()));
    }

    void PopulateWorld(JSONNode data)
    {
        //string downloadedDir = Application.dataPath + "/DownloadedObjects/"; ;
       // if (!Directory.Exists(downloadedDir))
        //    Directory.CreateDirectory(downloadedDir);

        for (int k = 0; k < data.Count; k++)
        {
            foreach (JSONNode o in data[k].Children)
            {
                foreach (KeyValuePair<string, JSONNode> kvp in (JSONObject)JSON.Parse(o))
                {
                    //string filepath = kvp.Key.Replace("Assets/Resources/RecentlyPlacedObjects/", downloadedDir);

                    Debug.Log(kvp.Key);
                    Debug.Log(kvp.Value);

                    File.WriteAllBytes(kvp.Value["filepath"], Convert.FromBase64String(kvp.Value["bundle"]));

                    var lab = AssetBundle.LoadFromFile(kvp.Value["filepath"]);

                    foreach (string assetName in lab.GetAllAssetNames())
                    {
                        if (assetName == kvp.Value["prefabName"])
                        {
                            var prefab = lab.LoadAsset<GameObject>(assetName);
                            Instantiate(prefab, kvp.Value["position"], kvp.Value["rotation"]);
                        }
                    }

                    lab.Unload(false);
                }

                //DirectoryInfo dir = new DirectoryInfo(downloadedDir);
                //FileInfo[] info = dir.GetFiles("*.*");
                //for (int i = 0; i < info.Length; i++)
                //    Instantiate(AssetDatabase.LoadAssetAtPath("DownloadedObjects/" + info[i].Name, typeof(UnityEngine.Object)) as GameObject);

                break;
            }
        }
        
    }

    IEnumerator Post(string url, string bodyJsonString)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        var response = JSON.Parse(request.downloadHandler.text);
        Debug.Log("Status Code: " + request.responseCode);
        if (response != null && response["type"] == "grid/index")
            PopulateWorld(response["block"]["data"]);
    }

}
