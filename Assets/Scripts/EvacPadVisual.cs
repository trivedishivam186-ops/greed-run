using UnityEngine;

/// <summary>
/// Makes the evac pad pulse and spin so it's impossible to miss.
/// Goes on the EvacPad object.
/// </summary>
public class EvacPadVisual : MonoBehaviour
{
    [Tooltip("The part that pulses (e.g. the ring). Leave empty to pulse the whole pad.")]
    public Transform pulsePart;
    public float pulseSpeed = 4f;
    public float pulseAmount = 0.08f;

    [Tooltip("Optional part that spins (e.g. an inner ring).")]
    public Transform spinPart;
    public float spinSpeed = 90f;

    Vector3 baseScale;

    void Awake()
    {
        if (pulsePart == null) pulsePart = transform;
        baseScale = pulsePart.localScale;
    }

    void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        pulsePart.localScale = new Vector3(baseScale.x * s, baseScale.y, baseScale.z * s);

        if (spinPart != null)
            spinPart.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }
}
