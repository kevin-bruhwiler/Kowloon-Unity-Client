using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    private Button[] buttons;
    private int activeIndex = 0;
    private long changedTime = 0;

    public Canvas canvas;

    // Start is called before the first frame update
    void Start()
    {
        buttons = new Button[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            buttons[i] = transform.GetChild(i).gameObject.GetComponent<Button>();
    }

    // Update is called once per frame
    void Update()
    {
        if (canvas.enabled)
        {
            Vector2 inp = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            if (Mathf.Abs(inp[1]) > Mathf.Abs(inp[0]) && System.DateTime.Now.Ticks - changedTime > 10000000)
            {
                changedTime = System.DateTime.Now.Ticks;
                if (inp[1] > 0)
                    activeIndex = (activeIndex + 1) % buttons.Length;
                else
                    activeIndex = (((activeIndex - 1) % buttons.Length) + buttons.Length) % buttons.Length;

                buttons[activeIndex].Select();
            }

            if (OVRInput.GetUp(OVRInput.RawButton.X))
                buttons[activeIndex].GetComponent<Button>().onClick.Invoke();
        }
    }
}
