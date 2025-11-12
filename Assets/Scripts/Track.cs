using System;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
public class Track : MonoBehaviour
{
    [Tooltip("How far along the track the first checkpoint starts")] [Range(0f, 1f)] [SerializeField]
    private float _checkpointPositionOffset = 0f;

    [field: SerializeField]
    [field: Range(0f, 1f)]
    public float[] CheckpointPositions { get; private set; } = Array.Empty<float>();

    [SerializeField] private SplineContainer _splineContainer;

    [SerializeField] private Transform _checkpointsParent;
    [SerializeField] private Checkpoint _checkpointPrefab;


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
        var count = CheckpointPositions.Length;
        for (var i = 0; i < count; i++)
        {
            CheckpointPositions[i] = i / (float)count;
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
        var neededCount = CheckpointPositions.Length;

        if (existingCount > neededCount)
        {
            // remove excess
            for (var i = existingCount - 1; i >= neededCount; i--)
            {
                DestroyImmediate(_checkpointsParent.GetChild(i).gameObject);
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
        for (var i = 0; i < CheckpointPositions.Length; i++)
        {
            var t = CheckpointPositions[i] + _checkpointPositionOffset;
            t -= Mathf.Floor(t); // wrap around 0-1
            var position = transform.TransformPoint(_splineContainer.Spline.EvaluatePosition(t));
            var rotation = Quaternion.LookRotation(_splineContainer.Spline.EvaluateTangent(t), -Vector3.forward);

            var checkpoint = _checkpointsParent.GetChild(i).GetComponent<Checkpoint>();
            checkpoint.transform.position = position;
            checkpoint.transform.rotation = rotation;
            checkpoint.name = $"Checkpoint {i + 1}";
            checkpoint.CheckpointNum = i + 1;

            UnityEditor.EditorUtility.SetDirty(checkpoint);
        }
#endif
    }
}