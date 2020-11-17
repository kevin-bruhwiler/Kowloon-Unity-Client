using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollController : MonoBehaviour
{
    public ScrollRect scrollRect;

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
        if(scrollRect != null && scrollRect.IsActive() && Mathf.Abs(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1]) != Mathf.Abs(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[0]))
        {
            scrollRect.verticalScrollbar.value += OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1] * 2f * Time.smoothDeltaTime;
        }

        // Change "tabs" with right dpad
        if (scrollRect != null && scrollRect.IsActive() && Mathf.Abs(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[0]) > Mathf.Abs(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1]))
        {
            if (System.DateTime.Now.Ticks - tabChangeTime > 2000000)
            {
                tabChangeTime = System.DateTime.Now.Ticks;
                tab.Clear();
                tab.Populate();
            }
            
        }
    }
}
