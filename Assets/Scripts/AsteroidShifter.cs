using UnityEngine;

public class AsteroidShifter : MonoBehaviour
{
    [SerializeField] private float _asteroidRadius = 10f;
    [SerializeField] private Vector3 _axis = Vector3.forward;

    [ContextMenu("Shift")]
    private void Shift()
    {
        foreach (Transform child in transform)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(child, "Shift Asteroid Position");
#endif

            child.localPosition += _axis.normalized * _asteroidRadius * child.localScale.z;
        }
    }
}