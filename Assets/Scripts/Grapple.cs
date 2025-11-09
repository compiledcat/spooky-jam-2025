using UnityEngine;

public class Grapple : MonoBehaviour
{
    [HideInInspector] public Vector3 targetPosition;
    [SerializeField] private float speed = 40;
    [SerializeField] private float maxLength = 30;

    private void Start()
    {
        Vector3 desiredDirection = (targetPosition - transform.position).normalized;
        
        // our local +y is forward
        transform.up = desiredDirection;
    }

    private void Update()
    {
        float scaleAmount = speed * Time.deltaTime;
        transform.localScale += Vector3.up * scaleAmount;
        
        if (transform.localScale.y > maxLength)
        {
            Destroy(gameObject);
        }
    }
}