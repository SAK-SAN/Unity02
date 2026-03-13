using System;
using Unity.Mathematics;
using UnityEngine;

public class StarProjectile : MonoBehaviour
{
    public float speed = 15f; //星形弾のスピード

    void Start()
    {
        //プレイヤーの向きを検索して格納
        GameObject player = GameObject.Find("Player");
        float direction = Math.Sign(player.transform.localScale.x);

        //向いている方向に飛ばす
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = new Vector2(speed * direction, 0);

        //2秒後に自動で消える
        Destroy(gameObject, 2f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Boss"))
        {
            HP bossHealth = other.GetComponent<HP>();

            if(bossHealth != null)
            {
                bossHealth.TakeDamage(1);
            }

            Destroy(gameObject);
        }
    }
}
