using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;


public class updater : MonoBehaviour
{

    public VRTeleporter teleporter;
    public OVRPlayerController player;
    private CharacterController cc;
    private string placedObjectDir = "Assets/Resources/RecentlyPlacedObjects/";

    // Start is called before the first frame update
    void Start()
    {
        cc = player.GetComponent<CharacterController>();
        cc.enabled = true;
    }

    // Update is called once per frame
    void Update()
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

        if (OVRInput.GetUp(OVRInput.Button.Start)) //Temporary - save all placed objects selection
        {
            GameObject[] placedObjects = GameObject.FindGameObjectsWithTag("RecentlyPlaced");
            foreach (GameObject go in placedObjects)
            {
                string localPath = placedObjectDir + go.GetInstanceID() + ".prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, localPath, InteractionMode.UserAction);
            }

            DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/Resources/RecentlyPlacedObjects/");
            FileInfo[] info = dir.GetFiles("*.*");
            foreach (FileInfo f in info)
                File.Delete(f.FullName);
        }
    }
}
