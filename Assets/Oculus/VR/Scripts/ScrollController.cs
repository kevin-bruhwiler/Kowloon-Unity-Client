using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollController : MonoBehaviour
{
    public Canvas canvas;

    public PopulateContent tab;

    private long tabChangeTime = 0;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Scroll with left dpad
        if(canvas != null && canvas.enabled && Mathf.Abs(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1]) > Mathf.Abs(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[0]))
        {
            if (System.DateTime.Now.Ticks - tabChangeTime > 2000000)
            {
                tabChangeTime = System.DateTime.Now.Ticks;
                tab.Clear();
                tab.IncrementRow(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1] > 0);
                tab.Populate();
            }
        }

        // Change "tabs" with left dpad
        if (canvas != null && canvas.enabled && Mathf.Abs(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[0]) > Mathf.Abs(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1]))
        {
            if (System.DateTime.Now.Ticks - tabChangeTime > 2000000)
            {
                tabChangeTime = System.DateTime.Now.Ticks;
                tab.Clear();
                tab.IncrementIndex(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[0] > 0);
                tab.Populate();
            }
            
        }
    }
}
