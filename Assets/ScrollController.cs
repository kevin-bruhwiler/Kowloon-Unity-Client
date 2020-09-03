using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollController : MonoBehaviour
{
    public ScrollRect scrollRect;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(scrollRect != null && scrollRect.IsActive() && OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1] != 0)
        {
            scrollRect.verticalScrollbar.value += OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1] * 2f * Time.smoothDeltaTime;
        }
    }
}
