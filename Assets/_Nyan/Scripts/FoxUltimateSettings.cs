using UnityEngine;

[CreateAssetMenu(menuName = "Meichu/Combat/Fox Ultimate Settings")]
public class FoxUltimateSettings : ScriptableObject
{
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private float spawnMinDelay = 2.5f;
    [SerializeField] private float spawnMaxDelay = 5.5f;
    [SerializeField] private float visibleDuration = 1.1f;
    [SerializeField] private float targetRadius = 0.045f;
    [SerializeField] private float outerMinRadius = 0.35f;
    [SerializeField] private float outerMaxRadius = 0.43f;
    [SerializeField, Range(0f, 180f)] private float targetFanMinAngle = 35f;
    [SerializeField, Range(0f, 180f)] private float targetFanMaxAngle = 145f;
    [SerializeField] private float spawnDistanceFromCamera = 1.1f;
    [SerializeField] private int damage = 45;

    public PlayerUltimateConfig ToConfig()
    {
        return new PlayerUltimateConfig
        {
            FoxPrefab = summonPrefab,
            SpawnMinDelay = spawnMinDelay,
            SpawnMaxDelay = spawnMaxDelay,
            VisibleDuration = visibleDuration,
            TargetRadius = targetRadius,
            OuterMinRadius = outerMinRadius,
            OuterMaxRadius = outerMaxRadius,
            TargetFanMinAngle = targetFanMinAngle,
            TargetFanMaxAngle = targetFanMaxAngle,
            SpawnDistanceFromCamera = spawnDistanceFromCamera,
            FoxDamage = damage
        };
    }

    private void OnValidate()
    {
        spawnMinDelay = Mathf.Max(0.1f, spawnMinDelay);
        spawnMaxDelay = Mathf.Max(spawnMinDelay, spawnMaxDelay);
        visibleDuration = Mathf.Max(0.1f, visibleDuration);
        targetRadius = Mathf.Max(0.01f, targetRadius);
        outerMinRadius = Mathf.Max(0f, outerMinRadius);
        outerMaxRadius = Mathf.Max(outerMinRadius, outerMaxRadius);
        targetFanMinAngle = Mathf.Clamp(targetFanMinAngle, 0f, 180f);
        targetFanMaxAngle = Mathf.Clamp(Mathf.Max(targetFanMaxAngle, targetFanMinAngle), 0f, 180f);
        spawnDistanceFromCamera = Mathf.Max(0.01f, spawnDistanceFromCamera);
        damage = Mathf.Max(0, damage);
    }
}
