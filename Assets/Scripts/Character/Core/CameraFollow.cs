using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform player; // Assign the player GameObject in the Inspector
    public Vector2 minBounds; // Minimum X and Y 
    public Vector2 maxBounds; // Maximum X and Y boundary
    public float cameraYOffset = 0;
    public float cameraZoom = 0.5f;
    private float defaultCameraZoom;
    public float smoothSpeed = 5f; // How smooth the camera follows
    private Camera cam;
    private float camHalfWidth, camHalfHeight;

    void Start()
    {
        cam = Camera.main;
        camHalfHeight = cam.orthographicSize;
        cam.orthographicSize = camHalfHeight - cameraZoom;
        camHalfWidth = camHalfHeight * cam.aspect; // Adjust width based on aspect ratio
    }

    void LateUpdate()
    {
        if (!player) 
        {
            player = PersistentPlayerHealth.Instance.GetComponent<Hero>().transform;
        }
        // Update camera zoom
        cam.orthographicSize = camHalfHeight - cameraZoom;
        // Get target position
        float targetX = Mathf.Clamp(player.position.x, minBounds.x + camHalfWidth, maxBounds.x - camHalfWidth);
        float targetY = Mathf.Clamp(player.position.y + cameraYOffset, minBounds.y + camHalfHeight, maxBounds.y - camHalfHeight);
        // Smoothly move the camera
        Vector3 targetPosition = new Vector3(targetX, targetY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}
