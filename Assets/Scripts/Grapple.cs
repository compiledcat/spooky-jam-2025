using UnityEngine;
using UnityEngine.Serialization;

public class Grapple : MonoBehaviour
{

    [HideInInspector] public Vector3 targetPosition;
    [SerializeField] private float speed;
    [SerializeField] private GameObject parent; //The parent grapple
    [SerializeField] private float maxLength;
    
    void Start()
    {
        Vector3 desiredDirection = (targetPosition - transform.position).normalized;
        float angle = Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0.0f, 0.0f, angle - 90.0f);
    }

    void Update()
    {
        float scaleAmount = speed * Time.deltaTime;
        transform.localScale += Vector3.up * scaleAmount;
        transform.localPosition = transform.up * (transform.localScale.y / 2);
        if (transform.localScale.y > maxLength)
        {
            Destroy(parent);
        }
    }
}