using UnityEngine;

public class Grapple : MonoBehaviour
{
    public Asteroid Target;

    [SerializeField] private LineRenderer _lineRenderer;

    private SpaceshipController _ship;
    private ConfigurableJoint _joint;
    private Vector3 _beamStartPosLocal;

    private void Start()
    {
        _ship = GetComponentInParent<SpaceshipController>();

        var distance = Vector3.Distance(transform.position, Target.transform.position);
        var direction = (Target.transform.position - transform.position).normalized;
        var isRightSide = Vector3.Dot(direction, transform.right) > 0;
        _beamStartPosLocal = (isRightSide ? Vector3.right : -Vector3.right) * 0.5f;

        _joint = _ship.rb.gameObject.AddComponent<ConfigurableJoint>();
        _joint.connectedBody = Target.GetComponentInParent<Rigidbody>();
        _joint.autoConfigureConnectedAnchor = false;
        _joint.anchor = _beamStartPosLocal;
        _joint.connectedAnchor = Vector3.zero;

        _joint.xMotion = ConfigurableJointMotion.Limited;
        _joint.yMotion = ConfigurableJointMotion.Limited;
        _joint.zMotion = ConfigurableJointMotion.Locked;

        _joint.enableCollision = true;

        _joint.linearLimit = new SoftJointLimit
        {
            limit = distance,
            bounciness = 0.1f
        };
    }

    private void LateUpdate()
    {
        var startPos = _ship.transform.TransformPoint(_beamStartPosLocal);
        var endPos = _joint.connectedBody.transform.TransformPoint(_joint.connectedAnchor);

        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);
    }

    private void OnDestroy()
    {
        if (_joint) Destroy(_joint);
    }
}