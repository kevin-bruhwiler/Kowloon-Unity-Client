using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    private Button[] buttons;
    private int activeIndex = 0;
    private volatile bool canRepeat = true;

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
            if (Mathf.Abs(inp[1]) > Mathf.Abs(inp[0]) && canRepeat)
            {
                canRepeat = !canRepeat;
                StartCoroutine(WaitAndRepeat());
                if (inp[1] > 0)
                    activeIndex = Mathf.Min(activeIndex + 1, buttons.Length - 1);
                else
                    activeIndex = Mathf.Max(activeIndex - 1, 0);

                buttons[activeIndex].Select();
            }

            buttons[activeIndex].Select();
            if (OVRInput.GetUp(OVRInput.RawButton.X))
                buttons[activeIndex].GetComponent<Button>().onClick.Invoke();
        }
    }

    IEnumerator WaitAndRepeat()
    {
        yield return new WaitForSeconds(0.2f);
        this.canRepeat = true;
    }
}
