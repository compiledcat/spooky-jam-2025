using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceshipController : MonoBehaviour
{
    [SerializeField] private float linearThrust = 15.0f;
    [SerializeField] private float angularThrust = 10.0f;
    [SerializeField] private float maxLinearVelocity = 10.0f;

    [SerializeField] private float lateralDamping = 1.0f;
    [SerializeField] private float camLinearLerpSpeed = 10.0f;
    [SerializeField] private float camAngularLerpSpeed = 2.0f;
    [SerializeField] private float maxCamLookaheadAmount = 0.8f; //Multiplied by the camera's half-height
    [SerializeField] private float maxFovIncreaseAmount = 0.1f; //Multiplied by the camera's half-height

    private Rigidbody rb;
    private InputAction moveAction;

    private Camera cam;
    private float camDefaultOrthoSize;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        moveAction = InputSystem.actions.FindAction("Move");
        
        cam = Camera.main;
        camDefaultOrthoSize = cam.orthographicSize;
    }

    private void LateUpdate()
    {
        //When the player thrusts forward, the camera should be moved up to be higher than the spaceship - so the player can see more of what's ahead of them
        //The amount the camera moves ahead of the spaceship is a function of the spaceship's velocity - higher velocity, further ahead
        //The maximum distance the camera can move ahead of the spaceship is the maxCamLookaheadAmount * half the camera's orthographic height
        float t = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(0.0f, maxLinearVelocity, rb.linearVelocity.magnitude));
        float camLookaheadAmount = Mathf.Lerp(0.0f, maxCamLookaheadAmount, t);
        Vector2 newCamPos = Vector2.Lerp(cam.transform.position, transform.position + rb.linearVelocity.normalized * camLookaheadAmount * cam.orthographicSize, camLinearLerpSpeed * Time.deltaTime);
        cam.transform.position = new Vector3(newCamPos.x, newCamPos.y, cam.transform.position.z);

        //Same as above, but increase the fov (orthographic size) when the spaceship is moving fast
        float camFOVIncrease = Mathf.Lerp(0.0f, maxFovIncreaseAmount, t);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, camDefaultOrthoSize + camFOVIncrease * camDefaultOrthoSize, camLinearLerpSpeed * Time.deltaTime);

        //Also apply some slerpin' to the rotation
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, transform.rotation, camAngularLerpSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        Vector2 moveState = moveAction.ReadValue<Vector2>();

        rb.AddForce(linearThrust * transform.up * Mathf.Max(0, moveState.y));
        if (rb.linearVelocity.sqrMagnitude >= maxLinearVelocity * maxLinearVelocity)
        {
            //Player is moving faster than is permitted, clamp their velocity
            Vector3 dir = rb.linearVelocity.normalized;
            rb.linearVelocity = dir * maxLinearVelocity;
        }

        rb.AddTorque(angularThrust * transform.forward * -moveState.x);

        if (moveState.y > 0)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
            rb.AddForce(transform.right * -localVelocity.x * lateralDamping);
        }
    }
}
