using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceshipController : MonoBehaviour
{
    public Rigidbody rb;
    [SerializeField] private Grapple grapplePrefab;
    private Grapple grapple;

    [Space] [SerializeField] private Transform _yolk;
    [SerializeField] private Transform _greebleHead;
    [SerializeField] private float _maxTurnAnimateAngle = 30.0f;

    private Asteroid _hoveredAsteroid;

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

    public Vector3 enginePosition;

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
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        var ray = cam.ScreenPointToRay(mouseScreenPos);
        if (isControllable && Physics.Raycast(ray, out var hit, 100f, LayerMask.GetMask("Asteroid")))
        {
            var asteroid = hit.transform.GetComponentInParent<Asteroid>();
            if (_hoveredAsteroid != asteroid)
            {
                if (_hoveredAsteroid)
                {
                    _hoveredAsteroid.Outline.enabled = false;
                    _hoveredAsteroid = null;
                }

                if (asteroid && asteroid.AllowGrapple)
                {
                    _hoveredAsteroid = asteroid;
                    _hoveredAsteroid.Outline.enabled = true;
                }
            }

            if (grappleAction.WasPressedThisFrame() && asteroid.AllowGrapple)
            {
                grapple = Instantiate(grapplePrefab, transform);
                grapple.Target = hit.transform.GetComponentInParent<Asteroid>();
            }
        }
        else if (_hoveredAsteroid)
        {
            _hoveredAsteroid.Outline.enabled = false;
            _hoveredAsteroid = null;
        }

        if (grappleAction.WasReleasedThisFrame() && grapple)
        {
            Destroy(grapple.gameObject);
        }

        // Rotate yolk and greeble head to follow turn x
        var moveState = moveAction.ReadValue<Vector2>();
        var targetYolkRotationZ = -moveState.x * _maxTurnAnimateAngle;
        var yolkEuler = _yolk.localEulerAngles;
        yolkEuler.z = Mathf.LerpAngle(yolkEuler.z, targetYolkRotationZ, 10.0f * Time.deltaTime);
        _yolk.localEulerAngles = yolkEuler;

        var targetGreebleRotationY = moveState.x * _maxTurnAnimateAngle;
        var greebleEuler = _greebleHead.localEulerAngles;
        greebleEuler.y = Mathf.LerpAngle(greebleEuler.y, targetGreebleRotationY, 10.0f * Time.deltaTime);
        _greebleHead.localEulerAngles = greebleEuler;
    }

    private void FixedUpdate()
    {
        // Movement
        Vector2 moveState = isControllable ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        Vector3 engineWorldPos = transform.TransformPoint(enginePosition);
        rb.AddForceAtPosition(transform.forward * (linearThrust * Mathf.Max(0, moveState.y)), engineWorldPos);

        if (rb.linearVelocity.sqrMagnitude >= maxLinearVelocity * maxLinearVelocity)
        {
            //Player is moving faster than is permitted, clamp their velocity
            Vector3 dir = rb.linearVelocity.normalized;
            rb.linearVelocity = dir * maxLinearVelocity;
        }

        rb.AddTorque(transform.up * (angularThrust * moveState.x));

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