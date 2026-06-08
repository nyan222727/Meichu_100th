using System;
using UnityEngine;

public interface IPlayerDamageReceiver
{
    void TakeDamage(int amount);
    void Heal(int amount);
}

public interface IPlayerStatus
{
    event Action<int, int> HealthChanged;

    Transform PlayerTransform { get; }
    int MaxHealth { get; }
    int CurrentHealth { get; }
    float HealthRatio { get; }
    bool IsDead { get; }
    bool IsBlocking { get; }
}
