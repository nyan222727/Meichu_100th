using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Meteor : MonoBehaviour
{
    [Header("Warning")]
    public GameObject warningPrefab;
    public float warningTime = 1.2f;
    public float warningYOffset = 0.03f;

    [Header("Fall Settings")]
    public float fallSpeed = 12f;
    public float rotateSpeed = 360f;

    [Header("Damage Settings")]
    public int damage = 20;
    public string playerTag = "Player";

    [Header("Effect")]
    public GameObject hitEffect;

    private Vector3 targetPosition;
    private GameObject warningObj;

    private bool hasTarget = false;
    private bool isFalling = false;
    private bool hasHit = false;

    public void Init(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
        hasTarget = true;

        StartCoroutine(MeteorRoutine());
    }

    private IEnumerator MeteorRoutine()
    {
        SpawnWarning();

        yield return new WaitForSeconds(warningTime);

        isFalling = true;
    }

    private void Update()
    {
        if (!hasTarget || !isFalling || hasHit) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            fallSpeed * Time.deltaTime
        );

        transform.Rotate(Vector3.right, rotateSpeed * Time.deltaTime, Space.Self);

        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance <= 0.1f)
        {
            // 沒有撞到 Player，代表這顆隕石 miss，不造成傷害。
            FinishMeteor(false);
        }
    }

    private void SpawnWarning()
    {
        if (warningPrefab == null) return;

        warningObj = Instantiate(
            warningPrefab,
            targetPosition + Vector3.up * warningYOffset,
            Quaternion.identity
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHitPlayer(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHitPlayer(collision.collider);
    }

    private void TryHitPlayer(Collider other)
    {
        if (!isFalling || hasHit || other == null) return;

        if (!IsPlayerCollider(other)) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth = other.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        FinishMeteor(true);
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            return true;
        }

        Transform parent = other.transform.parent;

        while (parent != null)
        {
            if (parent.CompareTag(playerTag))
            {
                return true;
            }

            parent = parent.parent;
        }

        return false;
    }

    private void FinishMeteor(bool hitPlayer)
    {
        hasHit = true;

        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        if (warningObj != null)
        {
            Destroy(warningObj);
        }

        if (hitPlayer)
        {
            Debug.Log("Meatball Meteor HIT player!");
        }
        else
        {
            Debug.Log("Meatball Meteor missed.");
        }

        Destroy(gameObject);
    }
}
