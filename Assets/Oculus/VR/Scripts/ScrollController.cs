using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Scrolls the item menu - why is this not part of the PopulateContent.cs script? No idea.
public class ScrollController : MonoBehaviour
{
    public Canvas canvas;

    public PopulateContent tab;

    private long tabChangeTime = 0;


    void Start()
    {

    }

    void Update()
    {
        // Scroll with up/down on the left dpad
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
