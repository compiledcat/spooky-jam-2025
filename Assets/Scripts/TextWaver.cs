using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class TextWaver : MonoBehaviour
{
    private Vector3[] _originalPositions;
    [SerializeField] private Vector3 _moveAxis = Vector3.forward;
    [SerializeField] private float _magnitude = 0.1f;
    [SerializeField] private float _speed = 1.0f;

    private void Start()
    {
        _originalPositions = new Vector3[transform.childCount];
        for (var i = 0; i < transform.childCount; i++)
        {
            _originalPositions[i] = transform.GetChild(i).localPosition;
        }
    }

    private void Update()
    {
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            var sign = i % 2 == 0 ? 1 : -1;
            var offset = Mathf.Sin(Time.time * _speed + i) * _magnitude * sign;
            child.localPosition = _originalPositions[i] + _moveAxis * offset;
        }
    }
}