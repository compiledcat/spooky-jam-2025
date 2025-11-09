using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int CheckpointNum;

    private void OnTriggerEnter(Collider other)
    {
        var ship = other.GetComponentInParent<SpaceshipController>();
        if (ship)
        {
            ship.PassCheckpoint(CheckpointNum);
        }
    }
}