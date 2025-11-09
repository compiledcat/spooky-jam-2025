using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceshipController : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction grappleAction;
    private InputAction grapplePositionAction;
    
    private bool isControllable;
    private RaceStartHandler raceStartHandler;

    private Camera cam;
    
    [SerializeField] private float linearThrust = 24.0f;
    [SerializeField] private float angularThrust = 10.0f;

    [SerializeField] private float lateralDamping = 1.0f;
    public float maxLinearVelocity = 35.0f;

    public Rigidbody rb;
    
    [SerializeField] private GameObject grapplePrefab;
    private GameObject grapple;
    
    private void OnValidate()
    {
        rb ??= GetComponent<Rigidbody>();
    }

    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        grappleAction = InputSystem.actions.FindAction("Grapple");
        grapplePositionAction = InputSystem.actions.FindAction("GrapplePosition");

        cam = Camera.main!;
        
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

    private void Update()
    {
        if (grapple)
        {
            grapple.transform.position = transform.position;
        }

        //Grapple
        if (grappleAction.WasPressedThisFrame())
        {
            Destroy(grapple);
            grapple = Instantiate(grapplePrefab);
            grapple.transform.position = transform.position;
            
            Vector2 screenPos = grapplePositionAction.ReadValue<Vector2>();
            Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
            grapple.GetComponentInChildren<Grapple>().targetPosition = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }
    }

    private void FixedUpdate()
    {
        // Movement
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
