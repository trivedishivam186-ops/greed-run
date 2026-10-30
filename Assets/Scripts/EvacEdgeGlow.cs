using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// While the evac pad is open and off-screen, the screen edges glow on the side where the pad is.
/// Pad to the right = right edge glows. Pad up-left = top and left edges glow.
/// When the pad comes on screen the glow fades out (you can see the beam by then).
/// The script draws the glow itself - just put it on an empty, full-screen UI object.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EvacEdgeGlow : MonoBehaviour
{
    [Header("References")]
    public EvacManager evac;

    [Tooltip("The game camera. Leave empty to use the main camera.")]
    public Camera cam;

    [Header("Look (designer knobs)")]
    public Color glowColor = new Color(1f, 0.69f, 0f, 1f); // gold

    [Tooltip("How far the glow reaches in from the edge (in UI units; the screen is 540 wide).")]
    public float thickness = 110f;

    [Tooltip("Brightness at its strongest (0-1).")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.85f;

    [Tooltip("Pulses per second normally.")]
    public float pulseSpeed = 1.2f;

    [Tooltip("Pulses per second in the last few seconds before the evac closes.")]
    public float urgentPulseSpeed = 3f;

    [Tooltip("Seconds left when the pulse turns urgent.")]
    public float urgentWhenSecondsLeft = 3f;

    [Tooltip("How quickly the glow fades in and out.")]
    public float fadeSpeed = 6f;

    RawImage top, bottom, left, right;
    float intensity; // 0..1, smoothed

    void Awake()
    {
        if (cam == null) cam = Camera.main;

        // Build two soft gradient textures: one fading up-down, one fading left-right.
        Texture2D vertical = MakeGradient(true);
        Texture2D horizontal = MakeGradient(false);

        // Each edge stretches along one side of the screen. uvRect flips the gradient
        // so the bright part always touches the screen edge.
        top    = MakeEdge("GlowTop",    vertical,   new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Rect(0, 0, 1, 1));
        bottom = MakeEdge("GlowBottom", vertical,   new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Rect(0, 1, 1, -1));
        right  = MakeEdge("GlowRight",  horizontal, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f), new Rect(0, 0, 1, 1));
        left   = MakeEdge("GlowLeft",   horizontal, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f), new Rect(1, 0, -1, 1));

        SetWeights(0, 0, 0, 0, 0);
    }

    void Update()
    {
        // Work out where the pad is compared to the middle of the screen.
        Vector2 dir = Vector2.zero;
        bool show = false;

        if (evac != null && evac.IsOpen && !evac.RunOver && evac.evacPad != null && cam != null)
        {
            Vector3 vp = cam.WorldToViewportPoint(evac.evacPad.position);
            if (vp.z < 0f) { vp.x = 1f - vp.x; vp.y = 1f - vp.y; } // behind the camera: flip

            bool onScreen = vp.z > 0f && vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.05f && vp.y < 0.95f;
            if (!onScreen)
            {
                show = true;
                dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f).normalized;
            }
        }

        intensity = Mathf.MoveTowards(intensity, show ? 1f : 0f, fadeSpeed * Time.deltaTime);
        if (intensity <= 0f)
        {
            SetWeights(0, 0, 0, 0, 0);
            return;
        }

        // Faster pulse when the evac is about to close.
        bool urgent = evac != null && evac.TimeLeftOpen <= urgentWhenSecondsLeft;
        float speed = urgent ? urgentPulseSpeed : pulseSpeed;
        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * speed * Mathf.PI * 2f);

        // How much each edge should glow: only the side(s) facing the pad.
        float wRight  = Sharpen(dir.x);
        float wLeft   = Sharpen(-dir.x);
        float wTop    = Sharpen(dir.y);
        float wBottom = Sharpen(-dir.y);

        SetWeights(wTop, wBottom, wLeft, wRight, intensity * pulse * maxAlpha);
    }

    // Ignore small sideways amounts so only the edges really facing the pad light up.
    static float Sharpen(float v) => Mathf.Clamp01((v - 0.25f) / 0.75f);

    void SetWeights(float t, float b, float l, float r, float alpha)
    {
        SetAlpha(top, t * alpha);
        SetAlpha(bottom, b * alpha);
        SetAlpha(left, l * alpha);
        SetAlpha(right, r * alpha);
    }

    void SetAlpha(RawImage img, float a)
    {
        if (img == null) return;
        Color c = glowColor;
        c.a = a;
        img.color = c;
        img.enabled = a > 0.001f;
    }

    RawImage MakeEdge(string name, Texture2D tex, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Rect uv)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        // Stretch along the edge, 'thickness' deep into the screen.
        bool horizontalEdge = anchorMin.y == anchorMax.y;
        rt.sizeDelta = horizontalEdge ? new Vector2(0f, thickness) : new Vector2(thickness, 0f);

        RawImage img = go.GetComponent<RawImage>();
        img.texture = tex;
        img.uvRect = uv;
        img.raycastTarget = false; // never block touches
        return img;
    }

    // A small texture that's solid at one end and fades to nothing at the other.
    static Texture2D MakeGradient(bool vertical)
    {
        const int size = 64;
        Texture2D tex = vertical ? new Texture2D(1, size, TextureFormat.RGBA32, false)
                                 : new Texture2D(size, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int i = 0; i < size; i++)
        {
            float t = i / (float)(size - 1);   // 0 = inner side, 1 = screen edge
            float a = t * t;                   // soft falloff
            Color c = new Color(1f, 1f, 1f, a);
            if (vertical) tex.SetPixel(0, i, c);
            else tex.SetPixel(i, 0, c);
        }
        tex.Apply();
        return tex;
    }
}
