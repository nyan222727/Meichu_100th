using System.Collections;
using UnityEngine;

public class AutoShooter : MonoBehaviour
{
    [Header("射擊設定")]
    [Tooltip("請把你的方塊 (預製體) 拖曳到這裡")]
    public GameObject bulletPrefab;

    [Tooltip("射擊的初始力道")]
    public float shootForce = 15f;

    [Tooltip("每幾秒射擊一次")]
    public float fireRate = 5f;

    void Start()
    {
        // 遊戲開始時，啟動自動射擊的迴圈
        StartCoroutine(ShootRoutine());
    }

    IEnumerator ShootRoutine()
    {
        while (true)
        {
            Shoot();
            // 等待 5 秒後再執行下一次迴圈
            yield return new WaitForSeconds(fireRate);
        }
    }

    void Shoot()
    {
        // 1. 在發射器(這個物件)的當前位置與角度，生成一個新的方塊
        GameObject newBullet = Instantiate(bulletPrefab, transform.position, transform.rotation);

        // 2. 獲取方塊身上的剛體，並給予向前的瞬間推力
        Rigidbody rb = newBullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 使用 ForceMode.Impulse 模擬瞬間爆發力 (像開槍一樣)
            rb.AddForce(transform.forward * shootForce, ForceMode.Impulse);
        }

        // 3. 為了避免時間久了場景塞滿方塊導致卡頓，設定子彈在 10 秒後自動銷毀
        Destroy(newBullet, 10f);
    }
}