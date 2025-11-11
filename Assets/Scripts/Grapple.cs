using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Grapple : MonoBehaviour
{
    [HideInInspector] public Vector3 targetPosition;
    [SerializeField] private float speed = 40;
    [SerializeField] private float maxLength = 30;

    public AK.Wwise.Event GrappleWub;

    private void Update()
    {
        Vector3 desiredDirection = (targetPosition - transform.position).normalized;
        
        // our local +y is forward
        transform.up = desiredDirection;
        
        float scaleAmount = speed * Time.deltaTime;
        transform.localScale += Vector3.up * scaleAmount;

        GrappleWub.Post(gameObject);

        if (transform.localScale.y > maxLength)
        {
            Destroy(gameObject);
            GrappleWub.Stop(gameObject);
           
        }


    }

}