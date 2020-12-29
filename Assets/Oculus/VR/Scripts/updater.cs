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
using System.IO.Compression;


public class updater : MonoBehaviour
{

    public VRTeleporter teleporter;
    public OVRPlayerController player;
    public Button controlsButton;
    public Button uploadButton;
    public Button quitButton;
    public Canvas menu;
    public OVRGrabber rightHandGrabber;
    public Canvas controls;
    private CharacterController cc;
    private string storageDir;

    private string metadataPath;
    private JSONNode metadata;

    private Dictionary<string, string> baseBundles = new Dictionary<string, string>
        {
            { "cyberpunk1", "Cyberpunk/" },
            { "foliage1", "Foliage/" },
            { "light", "Lights/" },
            { "polygon", "Polygons/" },
            { "road", "Roads/" },
            { "streetprops", "Street Props/"},
            { "utopia", "Utopian/" }
        };

    // Start is called before the first frame update
    void Start()
    {
        cc = player.GetComponent<CharacterController>();
        cc.enabled = true;

        controls.enabled = false;

        storageDir = Application.persistentDataPath + "/LoadedAssetBundles/";

        Button btn = uploadButton.GetComponent<Button>();
        btn.onClick.AddListener(UploadRecentlyPlacedObjects);

        btn = quitButton.GetComponent<Button>();
        btn.onClick.AddListener(Application.Quit);

        btn = controlsButton.GetComponent<Button>();
        btn.onClick.AddListener(showControls);

        metadataPath = Application.persistentDataPath + "/metadata.json";

        DownloadObjectsAtBlock();
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

    void showControls()
    {
        controls.enabled = !controls.enabled;
    }

    void UploadRecentlyPlacedObjects()
    {
        GameObject[] placedObjects = GameObject.FindGameObjectsWithTag("RecentlyPlaced");
        List<(byte[], string)> bundles = new List<(byte[], string)>();
        var files = JSON.Parse("{}");
        foreach (GameObject go in placedObjects)
        {
            FilepathStorer fps = go.GetComponent<FilepathStorer>();
            go.tag = "Untagged";
            int id = go.GetInstanceID();
            var file = JSON.Parse("{}");
            file["filepath"] = fps.GetFilename();
            file["prefabName"] = fps.GetPrefabName();
            file["position"] = go.transform.position;
            file["rotation"] = go.transform.rotation;
            file["scale"] = go.transform.localScale;
            file["grabbable"] = go.GetComponent<OVRGrabbable>() != null;
            file["rigidbody"] = go.GetComponent<Rigidbody>() != null;
            file["meshcollider"] = go.GetComponent<MeshCollider>() != null;
            if (file["rigidbody"])
            {
                file["kinematic"] = go.GetComponent<Rigidbody>().isKinematic;
                file["gravity"] = go.GetComponent<Rigidbody>().useGravity;
            }
            if (file["meshcollider"])
                file["convex"] = go.GetComponent<MeshCollider>().convex;

            byte[] f = File.ReadAllBytes(fps.GetFilepath());            
            //Copy bundle to storage dir
            File.WriteAllBytes(storageDir + fps.GetFilename(), f);
            bundles.Add((f, file["filepath"]));
            //file["bundle"] = f; //ByteArrayToString(f);

            files[""+id] = file;
        }
        files["delete"] = rightHandGrabber.filesToDelete;

        StartCoroutine(MultipartPost("http://kowloon-env.eba-hc3agzzc.us-east-2.elasticbeanstalk.com//transactions/new/unsigned", files.ToString(), bundles));
        //StartCoroutine(Post("http://localhost:5000/transactions/new/unsigned", files.ToString()));
        //StartCoroutine(MultipartPost("http://localhost:5000/transactions/new/unsigned", files.ToString(), bundles));
    }

    public void DownloadObjectsAtBlock()
    {
        Vector3 pos = player.transform.position;
        string loc = "[" + Math.Truncate(pos[0] / 100) + ", " + Math.Truncate(pos[1] / 100) + ", " + Math.Truncate(pos[2] / 100) + "]";

        if (!File.Exists(metadataPath))
            File.WriteAllText(metadataPath, JSON.Parse("{}").ToString());

        metadata = JSON.Parse(File.ReadAllText(metadataPath));
        if (metadata[loc] == null)
            metadata[loc] = 0;

        var location = JSON.Parse("{index: " + loc + ", time: " + metadata[loc] + "}");
        StartCoroutine(PostGetFile("http://kowloon-env.eba-hc3agzzc.us-east-2.elasticbeanstalk.com//grid/index/bundles", location.ToString()));
        //StartCoroutine(PostGetFile("http://localhost:5000/grid/index/bundles", location.ToString()));

        PopulateWorld(JSON.Parse(File.ReadAllText(Application.persistentDataPath + "/" + loc + ".json")));
    }

    void SaveBundles(byte[] bundles)
    {
        if (bundles != null)
        {
            string zipPath = storageDir + "/temp.zip";
            File.WriteAllBytes(zipPath, bundles);
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (baseBundles.ContainsKey(entry.FullName))
                    {
                        entry.ExtractToFile(storageDir + baseBundles[entry.FullName] + entry.FullName);
                    }
                    entry.ExtractToFile(storageDir + entry.FullName);
                }
            }
            File.Delete(zipPath);
        }
    }

    void PopulateWorld(JSONNode data)
    {
        if (!Directory.Exists(storageDir))
            Directory.CreateDirectory(storageDir);
        
        if(data != null)
        {
            for (int k = 0; k < data.Count; k++)
            {
                foreach (JSONNode o in data[k].Children)
                {
                    foreach (KeyValuePair<string, JSONNode> kvp in (JSONObject)JSON.Parse(o))
                    {
                        if (kvp.Key == "delete")
                            continue;
                        var lab = AssetBundle.LoadFromFile(storageDir + kvp.Value["filepath"]);

                        foreach (string assetName in lab.GetAllAssetNames())
                        {
                            if (assetName == kvp.Value["prefabName"]) // && Physics.OverlapSphere(kvp.Value["position"], 0).Length == 1)
                            {
                                var prefab = lab.LoadAsset<GameObject>(assetName);
                                GameObject go = (GameObject)Instantiate(prefab, kvp.Value["position"], kvp.Value["rotation"]);
                                go.transform.localScale = kvp.Value["scale"];

                                FilepathStorer fps = go.AddComponent(typeof(FilepathStorer)) as FilepathStorer;
                                fps.SetFilepath(kvp.Value["filepath"]);
                                fps.SetID(kvp.Key);

                                if (kvp.Value["rigidbody"])
                                {
                                    Rigidbody rb = go.GetComponent<Rigidbody>();
                                    rb.useGravity = kvp.Value["gravity"];
                                    rb.isKinematic = kvp.Value["kinematic"];
                                }
                                else
                                {
                                    Destroy(go.GetComponent<Rigidbody>());
                                }
                                if (kvp.Value["meshcollider"])
                                {
                                    MeshCollider mc = go.GetComponent<MeshCollider>();
                                    mc.convex = kvp.Value["convex"];
                                }
                                else
                                {
                                    Destroy(go.GetComponent<MeshCollider>());
                                }
                                if (!kvp.Value["grabbable"])
                                    Destroy(go.GetComponent<OVRGrabbable>());
                            }
                        }

                        lab.Unload(false);
                    }

                    break;
                }
            }
        }
    }

    IEnumerator MultipartPost(string url, string bodyJsonString, List<(byte[], string)> bundles)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        foreach ((byte[], string) bundle in bundles)
        {
            formData.Add(new MultipartFormFileSection(bundle.Item2, bundle.Item1, bundle.Item2, null));
        }
        formData.Add(new MultipartFormDataSection(bodyJsonString));

        var request = UnityWebRequest.Post(url, formData);
        //request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        yield return request.SendWebRequest();
        var response = JSON.Parse(request.downloadHandler.text);
        Debug.Log(request.responseCode);
        Debug.Log(response);
    }

    IEnumerator PostGetFile(string url, string bodyJsonString)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log(request.responseCode);

        if (request.downloadHandler.data != null)
        {
            SaveBundles(request.downloadHandler.data);
            StartCoroutine(PostGetBlock("http://kowloon-env.eba-hc3agzzc.us-east-2.elasticbeanstalk.com//grid/index", bodyJsonString));
            //StartCoroutine(PostGetBlock("http://localhost:5000/grid/index", bodyJsonString));
        }
    }

    IEnumerator PostGetBlock(string url, string bodyJsonString)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log(request.responseCode);
        var response = JSON.Parse(request.downloadHandler.text);

        if (response != null)
        {
            PopulateWorld(response["block"]);

            Vector3 pos = player.transform.position;
            string loc = "[" + Math.Truncate(pos[0] / 100) + ", " + Math.Truncate(pos[1] / 100) + ", " + Math.Truncate(pos[2] / 100) + "]";
            File.WriteAllText(Application.persistentDataPath + "/" + loc + ".json", response["block"].ToString());

            metadata[loc] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            File.WriteAllText(metadataPath, metadata.ToString());
        }
    }



    static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }


    static string ByteArrayToString(byte[] ba)
    {
        return BitConverter.ToString(ba).Replace("-", "");
    }

    static byte[] StringToByteArray(String hex)
    {
        int NumberChars = hex.Length;
        byte[] bytes = new byte[NumberChars / 2];
        for (int i = 0; i < NumberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }

}
