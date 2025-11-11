using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceshipController : MonoBehaviour
{
    public Rigidbody rb;
    [SerializeField] private Grapple grapplePrefab;
    private Grapple grapple;
    public AK.Wwise.Event GrappleLaunch;


    private InputAction moveAction;
    private InputAction grappleAction;



    private Camera cam;

    private bool isControllable;

    // start line is 1, we start having technically "passed" the start line (not physically true)
    public int reachedCheckpoint { get; private set; } = 1;

    [Space] [SerializeField] private float linearThrust = 30.0f;
    [SerializeField] private float angularThrust = 10.0f;

    [SerializeField] private float lateralDamping = 1.0f;
    public float maxLinearVelocity = 35.0f;

    [SerializeField] private Vector3 enginePosition;

    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        grappleAction = InputSystem.actions.FindAction("Grapple");

        cam = Camera.main!;
        RaceStartHandler.OnCountdownEnd.AddListener(() => isControllable = true);


    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.TransformPoint(enginePosition), 0.25f);
    }

    private void Update()
    {
        //Grapple
        if (grappleAction.WasPressedThisFrame() && isControllable)
        {
            if (grapple)
            {
                Destroy(grapple.gameObject);
            }

            grapple = Instantiate(grapplePrefab, transform);
            GrappleLaunch.Post(gameObject);
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
            grapple.targetPosition = new Vector3(mouseWorldPos.x, mouseWorldPos.y, transform.position.z);
        }

      

    }

    private void FixedUpdate()
    {
        // Movement
        Vector2 moveState = isControllable ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        Vector3 engineWorldPos = transform.TransformPoint(enginePosition);
        rb.AddForceAtPosition(transform.up * (linearThrust * Mathf.Max(0, moveState.y)), engineWorldPos);

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

    public void PassCheckpoint(int checkpointNum)
    {
        if (checkpointNum <= reachedCheckpoint) return;
        reachedCheckpoint = checkpointNum;
    }

 
}