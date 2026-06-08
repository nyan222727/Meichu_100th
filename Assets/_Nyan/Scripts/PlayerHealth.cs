using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IPlayerDamageReceiver, IPlayerStatus
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private bool isBlocking;
    [SerializeField] private bool vibrateOnDamage = true;
    [SerializeField, Min(1)] private int minDamageForVibration = 1;

    public event Action<int, int> HealthChanged;
    public event Action<int> Damaged;

    public Transform PlayerTransform => transform;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float HealthRatio => maxHealth <= 0 ? 0f : Mathf.Clamp01(currentHealth / (float)maxHealth);
    public bool IsDead => currentHealth <= 0;
    public bool IsBlocking => isBlocking;

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void SetHealth(int current, int max)
    {
        int previousMaxHealth = maxHealth;
        int previousCurrentHealth = currentHealth;

        maxHealth = Mathf.Max(1, max);
        currentHealth = Mathf.Clamp(current, 0, maxHealth);

        if (previousMaxHealth != maxHealth || previousCurrentHealth != currentHealth)
        {
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int previousHealth = currentHealth;
        SetCurrentHealth(Mathf.Max(0, currentHealth - amount));
        int actualDamage = previousHealth - currentHealth;

        if (actualDamage <= 0)
        {
            return;
        }

        Damaged?.Invoke(actualDamage);

        if (vibrateOnDamage && actualDamage >= minDamageForVibration)
        {
            TriggerDamageVibration();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetCurrentHealth(Mathf.Min(maxHealth, currentHealth + amount));
    }

    public void SetBlocking(bool value)
    {
        isBlocking = value;
    }

    private void SetCurrentHealth(int value)
    {
        int clampedValue = Mathf.Clamp(value, 0, maxHealth);
        if (currentHealth == clampedValue)
        {
            return;
        }

        currentHealth = clampedValue;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private static void TriggerDamageVibration()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
