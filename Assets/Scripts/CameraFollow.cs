using UnityEngine;

/// <summary>
/// Makes the camera smoothly follow the player, keeping the same angle.
/// The camera's rotation is whatever you set in the Inspector - this script only moves it.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("What the camera follows (drag the Player here).")]
    public Transform target;

    [Tooltip("Where the camera sits compared to the player. Matches the camera position we set: (0, 32, -22).")]
    public Vector3 offset = new Vector3(0f, 32f, -22f);

    [Tooltip("How long the camera takes to catch up, in seconds. 0 = locked on, bigger = floatier.")]
    [Range(0f, 0.5f)]
    public float smoothTime = 0.15f;

    Vector3 velocity;

    // LateUpdate runs after the player has moved this frame, so the camera never lags a frame behind.
    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
