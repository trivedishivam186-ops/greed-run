using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The screen shown when a run ends (extracted or died).
/// Freezes the game, shows the result, and starts a new run on tap / click / any key.
/// </summary>
public class RunEndScreen : MonoBehaviour
{
    [Tooltip("The panel to show (hidden during play).")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text bodyText;

    [Tooltip("Seconds before a tap restarts, so a panicked thumb doesn't skip the result.")]
    public float minShowTime = 1f;

    bool showing;
    float shownAt;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Show(string title, string body)
    {
        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body + "\n\n<size=70%>TAP TO PLAY AGAIN</size>";

        showing = true;
        shownAt = Time.unscaledTime;
        Time.timeScale = 0f; // freeze the game behind the screen
    }

    void Update()
    {
        if (!showing || Time.unscaledTime - shownAt < minShowTime) return;

        bool pressed =
            (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);

        if (pressed)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
