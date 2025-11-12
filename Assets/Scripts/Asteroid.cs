using UnityEngine;

public class Asteroid : MonoBehaviour
{
    private Rigidbody _rb;

    private void OnValidate()
    {
        _rb ??= GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        // random angular velocity
        _rb.angularVelocity = Random.onUnitSphere * Random.Range(0.1f, 1.0f);
    }
}