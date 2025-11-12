using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowShip : MonoBehaviour
{
    [SerializeField] private SpaceshipController ship;

    private Camera cam;
    [SerializeField] private float camLinearLerpSpeed = 10.0f;
    [SerializeField] private float camAngularLerpSpeed = 2.0f;
    [SerializeField] private float maxCamLookaheadAmount = 0.8f; //Multiplied by the camera's half-height
    [SerializeField] private float maxFovIncreaseAmount = 0.1f; //Multiplied by the camera's half-height

    private float camDefaultOrthoSize;
    
    private void Start()
    {
        cam = Camera.main!;
        camDefaultOrthoSize = cam.orthographicSize;
    }

    [ContextMenu("Move to player")]
    private void SetToPlayer()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Move Camera to Player");
#endif

        transform.position = new Vector3(ship.transform.position.x, ship.transform.position.y, transform.position.z);
        transform.rotation = ship.transform.rotation;
    }

    private void LateUpdate()
    {
        //When the player thrusts forward, the camera should be moved up to be higher than the spaceship - so the player can see more of what's ahead of them
        //The amount the camera moves ahead of the spaceship is a function of the spaceship's velocity - higher velocity, further ahead
        //The maximum distance the camera can move ahead of the spaceship is the maxCamLookaheadAmount * half the camera's orthographic height
        float t = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(0.0f, ship.maxLinearVelocity, ship.rb.linearVelocity.magnitude));
        
        float camLookaheadAmount = Mathf.Lerp(0.0f, maxCamLookaheadAmount, t);
        Vector2 newCamPos = Vector2.Lerp(transform.position, ship.transform.position + ship.rb.linearVelocity.normalized * (camLookaheadAmount * cam.orthographicSize), camLinearLerpSpeed * Time.deltaTime);
        transform.position = new Vector3(newCamPos.x, newCamPos.y, transform.position.z);

        //Same as above, but increase the fov (orthographic size) when the spaceship is moving fast
        float camFOVIncrease = Mathf.Lerp(0.0f, maxFovIncreaseAmount, t);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, camDefaultOrthoSize + camFOVIncrease * camDefaultOrthoSize, camLinearLerpSpeed * Time.deltaTime);

        //Also apply some slerpin' to the rotation
        //Look in the ship's down direction with our top in the ship's forward direction 
        var lookVector = -ship.transform.up;
        var targetRotation = Quaternion.LookRotation(lookVector, ship.transform.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, camAngularLerpSpeed * Time.deltaTime);
    }
}