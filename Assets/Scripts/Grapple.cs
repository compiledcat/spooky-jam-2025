using UnityEngine;

public class Grapple : MonoBehaviour
{
    [HideInInspector] public Vector3 TargetPosition;

    [SerializeField] private float _maxRange = 30;
    [SerializeField] private float _extendSpeed = 110;
    [SerializeField] private LayerMask _asteroidLayer;
    [SerializeField] private LineRenderer _lineRenderer;

    private SpaceshipController _ship;
    private float _currentLength;
    private bool _isAttached;
    private ConfigurableJoint _joint;
    private GameObject _hitAsteroid;
    private Vector3 _beamStartPosLocal;

    private void Start()
    {
        _ship = GetComponentInParent<SpaceshipController>();

        var direction = (TargetPosition - transform.position).normalized;
        var isRightSide = Vector3.Dot(direction, transform.right) > 0;
        _beamStartPosLocal = (isRightSide ? Vector3.right : -Vector3.right) * 0.5f;
    }

    private void Update()
    {
        if (!_isAttached)
        {
            _currentLength += _extendSpeed * Time.deltaTime;

            var startPos = _ship.transform.TransformPoint(_beamStartPosLocal);
            var direction = (TargetPosition - startPos).normalized;

            // check if hit asteroid
            if (Physics.Raycast(startPos, direction, out var hit, _currentLength, _asteroidLayer))
            {
                _isAttached = true;
                _hitAsteroid = hit.collider.gameObject;

                _joint = _ship.rb.gameObject.AddComponent<ConfigurableJoint>();
                _joint.connectedBody = _hitAsteroid.GetComponentInParent<Rigidbody>();
                _joint.autoConfigureConnectedAnchor = false;
                _joint.anchor = _beamStartPosLocal;
                _joint.connectedAnchor = _hitAsteroid.transform.InverseTransformPoint(hit.point);

                _joint.xMotion = ConfigurableJointMotion.Limited;
                _joint.yMotion = ConfigurableJointMotion.Limited;
                _joint.zMotion = ConfigurableJointMotion.Locked;

                _joint.enableCollision = true;

                _joint.linearLimit = new SoftJointLimit
                {
                    limit = _currentLength,
                    bounciness = 0.1f
                };
            }
            else if (_currentLength >= _maxRange)
            {
                // missed
                Destroy(gameObject);
            }
        }

        RenderBeam();
    }

    private void RenderBeam()
    {
        var startPos = _ship.transform.TransformPoint(_beamStartPosLocal);
        Vector3 endPos;

        if (_isAttached && _hitAsteroid)
        {
            endPos = _joint.connectedBody.transform.TransformPoint(_joint.connectedAnchor);
        }
        else
        {
            var direction = (TargetPosition - startPos).normalized;
            endPos = startPos + direction * _currentLength;
        }

        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);
    }

    private void OnDestroy()
    {
        if (_joint) Destroy(_joint);
    }
}