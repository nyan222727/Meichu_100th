
using UnityEngine;

public class Launcher : MonoBehaviour
{
    public Rigidbody _prefabWithRigidbody;
    void Update()
    {
#if UNITY_EDITOR
        if(Input.GetMouseButtonDown(0))
#else
        if (Input.touchCount > 0)
#endif
        {
            // spawn in front of at the camera
            var pos = Camera.main.transform.position;
            var forw = Camera.main.transform.forward;
            var thing = Instantiate(_prefabWithRigidbody, pos+(forw*0.4f), Quaternion.identity);

            thing.AddForce(forw * 200.0f);
        }
    }
}
