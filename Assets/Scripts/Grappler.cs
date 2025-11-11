using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)] // run after movement so constraint wins
[RequireComponent(typeof(Rigidbody))]
public class GrappleTetherOrbit : MonoBehaviour
{
    [Header("Pick / Targets")]
    public LayerMask grappleMask;
    public float pickRadius = 0.75f;
    public bool handleInputHere = true; // set false if another script attaches/releases

    [Header("Rope (pure constraint)")]
    [Tooltip("Current rope length. Set when you attach; retraction reduces this.")]
    public float ropeLen;
    [Tooltip("Units/sec to shorten rope. 0 = no auto-retract (no spiral-in).")]
    public float ropeRetractSpeed = 0f;
    [Tooltip("Position snap tolerance for 'taut' check.")]
    public float snapTolerance = 0.01f;

    [Header("Orbit controls")]
    [Tooltip("Clamp tangential speed while tethered. 0 = no clamp.")]
    public float maxOrbitSpeed = 0f;

    [Header("Rope visual (optional)")]
    public LineRenderer ropeLine;
    public float ropeLineWidth = 0.06f;

    // runtime
    public bool tetherActive { get; private set; }
    public Transform tetherTransform { get; private set; }
    public Vector3 tetherPoint; // used if target has no transform

    Rigidbody rb;
    Camera cam;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;

        if (!ropeLine)
        {
            ropeLine = gameObject.AddComponent<LineRenderer>();
            ropeLine.positionCount = 2;
            ropeLine.useWorldSpace = true;
            ropeLine.widthMultiplier = ropeLineWidth;
            ropeLine.material = new Material(Shader.Find("Sprites/Default"));
            ropeLine.startColor = ropeLine.endColor = Color.white;
            ropeLine.enabled = false;
        }
    }

    void Update()
    {
        if (!handleInputHere || cam == null) { if (tetherActive) UpdateRopeVisual(); return; }

        var mouse = Mouse.current;
        if (mouse == null) return;

        // Attach on LMB
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector3 world = MouseToWorldXY(mouse.position.ReadValue());
            var hits = Physics.OverlapSphere(world, pickRadius, grappleMask, QueryTriggerInteraction.Collide);
            if (hits.Length > 0)
            {
                var best = hits.OrderBy(h => (h.ClosestPoint(world) - world).sqrMagnitude).First();
                tetherTransform = best.transform;
                tetherPoint = tetherTransform.position;

                ropeLen = Vector3.Distance(rb.position, tetherPoint); // set to current distance
                tetherActive = true;
                ropeLine.enabled = true;
                UpdateRopeVisual();
            }
        }
        // Release on LMB up
        if (mouse.leftButton.wasReleasedThisFrame)
            ReleaseTether();

        if (tetherActive) UpdateRopeVisual();
    }

    void FixedUpdate()
    {
        if (!tetherActive) return;

        Vector3 anchor = tetherTransform ? tetherTransform.position : tetherPoint;

        // Optional steady retraction
        if (ropeRetractSpeed > 0f)
            ropeLen = Mathf.Max(0f, ropeLen - ropeRetractSpeed * Time.fixedDeltaTime);

        // Vector to anchor in XY
        Vector3 r = anchor - rb.position; r.z = 0f;
        float dist = r.magnitude;
        if (dist < 1e-6f) return;
        Vector3 dir = r / dist;

        // Are we taut?
        bool taut = dist > (ropeLen + snapTolerance);

        if (taut)
        {
            // Decompose velocity
            float vRad = Vector3.Dot(rb.linearVelocity, dir);
            Vector3 vTan = rb.linearVelocity - dir * vRad;
            float vTanMag = vTan.magnitude;

            // (1) Remove radial velocity so motion is purely tangent
            rb.linearVelocity = vTan;

            // (2) Apply exactly the centripetal acceleration needed to keep radius constant
            //     a_c = v_t^2 / r inward (perpendicular to v_t) → energy-neutral
            if (dist > 1e-4f && vTanMag > 1e-4f)
            {
                float aC = (vTanMag * vTanMag) / dist;
                rb.AddForce(dir * aC, ForceMode.Acceleration);
            }

            // (3) Snap position to the circle if overstretched
            rb.position = anchor - dir * ropeLen;
        }

        // Optional tangential clamp while tethered
        if (maxOrbitSpeed > 0f)
        {
            float vRad = Vector3.Dot(rb.linearVelocity, dir);
            Vector3 vTan = rb.linearVelocity - dir * vRad;
            if (vTan.magnitude > maxOrbitSpeed)
                rb.linearVelocity = vTan.normalized * maxOrbitSpeed + dir * vRad; // keep any radial (usually ~0 when taut)
        }

        UpdateRopeVisual();
    }

    public void ReleaseTether()
    {
        tetherActive = false;
        tetherTransform = null;
        ropeLine.enabled = false;
    }

    Vector3 MouseToWorldXY(Vector2 mousePos)
    {
        Ray ray = cam.ScreenPointToRay(mousePos);
        Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, transform.position.z));
        return plane.Raycast(ray, out float t)
            ? ray.GetPoint(t)
            : cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y,
                  Mathf.Abs(transform.position.z - cam.transform.position.z)));
    }

    void UpdateRopeVisual()
    {
        if (!ropeLine || !tetherActive) return;
        Vector3 anchor = tetherTransform ? tetherTransform.position : tetherPoint;
        ropeLine.SetPosition(0, transform.position);
        ropeLine.SetPosition(1, anchor);
    }

    void OnDrawGizmosSelected()
    {
        if (!tetherActive) return;
        Vector3 anchor = tetherTransform ? tetherTransform.position : tetherPoint;
        const int seg = 48;
        Vector3 prev = anchor + new Vector3(ropeLen, 0, 0);
        for (int i = 1; i <= seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            Vector3 p = anchor + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * ropeLen;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}
