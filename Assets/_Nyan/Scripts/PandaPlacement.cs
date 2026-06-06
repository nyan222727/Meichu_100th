using UnityEngine;
using UnityEngine.Serialization;

public class PandaPlacement : MonoBehaviour
{
    [SerializeField] private GameObject pandaPrefab;
    [SerializeField] private ReticleBehaviour reticle;
    [SerializeField] private DrivingSurfaceManager drivingSurfaceManager;
    [FormerlySerializedAs("projectileLauncher")]
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private bool lockPlaneAfterPlacement = true;
    [SerializeField] private bool hideReticleAfterPlacement = true;

    private GameObject spawnedPanda;

    private void Update()
    {
        if (spawnedPanda != null || pandaPrefab == null || reticle == null)
        {
            return;
        }

        if (!WasTapped() || reticle.CurrentPlane == null)
        {
            return;
        }

        spawnedPanda = Instantiate(pandaPrefab, reticle.transform.position, reticle.transform.rotation);
        EnsureDamageable(spawnedPanda);

        if (lockPlaneAfterPlacement && drivingSurfaceManager != null)
        {
            drivingSurfaceManager.LockPlane(reticle.CurrentPlane);
        }

        if (hideReticleAfterPlacement)
        {
            reticle.gameObject.SetActive(false);
        }

        if (combatController == null)
        {
            combatController = FindFirstObjectByType<PlayerCombatController>(FindObjectsInactive.Include);
        }

        if (combatController != null)
        {
            combatController.enabled = true;
        }
    }

    private static bool WasTapped()
    {
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        if (Input.touchCount == 0)
        {
            return false;
        }

        return Input.GetTouch(0).phase == TouchPhase.Began;
    }

    private static void EnsureDamageable(GameObject panda)
    {
        MonoBehaviour[] behaviours = panda.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IDamageable)
            {
                return;
            }
        }

        panda.AddComponent<PandaHealth>();
    }
}
