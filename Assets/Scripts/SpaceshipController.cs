using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceshipController : MonoBehaviour
{
    [SerializeField] private float linearThrust = 15.0f;
    [SerializeField] private float angularThrust = 10.0f;
    [SerializeField] private float lateralDamping = 1.0f;

    private Rigidbody rb;
    private InputAction moveAction;

    private Transform _camera;

    void Start()
    {
        _camera = Camera.main.transform;
        rb = GetComponent<Rigidbody>();
        moveAction = InputSystem.actions.FindAction("Move");
    }

    private void LateUpdate()
    {
        _camera.transform.position = Vector3.Lerp(_camera.position, transform.position, 10f * Time.deltaTime);
    }

    void FixedUpdate()
    {
        Vector2 moveState = moveAction.ReadValue<Vector2>();

        rb.AddForce(linearThrust * transform.up * Mathf.Max(0, moveState.y));
        rb.AddTorque(angularThrust * transform.forward * -moveState.x);

        if(moveState.y > 0)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
            rb.AddForce(transform.right * -localVelocity.x * lateralDamping);
        }
    }
}
