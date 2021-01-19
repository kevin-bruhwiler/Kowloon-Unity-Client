using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControlScroller : MonoBehaviour
{
    public Canvas controls;

    private Scrollbar sb;

    void Start()
    {
        sb = transform.GetComponent<Scrollbar>();
    }

    void Update()
    {
        if (controls.enabled)
        {
            Vector2 inp = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
            sb.value = Mathf.Clamp(sb.value + 0.01f * inp[1], 0, 1);
        }
    }
}
