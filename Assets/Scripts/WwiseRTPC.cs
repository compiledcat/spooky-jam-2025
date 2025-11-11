using UnityEngine;
using UnityEngine.InputSystem;

public class WwiseRTPC : MonoBehaviour
{
    public AK.Wwise.RTPC RTPCSpeed;
    public float RTPCpeed = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float RTPCpeed = 0;
        RTPCSpeed.SetGlobalValue(RTPCpeed);
    }

    // Update is called once per frame
    void Update()
    {
        IsWDown();
    }

    public void IsWDown()
    {
        var wKeyPressed = Keyboard.current.wKey.isPressed;
        if (wKeyPressed == true)
        {
            RTPCpeed += 1f;
        }
        else
        {
            RTPCpeed -= 3f;
        }

        if (RTPCpeed < 0)
        {
            RTPCpeed = 0;
        }
        if (RTPCpeed > 30)
        {
            RTPCpeed = 30;
        }
        Debug.Log(RTPCSpeed);
        RTPCSpeed.SetGlobalValue(RTPCpeed);

    }
}
