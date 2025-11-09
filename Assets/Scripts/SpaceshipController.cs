using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceshipController : MonoBehaviour
{
    public Rigidbody rb;
    [SerializeField] private Grapple grapplePrefab;
    private Grapple grapple;

    private InputAction moveAction;
    private InputAction grappleAction;

    private Camera cam;
    private RaceStartHandler raceStartHandler;

    private bool isControllable;

    [Space] [SerializeField] private float linearThrust = 24.0f;
    [SerializeField] private float angularThrust = 10.0f;

    [SerializeField] private float lateralDamping = 1.0f;
    public float maxLinearVelocity = 35.0f;

    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        grappleAction = InputSystem.actions.FindAction("Grapple");

        cam = Camera.main!;

        raceStartHandler = FindAnyObjectByType<RaceStartHandler>();
        if (!raceStartHandler)
        {
            Debug.LogWarning("No RaceStartHandler found in scene(???), race won't start and ship won't be controllable!");
        }
        else
        {
            raceStartHandler.OnCountdownEnd.AddListener(() => isControllable = true);
        }
    }

    private void Update()
    {
        //Grapple
        if (grappleAction.WasPressedThisFrame())
        {
            if (grapple)
            {
                Destroy(grapple.gameObject);
            }

            grapple = Instantiate(grapplePrefab, transform);

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
            grapple.targetPosition = new Vector3(mouseWorldPos.x, mouseWorldPos.y, transform.position.z);
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