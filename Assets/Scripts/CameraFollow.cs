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

    [Header("Screen shake (designer knobs)")]
    [Tooltip("Biggest shake distance at full strength.")]
    public float maxShake = 0.6f;

    [Tooltip("How quickly shake fades (strength lost per second).")]
    public float shakeDecay = 2.5f;

    Vector3 velocity;
    float trauma;            // 0..1, how hard we're shaking right now
    Vector3 shakeOffset;

    /// <summary>Add some shake. 0.1 = tiny bump, 0.4 = big hit, 1 = huge.</summary>
    public void Shake(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    // LateUpdate runs after the player has moved this frame, so the camera never lags a frame behind.
    void LateUpdate()
    {
        if (target == null) return;

        // Follow without the shake, then add the shake on top, so shaking never drifts the camera.
        Vector3 basePos = transform.position - shakeOffset;
        Vector3 desired = target.position + offset;
        basePos = Vector3.SmoothDamp(basePos, desired, ref velocity, smoothTime);

        // Shake strength is trauma squared: small bumps stay subtle, big hits really kick.
        trauma = Mathf.Max(0f, trauma - shakeDecay * Time.unscaledDeltaTime);
        float s = trauma * trauma * maxShake;
        shakeOffset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * s;

        transform.position = basePos + shakeOffset;
    }
}
