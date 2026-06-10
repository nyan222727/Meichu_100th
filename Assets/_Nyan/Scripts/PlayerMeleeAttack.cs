using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Attack Zone")]
    [SerializeField] private float meleeRange = 1.2f;
    [SerializeField] private float meleeHitRadius = 0.35f;
    [SerializeField] private LayerMask meleeHitMask = ~0;

    [Header("Damage")]
    [SerializeField] private int weakDamage = 6;
    [SerializeField] private int mediumDamage = 9;
    [SerializeField] private int strongDamage = 12;

    [Header("Lingering Combo")]
    [SerializeField, Min(1)] private int maxComboHits = 5;
    [SerializeField, Range(0.05f, 1f)] private float chargePerExtraHit = 0.25f;
    [SerializeField, Min(0.01f)] private float comboInterval = 0.25f;
    [SerializeField, Range(0.05f, 1f)] private float comboHitDamageScale = 1f;

    [Header("Visual")]
    [SerializeField] private GameObject slashPrefab;
    [SerializeField] private float slashLifetime = 0.22f;
    [SerializeField] private Vector3 slashScale = Vector3.one;
    [SerializeField] private float slashAngle = -25f;
    [SerializeField] private float alternateSlashAngle = 35f;
    [SerializeField] private bool logAttacks = true;

    private Coroutine activeCombo;

    private void OnValidate()
    {
        meleeRange = Mathf.Max(0.01f, meleeRange);
        meleeHitRadius = Mathf.Max(0.01f, meleeHitRadius);
        weakDamage = Mathf.Max(0, weakDamage);
        mediumDamage = Mathf.Max(weakDamage, mediumDamage);
        strongDamage = Mathf.Max(mediumDamage, strongDamage);
        maxComboHits = Mathf.Max(1, maxComboHits);
        chargePerExtraHit = Mathf.Clamp(chargePerExtraHit, 0.05f, 1f);
        comboInterval = Mathf.Max(0.01f, comboInterval);
        comboHitDamageScale = Mathf.Clamp(comboHitDamageScale, 0.05f, 1f);
        slashLifetime = Mathf.Max(0.01f, slashLifetime);
    }

    private void OnDisable()
    {
        if (activeCombo != null)
        {
            StopCoroutine(activeCombo);
            activeCombo = null;
        }
    }

    public bool Attack(
        Camera sourceCamera,
        Vector2 slashViewportPosition,
        float displacementRatio,
        float chargeRatio,
        bool appliesHitStun,
        float hitStunDuration)
    {
        if (sourceCamera == null)
        {
            Debug.LogWarning("[PlayerMeleeAttack] Missing camera.");
            return false;
        }

        if (activeCombo != null)
        {
            StopCoroutine(activeCombo);
        }

        Ray attackRay = sourceCamera.ViewportPointToRay(slashViewportPosition);
        Vector3 attackPosition = FindFixedAttackPosition(attackRay);
        Quaternion attackRotation = Quaternion.LookRotation(
            sourceCamera.transform.forward,
            sourceCamera.transform.up);
        int hitCount = GetComboHitCount(chargeRatio);
        int damage = EvaluateDamage(displacementRatio);

        activeCombo = StartCoroutine(AttackComboRoutine(
            attackPosition,
            attackRotation,
            hitCount,
            damage,
            appliesHitStun,
            hitStunDuration));
        return true;
    }

    private IEnumerator AttackComboRoutine(
        Vector3 attackPosition,
        Quaternion attackRotation,
        int hitCount,
        int baseDamage,
        bool appliesHitStun,
        float hitStunDuration)
    {
        float perHitScale = hitCount > 1 ? comboHitDamageScale : 1f;
        int damagePerHit = Mathf.Max(1, Mathf.RoundToInt(baseDamage * perHitScale));

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            bool isFinalHit = hitIndex == hitCount - 1;
            GameAudioController.PlayKnife();
            PlaySlashVisual(attackPosition, attackRotation, hitIndex);
            DamageTargetsAtPosition(
                attackPosition,
                damagePerHit,
                appliesHitStun && isFinalHit,
                hitStunDuration);

            if (!isFinalHit)
            {
                yield return new WaitForSeconds(comboInterval);
            }
        }

        activeCombo = null;
    }

    private Vector3 FindFixedAttackPosition(Ray attackRay)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            attackRay,
            meleeRange,
            meleeHitMask,
            QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        Vector3 nearestSurface = attackRay.GetPoint(meleeRange);
        for (int i = 0; i < hits.Length; i++)
        {
            if (i == 0)
            {
                nearestSurface = hits[i].point;
            }

            if (DamageableFinder.GetInParent(hits[i].collider) != null)
            {
                return hits[i].point;
            }
        }

        return nearestSurface;
    }

    private void DamageTargetsAtPosition(
        Vector3 attackPosition,
        int damage,
        bool appliesHitStun,
        float hitStunDuration)
    {
        Collider[] hits = Physics.OverlapSphere(
            attackPosition,
            meleeHitRadius,
            meleeHitMask,
            QueryTriggerInteraction.Ignore);
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = DamageableFinder.GetInParent(hits[i]);
            if (damageable == null || damageable.IsDefeated || !damagedTargets.Add(damageable))
            {
                continue;
            }

            damageable.TakeDamage(damage);
            if (appliesHitStun && !damageable.IsDefeated && damageable is IHitStunnable hitStunnable)
            {
                hitStunnable.ApplyHitStun(hitStunDuration);
                HitStunStatusIndicator.ShowOn(damageable.DamageTransform, hitStunDuration);
            }

            if (logAttacks)
            {
                string targetName = damageable.DamageTransform != null
                    ? damageable.DamageTransform.name
                    : hits[i].name;
                Debug.Log(
                    $"[PlayerMeleeAttack] Hit {targetName}. Damage={damage}, " +
                    $"HitStun={appliesHitStun}");
            }
        }
    }

    private void PlaySlashVisual(Vector3 attackPosition, Quaternion attackRotation, int hitIndex)
    {
        if (slashPrefab == null)
        {
            return;
        }

        float angle = hitIndex % 2 == 0 ? slashAngle : alternateSlashAngle;
        GameObject slash = Instantiate(
            slashPrefab,
            attackPosition,
            attackRotation * Quaternion.Euler(0f, 0f, angle));
        slash.transform.localScale = Vector3.Scale(slash.transform.localScale, slashScale);
        Destroy(slash, slashLifetime);
    }

    private int GetComboHitCount(float chargeRatio)
    {
        int extraHits = Mathf.FloorToInt((Mathf.Clamp01(chargeRatio) + 0.0001f) / chargePerExtraHit);
        return Mathf.Clamp(1 + extraHits, 1, maxComboHits);
    }

    private int EvaluateDamage(float displacementRatio)
    {
        float ratio = Mathf.Clamp01(displacementRatio);
        if (ratio <= 0.5f)
        {
            return Mathf.RoundToInt(Mathf.Lerp(weakDamage, mediumDamage, ratio * 2f));
        }

        return Mathf.RoundToInt(Mathf.Lerp(mediumDamage, strongDamage, (ratio - 0.5f) * 2f));
    }
}
