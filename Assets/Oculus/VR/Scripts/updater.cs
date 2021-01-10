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
#if UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || UNITY_TVOS || UNITY_WEBGL || UNITY_WSA || UNITY_PS4 || UNITY_WII || UNITY_XBOXONE || UNITY_SWITCH
#define DISABLESTEAMWORKS
#endif

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

// The updater class is reponsible for uploading/downloading data to and from the server
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
    public Canvas loadingInfo;

    private CharacterController cc;
    private string storageDir;
    private string old_location;
    private Text loadingText;

    private string metadataPath;
    private JSONNode metadata;

    // The names and locations of all of the "base" asset bundles
    private Dictionary<string, string> baseBundles = new Dictionary<string, string>
        {
            { "cyberpunk1", "Cyberpunk/" },
            { "foliage1", "Foliage/" },
            { "light", "Lights/" },
            { "road", "Roads/" },
            { "streetprops", "Street Props/"},
            { "utopia", "Utopian/" },
            { "fantasy1", "Fantasy/" }
        };

    // Start is called before the first frame update
    void Start()
    {
        cc = player.GetComponent<CharacterController>();
        cc.enabled = true;

        controls.enabled = false;

        storageDir = Application.persistentDataPath + "/LoadedAssetBundles/";

        // Add listeners to all buttons
        Button btn = uploadButton.GetComponent<Button>();
        btn.onClick.AddListener(UploadRecentlyPlacedObjects);

        btn = quitButton.GetComponent<Button>();
        btn.onClick.AddListener(Application.Quit);

        btn = controlsButton.GetComponent<Button>();
        btn.onClick.AddListener(showControls);

        metadataPath = Application.persistentDataPath + "/metadata.json";

        loadingText = loadingInfo.GetComponent<Text>();
        loadingInfo.enabled = false;
        DownloadObjectsAtBlock();

        // Check occasionally to see if the user's location has changed
        old_location = GetLocation();
        InvokeRepeating("CheckLocation", 10.0f, 10.0f);

        Debug.Log(SteamUser.GetSteamID());
    }

    // Update is called once per frame
    void Update()
    {
        // Check for user teleportation if the menu is not open
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

    // Check to see if the user's location has changed, if so download objects for the new location
    private void CheckLocation()
    {
        string loc = GetLocation();
        if (loc != old_location)
        {
            old_location = loc;
            DownloadObjectsAtBlock();
        }
    }

    // Display/hide the controls image
    void showControls()
    {
        controls.enabled = !controls.enabled;
    }

    // Send all objects placed this session to the server, along with their associated asset bundles
    void UploadRecentlyPlacedObjects()
    {
        GameObject[] placedObjects = GameObject.FindGameObjectsWithTag("RecentlyPlaced");
        List<(byte[], string)> bundles = new List<(byte[], string)>();
        var files = JSON.Parse("{}");
        // Iterate all recently placed objects
        foreach (GameObject go in placedObjects)
        {
            FilepathStorer fps = go.GetComponent<FilepathStorer>();
            if (fps == null)
                continue;
            // Add all metadata about the object (position, rotation, bundle, etc...) to a json object to be sent to the server
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
            
            //Copy bundle to storage dir so that it does not need to be redownloaded
            File.WriteAllBytes(storageDir + fps.GetFilename(), f);
            // Add bytes from the appropriate asset bundle to the list of files that will be uploaded to the server
            bundles.Add((f, file["filepath"]));

            // id used to identify each different object in an upload
            files[""+id] = file;
            // Untag the object to repeated uploads in the same session will not duplicate objects
            go.tag = "Untagged";
        }
        // Get information about all objects that have been deleted this session
        files["delete"] = rightHandGrabber.filesToDelete;

        // Send the data to the AWS instance
        StartCoroutine(MultipartPost("http://kowloon-env.eba-hc3agzzc.us-east-2.elasticbeanstalk.com/transactions/new/unsigned", files.ToString(), bundles));
        //StartCoroutine(Post("http://localhost:5000/transactions/new/unsigned", files.ToString()));
        //StartCoroutine(MultipartPost("http://localhost:5000/transactions/new/unsigned", files.ToString(), bundles));
    }

    // Get the current location of the player
    private string GetLocation()
    {
        Vector3 pos = player.transform.position;
        return "[" + Math.Truncate(pos[0] / 500) + ", " + Math.Truncate(pos[1] / 500) + ", " + Math.Truncate(pos[2] / 500) + "]";
    }

    // Download all objects for the block the player is currently in
    public void DownloadObjectsAtBlock()
    {
        string loc = GetLocation();

        // If this is the first time the client has been run, the metadata file must be created
        if (!File.Exists(metadataPath))
            File.WriteAllText(metadataPath, JSON.Parse("{}").ToString());

        // Get the timestamp of the last time data was downloaded - this prevents repeated downloads of the same data
        metadata = JSON.Parse(File.ReadAllText(metadataPath));
        if (metadata[loc] == null)
            metadata[loc] = 0;

        var location = JSON.Parse("{index: " + loc + ", time: " + metadata[loc] + "}");
        // Send the user location and most recent download time to the server, to get any updates
        StartCoroutine(PostGetFile("http://kowloon-env.eba-hc3agzzc.us-east-2.elasticbeanstalk.com/grid/index/bundles", location.ToString()));
        //StartCoroutine(PostGetFile("http://localhost:5000/grid/index/bundles", location.ToString()));

        PopulateWorld(JSON.Parse(File.ReadAllText(Application.persistentDataPath + "/" + loc + ".json")));
    }

    // Save assset bundles, retrieved from the server, to the storage directory
    void SaveBundles(byte[] bundles)
    {
        if (bundles != null)
        {
            // All bundles are retrieved as a zipfile
            string zipPath = storageDir + "/temp.zip";
            if (!System.IO.Directory.Exists(storageDir))
                System.IO.Directory.CreateDirectory(storageDir);
            File.WriteAllBytes(zipPath, bundles);
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                // For each bundle in the zip file, extract it to the storage dir, and to one of the "base" bundle dirs if necessary
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (baseBundles.ContainsKey(entry.FullName))
                    {
                        if (!File.Exists(storageDir + baseBundles[entry.FullName] + entry.FullName))
                        {
                            if (!System.IO.Directory.Exists(storageDir + baseBundles[entry.FullName]))
                                System.IO.Directory.CreateDirectory(storageDir + baseBundles[entry.FullName]);
                            entry.ExtractToFile(storageDir + baseBundles[entry.FullName] + entry.FullName);
                        } 
                    }
                    if (!File.Exists(storageDir + entry.FullName))
                    {
                        entry.ExtractToFile(storageDir + entry.FullName);
                        var lab = AssetBundle.LoadFromFile(storageDir + entry.FullName);
                        if (lab == null)
                            File.Delete(storageDir + entry.FullName);
                        lab.Unload(false);
                    }
                }
            }
            // Delete the retrieved zip file to save space
            File.Delete(zipPath);
        }
    }

    // Instantiate all the objects for a given block
    void PopulateWorld(JSONNode data)
    {
        // Remove all populated items so that they are not duplicated
        ClearPopulatedItems();
        if (!Directory.Exists(storageDir))
            Directory.CreateDirectory(storageDir);
        
        if(data != null)
        {
            // Iterate all objects in the block's metadata
            for (int k = 0; k < data.Count; k++)
            {
                foreach (JSONNode o in data[k].Children)
                {
                    foreach (KeyValuePair<string, JSONNode> kvp in (JSONObject)JSON.Parse(o))
                    {
                        // Ignore deleted objects
                        if (kvp.Key == "delete")
                            continue;

                        // Load the appropriate asset bundle
                        if (!File.Exists(storageDir + kvp.Value["filepath"]))
                            continue;
                        var lab = AssetBundle.LoadFromFile(storageDir + kvp.Value["filepath"]);

                        foreach (string assetName in lab.GetAllAssetNames())
                        {
                            // Load the specified prefab from the asset bundle (I think this for loop may be unnecessary)
                            if (assetName == kvp.Value["prefabName"])
                            {
                                // Instantiate the prefab using its metadata
                                var prefab = lab.LoadAsset<GameObject>(assetName);
                                GameObject go = (GameObject)Instantiate(prefab, kvp.Value["position"], kvp.Value["rotation"]);
                                go.tag = "Populated";
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

                        // Free the resources used by the loaded asset bundle
                        lab.Unload(false);
                    }

                    break;
                }
            }
        }
    }

    // Clear all the populated items
    void ClearPopulatedItems()
    {
        GameObject[] populatedObjects = GameObject.FindGameObjectsWithTag("Populated");
        foreach (GameObject go in populatedObjects)
            Destroy(go);
    }

    // Send data to the server - asset bundles and block metadata
    IEnumerator MultipartPost(string url, string bodyJsonString, List<(byte[], string)> bundles)
    {
        // Display upload to the user
        loadingInfo.enabled = true;
        loadingText.text = "Uploading data for region " + GetLocation();
        loadingText.color = Color.blue;

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        // Add all asset bundle contents to the post
        foreach ((byte[], string) bundle in bundles)
        {
            formData.Add(new MultipartFormFileSection(bundle.Item2, bundle.Item1, bundle.Item2, null));
        }
        // Add the block metadata
        formData.Add(new MultipartFormDataSection(bodyJsonString));

        // Send the request
        var request = UnityWebRequest.Post(url, formData);
        yield return request.SendWebRequest();
        var response = JSON.Parse(request.downloadHandler.text);
        Debug.Log(request.responseCode);
        Debug.Log(response);

        // Tell the user if the upload was successful or not
        if (request.responseCode == 200)
        {
            loadingText.text = "Uploading successful!";
            loadingText.color = Color.green;
        } else
        {
            loadingText.text = "Uploading failed with error " + request.responseCode;
            loadingText.color = Color.red;
        }
        StartCoroutine(ClearText());
    }

    // Get the zipfile of asset bundles from the server
    IEnumerator PostGetFile(string url, string bodyJsonString)
    {
        loadingInfo.enabled = true;
        loadingText.text = "Downloading data for region " + GetLocation();
        loadingText.color = Color.blue;

        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        // Request the bundles for the current location
        yield return request.SendWebRequest();
        Debug.Log(request.responseCode);

        if (request.responseCode == 200)
        {
            // If the request is successful, save the retrieved bundles and request the metadata
            SaveBundles(request.downloadHandler.data);
            StartCoroutine(PostGetBlock("http://kowloon-env.eba-hc3agzzc.us-east-2.elasticbeanstalk.com/grid/index", bodyJsonString));
            //StartCoroutine(PostGetBlock("http://localhost:5000/grid/index", bodyJsonString));
        } else
        {
            loadingText.text = "Downloading asset bundles failed with error " + request.responseCode;
            loadingText.color = Color.red;
            StartCoroutine(ClearText());
        }
    }

    // Get the metadata for the user's current block
    IEnumerator PostGetBlock(string url, string bodyJsonString)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log(request.responseCode);

        if (request.responseCode == 200)
        {
            // If the request is successful, populate the world with the new metadata
            var response = JSON.Parse(request.downloadHandler.text);
            PopulateWorld(response["block"]);

            // Update the metadata (so it can be used again offline)
            Vector3 pos = player.transform.position;
            string loc = "[" + Math.Truncate(pos[0] / 500) + ", " + Math.Truncate(pos[1] / 500) + ", " + Math.Truncate(pos[2] / 500) + "]";
            File.WriteAllText(Application.persistentDataPath + "/" + loc + ".json", response["block"].ToString());

            // Update the query metadata
            metadata[loc] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            File.WriteAllText(metadataPath, metadata.ToString());
            loadingText.text = "Downloading successful!";
            loadingText.color = Color.green;
        } else
        {
            loadingText.text = "Downloading region data failed with error " + request.responseCode;
            loadingText.color = Color.red;
        }
        StartCoroutine(ClearText());
    }

    // Clear user info text
    IEnumerator ClearText()
    {
        yield return new WaitForSeconds(4.0f);
        loadingInfo.enabled = false;
    }

}
