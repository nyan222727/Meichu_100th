using UnityEngine;

public class PlumProjectile : MonoBehaviour
{
    public float speed = 6f;
    public int damage = 10;
    public float lifeTime = 5f;

    private Vector3 moveDirection;

    // 傳入目標位置，梅花會朝該 XYZ 位置飛
    public void Init(Vector3 targetPosition)
    {
        moveDirection = (targetPosition - transform.position).normalized;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth = other.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}