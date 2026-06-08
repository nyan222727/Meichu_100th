using UnityEngine;

public static class DamageFeedbackUtility
{
    public static void ShowDamage(IDamageable damageable, int amount)
    {
        if (damageable == null || amount <= 0 || damageable.DamageTransform == null)
        {
            return;
        }

        Transform damageTransform = damageable.DamageTransform;
        DamageNumberFeedback feedback = damageTransform.GetComponentInChildren<DamageNumberFeedback>();
        if (feedback == null)
        {
            feedback = damageTransform.GetComponentInParent<DamageNumberFeedback>();
        }

        if (feedback == null)
        {
            feedback = damageTransform.gameObject.AddComponent<DamageNumberFeedback>();
        }

        feedback.Show(amount);
    }
}
