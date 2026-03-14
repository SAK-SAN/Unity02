using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using NUnit.Framework.Internal;   //時間差処理用
using UnityEngine.UI;

public class HP : MonoBehaviour
{
    [Header("体力設定")]
    public int maxHP = 3;
    private int currentHP;

    [Header("UI設定")]
    public Slider hpSlider; //HPゲージを割り当てる

    [Header("無敵時間の設定")]
    public float invincibleTime = 1.5f; //ダメージ後の無敵時間
    public bool isInvincible = false;   //今無敵かどうか

    private SpriteRenderer sr;
    private Color originalColor;    //元の色を保持

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHP = maxHP;
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;

        if(hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
    }

    public void TakeDamage(int damage)
    {
        if(isInvincible)
        {
            return;
        }
        
        PlayerController player = GetComponent<PlayerController>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if(player != null)  //バグ対策(パリィ中にボスと重なっていると浮遊するバグ&パネル選択中にダメージを受けるとパネルが閉じないバグ)
        {
            player.CloseSpellMenu();
            if(rb.gravityScale != player.defaultGravityScale)
            {
                rb.gravityScale = player.defaultGravityScale;
            }
        }
        
        
        currentHP -= damage;

        //UIを更新
        if(hpSlider != null)
        {
            hpSlider.value = currentHP;
        }

        Debug.Log(gameObject.name + "に" + damage + "ダメージ! 残りHP" + currentHP);

        if(currentHP <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(DamageRoutine());
        }
    }

    private IEnumerator DamageRoutine()
    {
        isInvincible = true;

        int playerLayer = LayerMask.NameToLayer("Player");
        int bossLayer = LayerMask.NameToLayer("Boss");
        if(gameObject.CompareTag("Player"))
            Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, true);

        float timer = 0;
        while(timer < invincibleTime)
        {
            /***
            new Color(R, G, B, A)
            で保持されているらしい
            R: Redで0.0~1.0のfloat型
            G: Greenで0.0~1.0のfloat型
            B: Blueで0.0~1.0のfloat型
            A: Alphaで透明度を表す0.0~1.0のfloat型
            ***/
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f); //半透明にする
            yield return new WaitForSeconds(0.1f);
            sr.color = originalColor;   //元の色にする
            yield return new WaitForSeconds(0.1f);
            timer += 0.2f;
        }

        if(gameObject.CompareTag("Player"))
            Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, false);
        isInvincible = false; //無敵終了
    }

    public void Heal(int amount)
    {
        currentHP += amount;
        if(currentHP > maxHP)
            currentHP = maxHP;
        if(hpSlider != null)
        {
            hpSlider.value = currentHP;
        }
    }

    public void Die()
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();

        if(gameObject.name == "Player")
        {
            if(gm != null)
                gm.ShowGameOver();
        }
        else if(gameObject.name == "Boss")
        {
            if(gm != null)
                gm.ShowGameClear();
        }
        Debug.Log(gameObject.name + "は倒れた！");
        gameObject.SetActive(false);
    }
}
