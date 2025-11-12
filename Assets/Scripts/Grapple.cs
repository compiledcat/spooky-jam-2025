using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class Grapple : MonoBehaviour
{
    [HideInInspector] public Asteroid Target;

    [SerializeField] private float _sidewaysLength = 0.75f;
    [SerializeField] private float _maxRange = 80;
    [SerializeField] private float _extendSpeed = 120;
    [SerializeField] private LayerMask _asteroidLayer;
    [SerializeField] private LineRenderer _lineRenderer;

    [SerializeField] private GameObject _beamPinPrefab;
    private Transform _beamPinInstance;

    private SpaceshipController _ship;
    private float _currentLength;
    private bool _isAttached;
    private ConfigurableJoint _joint;
    private Vector3 _beamStartPosLocal;

    private void Start()
    {
        _ship = GetComponentInParent<SpaceshipController>();

        var direction = (Target.transform.position - transform.position).normalized;
        var isRightSide = Vector3.Dot(direction, transform.right) > 0;
        _beamStartPosLocal = (isRightSide ? Vector3.right : -Vector3.right) * _sidewaysLength;
    }

    private void Update()
    {
        if (!_isAttached)
        {
            _currentLength += _extendSpeed * Time.deltaTime;

            var startPos = _ship.transform.TransformPoint(_beamStartPosLocal);

            var pinPos = new Vector3(Target.transform.position.x, Target.transform.position.y, _ship.transform.position.z);
            var direction = (pinPos - startPos).normalized;

            // check if reached asteroid
            var distanceToPin = Vector3.Distance(startPos + direction * _currentLength, pinPos);
            if (distanceToPin <= 2f)
            {
                _isAttached = true;
                _currentLength += distanceToPin;

                // spawn beam pin
                _beamPinInstance = Instantiate(_beamPinPrefab, pinPos, Quaternion.identity).transform;

                // attach joint
                _joint = _ship.rb.gameObject.AddComponent<ConfigurableJoint>();
                _joint.connectedBody = null;
                _joint.autoConfigureConnectedAnchor = false;
                _joint.anchor = _beamStartPosLocal;
                _joint.connectedAnchor = pinPos;

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

        var pinPos = new Vector3(Target.transform.position.x, Target.transform.position.y, _ship.transform.position.z);
        var direction = (pinPos - startPos).normalized;

        if (_isAttached)
        {
            endPos = pinPos;
        }
        else
        {
            endPos = startPos + direction * _currentLength;
        }

        var distance = Vector3.Distance(startPos, endPos);
        transform.localScale = new Vector3(1, 1, distance);

        transform.forward = direction;

        if (_beamPinInstance)
        {
            // _beamPinInstance.right = direction;
            _beamPinInstance.up = -Vector3.forward;
        }
    }

    private void OnDestroy()
    {
        if (_joint) Destroy(_joint);
        if (_beamPinInstance) Destroy(_beamPinInstance.gameObject);
    }
}