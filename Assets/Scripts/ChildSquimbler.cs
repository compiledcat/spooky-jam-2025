using System.Linq;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class ChildSquimbler : MonoBehaviour
{
    private float _lastTime;
    [SerializeField] private bool _randomiseChildPositions;
    [SerializeField] private Vector3 _moveAxis = Vector3.forward;
    [SerializeField] private float _magnitude = 0.05f;
    [SerializeField] private float _speed = 5.0f;

    private void Start()
    {
        if (_randomiseChildPositions)
        {
            var localPositions = new Vector3[transform.childCount];
            for (var i = 0; i < transform.childCount; i++)
            {
                localPositions[i] = transform.GetChild(i).localPosition;
            }

            // randomise positions
            localPositions = localPositions.OrderBy(_ => Random.value).ToArray();

            for (var i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).localPosition = localPositions[i];
            }
        }
    }

    private void Update()
    {
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            var sign = i % 2 == 0 ? 1 : -1;
            var lastOffset = Mathf.Sin(_lastTime * _speed + i) * _magnitude * sign;
            var offset = Mathf.Sin(Time.time * _speed + i) * _magnitude * sign;

            // apply difference from last frame, allows effects to be stacked
            child.localPosition += _moveAxis * (offset - lastOffset);
        }

        _lastTime = Time.time;
    }
}