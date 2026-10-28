using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The player's health. Enemies call TakeDamage when they touch the player.
/// When health reaches 0 the player stops moving and the run restarts after a short pause.
/// (Later this is where "you lose everything you were carrying" will happen.)
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health (designer knobs)")]
    public float maxHealth = 100f;

    [Tooltip("Seconds to wait after dying before the run restarts.")]
    public float restartDelay = 1.5f;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        Debug.Log($"Player hit for {amount}. Health: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0f)
            Die();
    }

    void Die()
    {
        IsDead = true;
        Debug.Log("Player died. Restarting...");

        // Stop the player moving.
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        StartCoroutine(RestartAfterDelay());
    }

    IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
