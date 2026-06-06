using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;

public class PandaPlacement : MonoBehaviour
{
    [SerializeField] private GameObject pandaPrefab;
    [SerializeField] private GameObject gameplayFloorPrefab;
    [SerializeField] private ReticleBehaviour reticle;
    [SerializeField] private DrivingSurfaceManager drivingSurfaceManager;
    [FormerlySerializedAs("projectileLauncher")]
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private bool lockPlaneAfterPlacement = true;
    [SerializeField] private bool hideReticleAfterPlacement = true;

    private GameObject spawnedPanda;
    private GameObject gameplayFloor;

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

        ARPlane placementPlane = reticle.CurrentPlane;

        spawnedPanda = Instantiate(pandaPrefab, reticle.transform.position, reticle.transform.rotation);
        EnsureDamageable(spawnedPanda);

        if (gameplayFloorPrefab != null)
        {
            gameplayFloor = Instantiate(
                gameplayFloorPrefab,
                placementPlane.transform.position,
                placementPlane.transform.rotation);

            StopPlaneDetection();
        }
        else if (lockPlaneAfterPlacement && drivingSurfaceManager != null)
        {
            drivingSurfaceManager.LockPlane(placementPlane);
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

    private void StopPlaneDetection()
    {
        if (drivingSurfaceManager == null)
        {
            return;
        }

        ARPlaneManager planeManager = drivingSurfaceManager.PlaneManager;
        if (planeManager != null)
        {
            foreach (ARPlane detectedPlane in planeManager.trackables)
            {
                detectedPlane.gameObject.SetActive(false);
            }

            planeManager.enabled = false;
        }

        ARRaycastManager raycastManager = drivingSurfaceManager.RaycastManager;
        if (raycastManager != null)
        {
            raycastManager.enabled = false;
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
