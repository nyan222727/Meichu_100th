using UnityEngine;

public static class DamageableFinder
{
    public static IDamageable GetInParent(Component source)
    {
        if (source == null)
        {
            return null;
        }

        MonoBehaviour[] behaviours = source.GetComponentsInParent<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IDamageable damageable)
            {
                return damageable;
            }
        }

        return null;
    }

    public static IDamageable FindFirst()
    {
        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IDamageable damageable && !damageable.IsDefeated)
            {
                return damageable;
            }
        }

        return null;
    }
}
