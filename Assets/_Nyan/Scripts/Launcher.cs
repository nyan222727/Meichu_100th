
using UnityEngine;

public class Launcher : MonoBehaviour
{
    public Rigidbody _prefabWithRigidbody;

    void Update()
    {
        if (!WasTapped())
        {
            return;
        }

        if (_prefabWithRigidbody == null || Camera.main == null)
        {
            return;
        }

        var cameraTransform = Camera.main.transform;
        var pos = cameraTransform.position;
        var forw = cameraTransform.forward;
        var thing = Instantiate(_prefabWithRigidbody, pos + (forw * 0.4f), Quaternion.identity);

        thing.AddForce(forw * 1000.0f);
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
}
