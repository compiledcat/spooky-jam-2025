using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public class BlackHoleSidePullPulsed : MonoBehaviour
{
    [Header("Radius & Who")]
    public float pullRadius = 20f;              // trigger + ring radius
    public LayerMask affectLayers = ~0;
    public bool onlyAffectRigidbodies = true;

    [Header("Side Pull (curvature-based)")]
    [Tooltip("Side accel ≈ curvatureGain * speed^2. Bigger = stronger curve at any speed.")]
    public float curvatureGain = 0.06f;         // try 0.04–0.10
    [Tooltip("Hard cap for the side acceleration.")]
    public float accelClamp = 50f;

    [Header("Entry Bite (optional)")]
    [Tooltip("Instant sideways delta-V when entering the ring. 0 = off.")]
    public float entrySideImpulse = 0f;         // in m/s (VelocityChange)
    public float entryImpulseMax = 10f;

    [Header("Pulse (optional)")]
    public bool pulseEnabled = true;
    [Tooltip("Pulses per second.")]
    public float pulseFrequencyHz = 3f;
    [Range(0f, 1f)] public float pulseDuty = 0.45f;  // fraction ON
    [Tooltip("Fade in/out times to avoid harsh pops.")]
    public float attackTime = 0.06f, releaseTime = 0.08f;
    [Tooltip("Randomize pulse phase so multiple holes don't sync.")]
    public bool randomizePhase = true;

    [Header("Ring (in-game)")]
    public bool drawRing = true;
    [Range(8, 256)] public int ringSegments = 96;
    public float ringWidth = 0.06f;
    public Color ringColor = new Color(1f, 0.6f, 0.1f, 0.9f);

    // runtime
    SphereCollider trigger;
    LineRenderer ring;
    Material ringMat;

    // per-body state for smooth pulsing
    readonly Dictionary<Rigidbody, float> _amp = new();
    readonly Dictionary<Rigidbody, float> _phase = new();

    void Awake()
    {
        EnsureTrigger();
        EnsureRing();
        RedrawRing();
    }

    void OnEnable() { EnsureTrigger(); EnsureRing(); RedrawRing(); }
    void OnValidate() { EnsureTrigger(); RedrawRing(); }
    void Update() { if (!Application.isPlaying) RedrawRing(); }

    void EnsureTrigger()
    {
        if (!trigger) trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = pullRadius;
    }

    void EnsureRing()
    {
        if (!drawRing)
        {
            if (ring) ring.enabled = false;
            return;
        }
        if (!ring)
        {
            ring = GetComponent<LineRenderer>();
            if (!ring) ring = gameObject.AddComponent<LineRenderer>();
        }
        if (ringMat == null) ringMat = new Material(Shader.Find("Sprites/Default"));

        ring.enabled = true;
        ring.loop = true;
        ring.useWorldSpace = true;
        ring.material = ringMat;
        ring.widthMultiplier = ringWidth;
        ring.startColor = ring.endColor = ringColor;
        ring.numCornerVertices = 4;
        ring.numCapVertices = 2;
        ring.sortingOrder = 1000;
    }

    void RedrawRing()
    {
        if (!drawRing) return;
        EnsureRing();

        int count = Mathf.Max(8, ringSegments);
        ring.positionCount = count;
        Vector3 center = transform.position;
        float step = Mathf.PI * 2f / count;

        for (int i = 0; i < count; i++)
        {
            float a = i * step;
            Vector3 p = new Vector3(Mathf.Cos(a) * pullRadius, Mathf.Sin(a) * pullRadius, 0f);
            ring.SetPosition(i, center + p);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & affectLayers) == 0) return;

        var rb = other.attachedRigidbody;
        if (onlyAffectRigidbodies && rb == null) return;
        if (rb != null && rb.isKinematic) return;

        if (rb)
        {
            // set pulse phase & amplitude
            if (randomizePhase && !_phase.ContainsKey(rb))
                _phase[rb] = Random.value;
            if (!_amp.ContainsKey(rb))
                _amp[rb] = 0f;

            // entry bite (sideways delta-V)
            if (entrySideImpulse > 0f)
            {
                Vector3 toCenter = transform.position - rb.position; toCenter.z = 0f;
                Vector3 v = rb.linearVelocity; v.z = 0f;

                Vector3 sideDir = ComputeSideDir(toCenter, v);
                if (sideDir.sqrMagnitude > 0f)
                {
                    float dv = Mathf.Min(entrySideImpulse, entryImpulseMax);
                    rb.AddForce(sideDir * dv, ForceMode.VelocityChange);
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb)
        {
            _amp.Remove(rb);
            _phase.Remove(rb);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & affectLayers) == 0) return;

        Rigidbody rb = other.attachedRigidbody;
        if (onlyAffectRigidbodies && rb == null) return;
        if (rb != null && rb.isKinematic) return;

        Vector3 center = transform.position;
        Vector3 pos = rb ? rb.position : other.transform.position;

        Vector3 toCenter = center - pos; toCenter.z = 0f;
        float dist = toCenter.magnitude;
        if (dist <= 0f || dist > pullRadius) return;

        Vector3 v = rb ? rb.linearVelocity : Vector3.zero; v.z = 0f;
        Vector3 sideDir = ComputeSideDir(toCenter, v);
        if (sideDir.sqrMagnitude == 0f) return;

        // Speed-scaled side accel (curvature-like), clamped
        float speed = v.magnitude;
        float sideAccel = Mathf.Min(curvatureGain * speed * speed, accelClamp);

        // Pulse envelope 0..1 per-body
        float alpha = 1f;
        if (pulseEnabled && rb != null)
        {
            float phase = _phase.TryGetValue(rb, out var ph) ? ph : 0f;
            float t = Time.time * pulseFrequencyHz + phase;
            float square = (t - Mathf.Floor(t) < pulseDuty) ? 1f : 0f;

            float current = _amp.TryGetValue(rb, out var a) ? a : 0f;
            float target = square;
            float dt = Application.isPlaying ? Time.fixedDeltaTime : 0.02f;
            float tau = target > current ? Mathf.Max(attackTime, 1e-4f) : Mathf.Max(releaseTime, 1e-4f);
            current = Mathf.MoveTowards(current, target, dt / tau);
            _amp[rb] = current;
            alpha = current;
        }

        float accel = sideAccel * alpha;

        if (rb) rb.AddForce(sideDir * accel, ForceMode.Acceleration);
        else other.transform.position += sideDir * (accel * Time.deltaTime);
    }

    // component of "toCenter" perpendicular to velocity (or toward center if nearly stationary)
    static Vector3 ComputeSideDir(Vector3 toCenter, Vector3 velocity)
    {
        float d = toCenter.magnitude;
        if (d <= 1e-6f) return Vector3.zero;
        Vector3 dirToHole = toCenter / d;

        if (velocity.sqrMagnitude > 1e-6f)
        {
            Vector3 vN = velocity.normalized;
            Vector3 side = dirToHole - Vector3.Dot(dirToHole, vN) * vN; // remove along-velocity part
            float m = side.magnitude;
            return (m > 1e-6f) ? side / m : Vector3.zero;
        }
        else
        {
            return dirToHole;
        }
    }
}
