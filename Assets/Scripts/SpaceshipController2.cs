// ReSharper disable All im sorry bro this is throwing so many errors for me
#pragma warning disable CS0414

using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class SpaceshipController2 : MonoBehaviour
{
    [Header("Core Tuning")]
    public float maxLinearVelocity = 18f;
    public float linearThrust = 55f;
    public float angularThrust = 25f;
    [SerializeField] private Vector3 enginePosition = new Vector3(0f, -0.2f, 0f);
    public bool allowReverse = false;

    [Header("Feel: Steering")]
    [SerializeField] float steerAt0 = 1.0f;
    [SerializeField] float steerAtVmax = 0.35f;
    [SerializeField] float steerResponse = 3f;
    [SerializeField] float yawDampingHighSpeed = 1.5f;

    [Header("Feel: Grip & Drift")]
    [SerializeField] float latStiff = 18f;
    [SerializeField] float latStiffMax = 28f;
    [SerializeField] float driftThresholdDeg = 7f;
    [SerializeField] float driftGripScale = 0.82f;
    [SerializeField] float counterSteerAssist = 0.14f;
    [SerializeField] float coastRearGripScale = 0.90f;

    [Header("Feel: Traction Circle")]
    [SerializeField] float mu = 1.35f;
    [SerializeField] float tractionSoftness = 0.25f;

    [Header("Feel: Lateral Damping")]
    [SerializeField] float baseLateralDamp = 9f;
    [SerializeField] float lateralDampSpeedScale = 0.6f;

    [Header("Feel: Walls")]
    [SerializeField] float wallRestitution = 0.2f;
    [SerializeField] float wallTangentKeep = 0.85f;
    [SerializeField] float wallAlignNudgeDeg = 12f;
    [SerializeField] float wallSpeedFloor = 6f;

    [Header("Grapple (visual only here)")]
    [SerializeField] private Grapple grapplePrefab;
    private Grapple _grapple;

    [Header("Control Gate")]
    public bool isControllable = true;

    // ---- Tether awareness (reference existing GrappleTetherOrbit) ----
    [Header("Tether Awareness")]
    [SerializeField] private GrappleTetherOrbit tether;   // assign or auto-grab
    [SerializeField] private float tautEpsilon = 0.01f;   // extra slack margin

    // ---- Orbit follow (the new bit) ----
    [Header("Orbit Follow (when tether is taut)")]
    public bool followOrbit = true;
    [Tooltip("How close to ropeLen counts as taut for following.")]
    public float orbitNearTautMargin = 0.12f;
    [Tooltip("Proportional torque gain toward orbit tangent (per rad).")]
    public float orbitAlignKp = 6f;
    [Tooltip("Angular damping on orbit alignment.")]
    public float orbitAlignKd = 0.6f;
    [Tooltip("Fraction of radial velocity removed per step when taut.")]
    [Range(0f, 1f)] public float orbitRadialKill = 1.0f;
    [Tooltip("Ensure at least this tangential speed when we first capture.")]
    public float orbitMinTangentialSpeed = 3.5f;
    [Tooltip("Scale your forward thrust while taut to avoid re-injecting radial.")]
    [Range(0f, 2f)] public float orbitThrustScale = 1.0f;

    [Tooltip("Overall scale for how strongly this body feels black holes.")]
    public float gravityScale = 0.25f;

    // State
    public Rigidbody rb;
    public Camera cam;
    private float _sqrVmax;
    private float _lastThrottle;
    float _steerInput, _throttleInput;

    // remember CW/CCW choice when tangential velocity is tiny
    int _orbitSign = 0; // +1 CCW, -1 CW

    public int reachedCheckpoint { get; private set; } = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY;

        _sqrVmax = maxLinearVelocity * maxLinearVelocity;
        if (!tether) tether = GetComponent<GrappleTetherOrbit>();
    }

    void OnValidate()
    {
        if (maxLinearVelocity < 0f) maxLinearVelocity = 0f;
        _sqrVmax = maxLinearVelocity * maxLinearVelocity;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.TransformPoint(enginePosition), 0.25f);
    }

    void Update()
    {
        if (!isControllable) return;

        float steer = 0f, throttle = 0f;

        var gp = Gamepad.current;
        if (gp != null) { steer = gp.leftStick.x.ReadValue(); throttle = gp.leftStick.y.ReadValue(); }

        var kb = Keyboard.current;
        if (kb != null)
        {
            float kx = 0f, ky = 0f;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) kx -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) kx += 1f;
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) ky += 1f;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) ky -= 1f;
            if (Mathf.Abs(kx) > Mathf.Abs(steer)) steer = kx;
            if (Mathf.Abs(ky) > Mathf.Abs(throttle)) throttle = ky;
        }

        _steerInput = Mathf.Clamp(steer, -1f, 1f);
        _throttleInput = Mathf.Clamp(throttle, -1f, 1f);

        // (optional) visual grapple spawn...
        bool pressed = (kb != null && kb.spaceKey.wasPressedThisFrame) ||
                       (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (pressed && grapplePrefab != null)
        {
            if (_grapple == null) _grapple = Instantiate(grapplePrefab, transform);

            Vector3 target = transform.position;
            if (cam != null)
            {
                var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
                Ray ray = cam.ScreenPointToRay(pos);
                Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));
                if (plane.Raycast(ray, out float enter)) target = ray.GetPoint(enter);
            }
            _grapple.targetPosition = new Vector3(target.x, target.y, transform.position.z);
        }
    }

    void FixedUpdate()
    {
        float rawSteer = _steerInput;
        float throttleRaw = _throttleInput;
        float throttle = allowReverse ? throttleRaw : Mathf.Max(0f, throttleRaw);

        Vector3 v = rb.linearVelocity;

        float speed = v.magnitude;
        Vector3 vLocal = transform.InverseTransformDirection(v);

        // ---- Tether state (strict taut) ----
        bool tetherOn = tether && tether.tetherActive;
        bool isTaut = false;
        Vector3 dir = Vector3.zero, tangBase = Vector3.zero, anchor = Vector3.zero;
        float dist = 0f;

        if (tetherOn)
        {
            anchor = tether.tetherTransform ? tether.tetherTransform.position : tether.tetherPoint;
            Vector3 r = anchor - rb.position; r.z = 0f;
            dist = r.magnitude;
            if (dist > 1e-6f)
            {
                dir = r / dist;
                tangBase = new Vector3(-dir.y, dir.x, 0f);     // tangent (CCW)
                                                           // STRICT taut: only when stretched beyond ropeLen + snapTolerance
                isTaut = dist > (tether.ropeLen + tether.snapTolerance);
            }
        }


        // (1) Steering
        float steerCurve = Mathf.Sign(rawSteer) * Mathf.Pow(Mathf.Abs(rawSteer), steerResponse);
        float steerPower = Mathf.Lerp(steerAtVmax, steerAt0, 1f - Mathf.Clamp01(speed / maxLinearVelocity));
        float steerTorqueAccel = steerPower * -steerCurve;
        rb.AddTorque(transform.forward * (angularThrust * steerTorqueAccel), ForceMode.Acceleration);

        // High-speed yaw damping
        float yawSpeed = rb.angularVelocity.z;
        float yawDamp = yawDampingHighSpeed * Mathf.Clamp01(speed / maxLinearVelocity);
        rb.AddTorque(-transform.forward * yawSpeed * yawDamp, ForceMode.Acceleration);

        // (2) Grip / Drift
        float slipDeg = Mathf.Rad2Deg * Mathf.Atan2(Mathf.Abs(vLocal.x), Mathf.Max(0.01f, Mathf.Abs(vLocal.y)));
        float smallSlipBoost = Mathf.Lerp(latStiffMax, latStiff, Mathf.InverseLerp(0f, driftThresholdDeg, slipDeg));
        float gripAfterPeak = (slipDeg > driftThresholdDeg) ? driftGripScale : 1f;
        float coastScale = (throttle < 0.01f && _lastThrottle > 0.01f) ? coastRearGripScale : 1f;

        float lateralGrip = smallSlipBoost * gripAfterPeak * coastScale;
        Vector3 latForce = transform.right * (-vLocal.x * lateralGrip);

        // skip lateral damping while taut (let the orbit keep its tangential speed)
        if (!isTaut) rb.AddForce(latForce, ForceMode.Acceleration);

        // counter-steer assist
        float assist = Mathf.Clamp((slipDeg / 45f) * counterSteerAssist, 0f, counterSteerAssist);
        rb.AddTorque(transform.forward * (angularThrust * assist * Mathf.Sign(vLocal.x)), ForceMode.Acceleration);

        // ---- ORBIT FOLLOW (only when tether is taut)
        Vector3 desiredTang = tangBase;
        if (isTaut && followOrbit && dist > 1e-6f)
        {
            // decide CW/CCW sign
            float vRad = Vector3.Dot(v, dir);
            Vector3 vTan = v - dir * vRad;
            float tanSignFromVel = Mathf.Sign(Vector3.Dot(vTan, tangBase));      // +1 or -1 or 0
            float tanSignFromInput = Mathf.Sign(rawSteer);                        // fallback from player intent

            int sign;
            if (Mathf.Abs(tanSignFromVel) > 0.01f) sign = (int)Mathf.Sign(tanSignFromVel);
            else if (Mathf.Abs(tanSignFromInput) > 0.01f) sign = (int)Mathf.Sign(tanSignFromInput);
            else sign = (_orbitSign == 0 ? 1 : _orbitSign); // persist last

            _orbitSign = sign;
            desiredTang = tangBase * sign;

            // (A) kill radial component (stick to circle)
            if (orbitRadialKill > 0f)
            {
                rb.linearVelocity -= dir * (vRad * orbitRadialKill);
            }

            // (B) align ship's nose to tangent (PD)
            Vector3 fwd = transform.up;
            float sin = Vector3.Cross(fwd, desiredTang).z;
            float cos = Vector3.Dot(fwd, desiredTang);
            float angErr = Mathf.Atan2(sin, cos); // radians
            float torqueCmd = orbitAlignKp * angErr - orbitAlignKd * rb.angularVelocity.z;
            rb.AddTorque(transform.forward * (angularThrust * torqueCmd), ForceMode.Acceleration);

            // (C) ensure a minimum tangential speed right after capture
            v = rb.linearVelocity; // re-read
            vRad = Vector3.Dot(v, dir);
            vTan = v - dir * vRad;
            float vTanMag = vTan.magnitude;
            if (orbitMinTangentialSpeed > 0f && vTanMag < orbitMinTangentialSpeed)
            {
                float dv = orbitMinTangentialSpeed - vTanMag;
                float a = Mathf.Min(dv / Time.fixedDeltaTime, linearThrust); // cap by available thrust-ish
                rb.AddForce(desiredTang.normalized * a, ForceMode.Acceleration);
            }
        }


        // (3) Throttle with traction circle; project along tangent if taut
        float Fx = (allowReverse ? throttleRaw : Mathf.Max(0f, throttleRaw)) * linearThrust;
        float Fy = Mathf.Abs(vLocal.x) * lateralGrip;
        float demand = Mathf.Sqrt(Fx * Fx + Fy * Fy);
        float maxDemand = Mathf.Max(0.01f, mu * linearThrust);
        if (demand > maxDemand) { float k = Mathf.Lerp(maxDemand / demand, 1f, tractionSoftness); Fx *= k; }

        Vector3 engineWorldPos = transform.TransformPoint(enginePosition);
        Vector3 forwardAccel = transform.up * Fx;

        if (isTaut && followOrbit)
            forwardAccel = Vector3.Project(forwardAccel * orbitThrustScale, desiredTang);

        rb.AddForceAtPosition(forwardAccel, engineWorldPos, ForceMode.Acceleration);

        // (4) clamp top speed
        if (rb.linearVelocity.sqrMagnitude > _sqrVmax)
            rb.linearVelocity = rb.linearVelocity.normalized * maxLinearVelocity;

        _lastThrottle = throttle;
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.contactCount == 0) return;

        Vector3 n = c.GetContact(0).normal;

        Vector3 vel = rb.linearVelocity;

        float vn = Vector3.Dot(vel, n);
        if (vn >= 0f) return;

        Vector3 reflected = vel - (1f + wallRestitution) * vn * n;
        Vector3 tangent = Vector3.ProjectOnPlane(reflected, n).normalized;
        float speedMag = Mathf.Max(reflected.magnitude, wallSpeedFloor);

        rb.linearVelocity = Vector3.Lerp(reflected, tangent * speedMag, 1f - wallTangentKeep);

        float sign = Mathf.Sign(Vector3.Cross(transform.up, tangent).z);
        rb.AddTorque(transform.forward * sign * Mathf.Deg2Rad * wallAlignNudgeDeg, ForceMode.VelocityChange);
    }

    public void PassCheckpoint(int checkpointNum)
    {
        if (checkpointNum > reachedCheckpoint)
            reachedCheckpoint = checkpointNum;
    }

}
