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
    public Canvas menu;
    public OVRGrabber rightHandGrabber;
    private CharacterController cc;
    private string storageDir;

    // Start is called before the first frame update
    void Start()
    {
        cc = player.GetComponent<CharacterController>();
        cc.enabled = true;

        storageDir = Application.persistentDataPath + "/LoadedAssetBundles/";

        Button btn = uploadButton.GetComponent<Button>();
        btn.onClick.AddListener(UploadRecentlyPlacedObjects);

        btn = quitButton.GetComponent<Button>();
        btn.onClick.AddListener(Application.Quit);

        //btn = downloadButton.GetComponent<Button>();
        //btn.onClick.AddListener(DownloadObjectsAtBlock);
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

    void UploadRecentlyPlacedObjects()
    {
        GameObject[] placedObjects = GameObject.FindGameObjectsWithTag("RecentlyPlaced");
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
            file["bundle"] = ByteArrayToString(f);

            files[""+id] = file;
        }
        files["delete"] = rightHandGrabber.filesToDelete;

        StartCoroutine(Post("http://localhost:5000/transactions/new/unsigned", files.ToString()));
    }

    public void DownloadObjectsAtBlock()
    {
        Vector3 pos = player.transform.position;
        string metadataPath = Application.persistentDataPath + "/metadata.json";
        string loc = "[" + Math.Truncate(pos[0] / 500) + ", " + Math.Truncate(pos[1] / 500) + ", " + Math.Truncate(pos[2] / 500) + "]";

        if (!File.Exists(metadataPath))
            File.WriteAllText(metadataPath, JSON.Parse("{}").ToString());

        var metadata = JSON.Parse(File.ReadAllText(metadataPath));
        if (metadata[loc] == null)
            metadata[loc] = 0;

        var location = JSON.Parse("{index: " + loc + ", time: " + metadata[loc] + "}");
        StartCoroutine(Post("http://localhost:5000/grid/index", location.ToString()));
        metadata[loc] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        File.WriteAllText(metadataPath, metadata.ToString());

        PopulateWorld(JSON.Parse(File.ReadAllText(Application.persistentDataPath + "/" + loc + ".json")), JSON.Parse("{}"));
    }

    void PopulateWorld(JSONNode data, JSONNode bundles)
    {
        if (!Directory.Exists(storageDir))
            Directory.CreateDirectory(storageDir);

        for (int k = 0; k < bundles.Count; k++)
        {
            File.WriteAllBytes(storageDir + bundles[k][0], StringToByteArray(bundles[k][1]));
        }

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
        {
            PopulateWorld(response["block"], response["bundles"]);

            Vector3 pos = player.transform.position;
            string loc = "[" + Math.Truncate(pos[0] / 500) + ", " + Math.Truncate(pos[1] / 500) + ", " + Math.Truncate(pos[2] / 500) + "]";
            File.WriteAllText(Application.persistentDataPath + "/" + loc + ".json", response["block"]["data"].ToString());
        }
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
