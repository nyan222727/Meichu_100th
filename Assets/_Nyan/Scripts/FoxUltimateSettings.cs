using UnityEngine;

[CreateAssetMenu(menuName = "Meichu/Combat/Fox Ultimate Settings")]
public class FoxUltimateSettings : ScriptableObject
{
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private float spawnMinDelay = 2.5f;
    [SerializeField] private float spawnMaxDelay = 5.5f;
    [SerializeField, Min(0f)] private float spawnWarningDelay = 0.35f;
    [SerializeField, Min(0.1f)] private float spawnWindowDuration = 300f;
    [SerializeField, Min(0)] private int spawnsPerWindow = 2;
    [SerializeField] private float visibleDuration = 1.1f;
    [Tooltip("Normalized by the shorter screen side. 0.1923 is a 150px diameter target on a 390px-wide phone design.")]
    [SerializeField] private float targetRadius = 0.1923077f;
    [SerializeField] private float outerMinRadius = 0.35f;
    [SerializeField] private float outerMaxRadius = 0.43f;
    [SerializeField, Range(0f, 180f)] private float targetFanMinAngle = 35f;
    [SerializeField, Range(0f, 180f)] private float targetFanMaxAngle = 145f;
    [SerializeField] private float spawnDistanceFromCamera = 1.1f;
    [SerializeField] private int damage = 150;

    public PlayerUltimateConfig ToConfig()
    {
        return new PlayerUltimateConfig
        {
            FoxPrefab = summonPrefab,
            SpawnMinDelay = spawnMinDelay,
            SpawnMaxDelay = spawnMaxDelay,
            SpawnWarningDelay = spawnWarningDelay,
            SpawnWindowDuration = spawnWindowDuration,
            SpawnsPerWindow = spawnsPerWindow,
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
        spawnWarningDelay = Mathf.Max(0f, spawnWarningDelay);
        spawnWindowDuration = Mathf.Max(0.1f, spawnWindowDuration);
        spawnsPerWindow = Mathf.Max(0, spawnsPerWindow);
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
