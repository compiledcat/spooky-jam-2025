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

    [ContextMenu("Evenly space checkpoints")]
    private void SetEvenlySpaced()
    {
        // set positions of existing array to be 1 / n apart
        var count = _checkpointPositions.Length;
        for (var i = 0; i < count; i++)
        {
            _checkpointPositions[i] = i / (float)count;
        }

        _isDirty = true;
    }

    [ContextMenu("Regenerate")]
    private void RegenerateCheckpoints()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Track checkpoints should not be changed at runtime - not updating.");
            return;
        }

#if UNITY_EDITOR
        // we use prefabs so they'll update automatically, we just need to create/remove as needed and update position/rotation
        if (!_splineContainer || !_checkpointPrefab || !_checkpointsParent) return;

        // create as many as needed
        var existingCount = _checkpointsParent.childCount;
        var neededCount = _checkpointPositions.Length;

        if (existingCount > neededCount)
        {
            // remove excess
            var excess = existingCount - neededCount;
            for (var i = 0; i < excess; i++)
            {
                var toDestroy = _checkpointsParent.GetChild(existingCount - 1 - i);
                DestroyImmediate(toDestroy.gameObject);
            }
        }
        else if (neededCount > existingCount)
        {
            // add missing
            var toAdd = neededCount - existingCount;
            for (var i = 0; i < toAdd; i++)
            {
                UnityEditor.PrefabUtility.InstantiatePrefab(_checkpointPrefab, _checkpointsParent);
            }
        }

        // modify all to correct position/rotation
        for (var i = 0; i < _checkpointPositions.Length; i++)
        {
            var t = _checkpointPositions[i];
            var position = transform.TransformPoint(_splineContainer.Spline.EvaluatePosition(t));
            var rotation = Quaternion.LookRotation(_splineContainer.Spline.EvaluateTangent(t), -Vector3.forward);

            var checkpoint = _checkpointsParent.GetChild(i);
            checkpoint.position = position;
            checkpoint.rotation = rotation;
#endif
        }
    }
}