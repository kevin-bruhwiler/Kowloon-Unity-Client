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
        foreach (GameObject go in placedObjects)
        {
            string localPath = placedObjectDir + go.GetInstanceID() + ".prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, localPath, InteractionMode.UserAction);
        }

        DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/Resources/RecentlyPlacedObjects/");
        FileInfo[] info = dir.GetFiles("*.*");

        // Read prefab and all dependencies into byte strings
        var files = JSON.Parse("{}");
        foreach (FileInfo f in info)
        {
            string[] dependencies = AssetDatabase.GetDependencies(placedObjectDir + f.Name, true);
            foreach (string dependency in dependencies)
                if (files[dependency] == null)
                    files[dependency] = Convert.ToBase64String(File.ReadAllBytes(dependency));
        }

        StartCoroutine(Post("http://localhost:5000/transactions/new/unsigned", files.ToString()));

        foreach (FileInfo f in info)
            File.Delete(f.FullName);
    }

    IEnumerator Post(string url, string bodyJsonString)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log("Status Code: " + request.responseCode);
    }

}
