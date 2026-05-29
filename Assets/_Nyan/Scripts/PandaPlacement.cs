using UnityEngine;

public class PandaPlacement : MonoBehaviour
{
    [SerializeField] private GameObject pandaPrefab;
    [SerializeField] private ReticleBehaviour reticle;
    [SerializeField] private DrivingSurfaceManager drivingSurfaceManager;
    [SerializeField] private Drag projectileLauncher;
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
        EnsurePandaHealth(spawnedPanda);

        if (lockPlaneAfterPlacement && drivingSurfaceManager != null)
        {
            drivingSurfaceManager.LockPlane(reticle.CurrentPlane);
        }

        if (hideReticleAfterPlacement)
        {
            reticle.gameObject.SetActive(false);
        }

        if (projectileLauncher == null)
        {
            projectileLauncher = FindFirstObjectByType<Drag>(FindObjectsInactive.Include);
        }

        if (projectileLauncher != null)
        {
            projectileLauncher.enabled = true;
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

    private static void EnsurePandaHealth(GameObject panda)
    {
        if (panda.GetComponentInChildren<PandaHealth>() != null)
        {
            return;
        }

        panda.AddComponent<PandaHealth>();
    }
}
