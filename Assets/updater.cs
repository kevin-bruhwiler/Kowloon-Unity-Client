using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class updater : MonoBehaviour
{

    public VRTeleporter teleporter;
    public OVRPlayerController player;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        player.GetComponent<CharacterController>().enabled = true;
        if (OVRInput.Get(OVRInput.Button.Three))
        {
            teleporter.ToggleDisplay(true);
        }
        if (OVRInput.GetUp(OVRInput.Button.Three))
        {
            player.GetComponent<CharacterController>().enabled = false;
            teleporter.Teleport();
            teleporter.ToggleDisplay(false);
        }
    }
}
