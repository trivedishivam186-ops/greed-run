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

    [Tooltip("If ticked, the run restarts by itself after dying. Leave unticked when a RunEndScreen handles restarting.")]
    public bool autoRestart = false;

    [Tooltip("Seconds to wait after dying before the run restarts (only if Auto Restart is ticked).")]
    public float restartDelay = 1.5f;

    /// <summary>Fires when the player takes damage (for screen flash, shake and sound).</summary>
    public static event System.Action<float> OnPlayerDamaged;

    /// <summary>Fires once when the player dies.</summary>
    public static event System.Action OnPlayerDied;

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
        else
            OnPlayerDamaged?.Invoke(amount);
    }

    /// <summary>Raise max health (and heal the same amount). Used by level-up upgrades.</summary>
    public void AddMaxHealth(float amount)
    {
        maxHealth += amount;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    void Die()
    {
        IsDead = true;
        Debug.Log("Player died.");
        OnPlayerDied?.Invoke();

        // Stop the player moving.
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        if (autoRestart) StartCoroutine(RestartAfterDelay());
    }

    IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
