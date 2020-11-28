using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    private GameObject[] buttons;
    private int activeIndex = 0;
    private long changedTime = System.DateTime.Now.Ticks;

    public Canvas canvas;

    // Start is called before the first frame update
    void Start()
    {
        buttons = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            buttons[i] = transform.GetChild(i).gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (canvas.enabled)
        {
            Vector2 inp = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            if (Mathf.Abs(inp[1]) > Mathf.Abs(inp[0]) && System.DateTime.Now.Ticks - changedTime > 20000000)
            {
                if (inp[1] > 0)
                    activeIndex = (activeIndex + 1) % buttons.Length;
                else
                    activeIndex = (((activeIndex - 1) % buttons.Length) + buttons.Length) % buttons.Length;

                changedTime = System.DateTime.Now.Ticks;
                buttons[activeIndex].GetComponent<Button>().Select();
            }

            if (OVRInput.GetUp(OVRInput.RawButton.X))
                buttons[activeIndex].GetComponent<Button>().onClick.Invoke();
        }
    }
}
