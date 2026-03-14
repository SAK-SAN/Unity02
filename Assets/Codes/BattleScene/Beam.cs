using UnityEngine;

public class Beam : MonoBehaviour
{
    [Header("ビーム攻撃の設定")]
    public int damage = 1;
    public float lifetime = 1.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            HP playerHP = collision.GetComponent<HP>();
            if(playerHP != null)
            {
                playerHP.TakeDamage(damage);
            }
        }
    }
}
