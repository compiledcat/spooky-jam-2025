using System;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
public class Track : MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] private float[] _checkpointPositions = Array.Empty<float>();
    [SerializeField] private SplineContainer _splineContainer;

    [SerializeField] private Transform _checkpointsParent;
    [SerializeField] private Transform _checkpointPrefab;

    private bool _isDirty;

    private void OnValidate()
    {
        _splineContainer ??= GetComponent<SplineContainer>();

        if (Application.isPlaying) return;
        _isDirty = true;
    }

    private void Update()
    {
        if (!_isDirty) return;

        RegenerateCheckpoints();
        _isDirty = false;
    }

    private void RegenerateCheckpoints()
    {
        if (!_splineContainer || !_checkpointPrefab || !_checkpointsParent) return;

        // clear existing
        var existingCheckpoints = new Transform[_checkpointsParent.childCount];
        for (var i = 0; i < _checkpointsParent.childCount; i++)
        {
            existingCheckpoints[i] = _checkpointsParent.GetChild(i);
        }

        foreach (var child in existingCheckpoints)
        {
            DestroyImmediate(child.gameObject);
        }

        // create new
        foreach (var t in _checkpointPositions)
        {
            var position = transform.TransformPoint(_splineContainer.Spline.EvaluatePosition(t));
            var rotation = Quaternion.LookRotation(_splineContainer.Spline.EvaluateTangent(t), -Vector3.forward);

            Instantiate(_checkpointPrefab, position, rotation, _checkpointsParent);
        }
    }
}