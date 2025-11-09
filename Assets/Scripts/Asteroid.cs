using UnityEngine;
using Random = UnityEngine.Random;

public class Asteroid : MonoBehaviour
{
    [SerializeField] private Rigidbody _rb;
    public Outline Outline;

    private void OnValidate()
    {
        _rb ??= GetComponent<Rigidbody>();
        Outline ??= GetComponent<Outline>();
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        // random angular velocity
        _rb.angularVelocity = Random.onUnitSphere * Random.Range(0.1f, 1.0f);
    }
}