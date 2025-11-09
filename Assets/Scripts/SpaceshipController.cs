using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceshipController : MonoBehaviour
{
    private InputAction moveAction;
    
    private bool isControllable;
    private RaceStartHandler raceStartHandler;
    
    [SerializeField] private float linearThrust = 24.0f;
    [SerializeField] private float angularThrust = 10.0f;

    [SerializeField] private float lateralDamping = 1.0f;
    public float maxLinearVelocity = 35.0f;

    public Rigidbody rb;
    
    private void OnValidate()
    {
        rb ??= GetComponent<Rigidbody>();
    }

    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        
        raceStartHandler = FindAnyObjectByType<RaceStartHandler>();
        if (!raceStartHandler)
        {
            Debug.LogWarning("No RaceStartHandler found in scene(???), ship won't be controllable!");
        }
        else
        {
            raceStartHandler.OnCountdownEnd.AddListener(() => isControllable = true);
        }
    }

    private void FixedUpdate()
    {
        Vector2 moveState = isControllable ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        rb.AddForce(transform.up * (linearThrust * Mathf.Max(0, moveState.y)));
        if (rb.linearVelocity.sqrMagnitude >= maxLinearVelocity * maxLinearVelocity)
        {
            //Player is moving faster than is permitted, clamp their velocity
            Vector3 dir = rb.linearVelocity.normalized;
            rb.linearVelocity = dir * maxLinearVelocity;
        }

        rb.AddTorque(transform.forward * (angularThrust * -moveState.x));

        if (moveState.y > 0)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
            rb.AddForce(transform.right * (-localVelocity.x * lateralDamping));
        }
    }
}
