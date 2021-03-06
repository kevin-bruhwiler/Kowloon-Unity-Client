﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollController : MonoBehaviour
{
    public ScrollRect scrollRect;

    public PopulateContent tab;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Scroll with left dpad
        if(scrollRect != null && scrollRect.IsActive() && OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1] != 0)
        {
            scrollRect.verticalScrollbar.value += OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1] * 2f * Time.smoothDeltaTime;
        }

        // Change "tabs" with right dpad
        if (scrollRect != null && scrollRect.IsActive() && OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[0] != 0)
        {
            Clear();
            tab.Populate();
        }
    }

    void Clear()
    {
        foreach (Transform child in tab.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
