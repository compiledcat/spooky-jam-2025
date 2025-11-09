using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class ChildSquimbler : MonoBehaviour
{
    private float _lastTime;
    [SerializeField] private Vector3 _moveAxis = Vector3.forward;
    [SerializeField] private float _magnitude = 0.05f;
    [SerializeField] private float _speed = 5.0f;

    private void Update()
    {
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            var sign = i % 2 == 0 ? 1 : -1;
            var lastOffset = Mathf.Sin(_lastTime * _speed + i) * _magnitude * sign;
            var offset = Mathf.Sin(Time.time * _speed + i) * _magnitude * sign;
            
            // work from original position, allows effects to be stacked
            child.localPosition -= _moveAxis * lastOffset;
            child.localPosition += _moveAxis * offset;
        }
        
        _lastTime = Time.time;
    }
}