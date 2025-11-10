using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PostWwiseEvent : MonoBehaviour
{
    public AK.Wwise.Event ThreeTwoOneGo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

    private void Update()
    {
        OnAnyKeyDown();
    }



    public void OnAnyKeyDown()
    {
        
        {
            if(Input.GetButtonDown("G"))
            {
                ThreeTwoOneGo.Post(gameObject);
                Debug.Log("input received");
            }
            

        }
    }
}
