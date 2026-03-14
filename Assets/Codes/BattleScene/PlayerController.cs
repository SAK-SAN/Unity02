using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using Unity.VisualScripting;

public class PlayerController   :   MonoBehaviour
{
    [Header("ステータス設定")]  //Unity上で見出しを表示
    public float moveSpeed = 5f;
    public float jumpForce = 3f;
    public float ParryTime = 0.3f;
    //public float hoverGravityScale = 0.5f;  //ホバリング中の重力
    private float defaultMoveSpeed;
    private float defaultJumpForce;
    public float defaultGravityScale;

    [Header("吸い込み・吐き出し設定")]
    public GameObject inhaleArea;   //吸い込み判定
    public int maxStarStock = 5;    //星型弾の最大ストック数
    public int currentStarStock = 0;    //現在の星型弾のストック数
    public GameObject startProjectilePrefab;    //吐き出す星形弾
    public Transform spitPoint; //星形弾の位置
    
    [Header("UI設定")]
    public TextMeshProUGUI stockText;   
    public GameObject spellUI;  //呪文選択画面のパネル
    public TextMeshProUGUI[] spellTexts;    //呪文のテキスト入れ
    public Color selectedColor = Color.yellow;  //選択中の文字色
    public Color normalColor = Color.white;  //通常時の色

    [Header("呪文パラメータ")]
    public float selectTime = 0.2f;
    public int healAmount = 1;
    public float buffDuration = 5f; //バフの継続時間
    public float speedBuffMultiplier = 1.5f;    //速度バフの倍率
    public float jumpBuffMultiplier = 1.5f; //ジャンプバフの倍率
    public float gravityReduction = 0.3f;   //重力の緩和時の値
    public int attackCost = 1;   //攻撃のコスト
    public int speedCost = 1;
    public int jumpCost = 1;
    public int gravityCost = 1;
    public int healCost = 1;
    public int InvincibleCost = 3;

    [Header("バフ演出")]
    public float blinkInterval = 0.1f;
    private int activeBuffCount = 0;
    private Coroutine blinkCoroutine;

    //状態管理
    public enum PlayerState {Normal, Inhaling, Damaged, Parry, Selecting}
    public PlayerState currentState = PlayerState.Normal;

    private Rigidbody2D rb;     //プレイヤーの重力や速度を操作する
    private float moveInput;    //プレイヤーの左右入力を覚えておく

    private int jumpCounter = 0;    //ジャンプ回数を記録する変数
    private int currentSpellIndex = 0;  //現在選んでいる呪文の番号

    private SpriteRenderer sr;
    private Color originalColor;    //オリジナルの色を保持

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
        defaultMoveSpeed = moveSpeed;
        defaultJumpForce = jumpForce;

        inhaleArea.SetActive(false);
        spellUI.SetActive(false);

        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;

        UpdateStockUI();
    }

    void Update()
    {

        if(Keyboard.current.cKey.wasPressedThisFrame)
        {
            if(currentState == PlayerState.Normal)
                OpenSpellMenu();
            else if(currentState == PlayerState.Selecting)
                CloseSpellMenu();
        }
        else if(Keyboard.current.xKey.wasPressedThisFrame)
        {
            if(currentState == PlayerState.Selecting)
                CloseSpellMenu();
        }

        switch(currentState)
        {
            case PlayerState.Normal:    //通常時
                HandleMovement();   //移動の処理
                HandleJump(); //ジャンプの処理
                if(Keyboard.current.zKey.wasPressedThisFrame 
                || Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.eKey.wasPressedThisFrame) //Z入力時
                {
                    StartInhaling();    //吸い込み開始の処理
                }
                else if(Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
                {
                    startParry();
                }
                break;

            case PlayerState.Inhaling:  //吸い込み時
                rb.linearVelocity = new Vector2(0, rb.linearVelocityY);
                if(Keyboard.current.zKey.wasReleasedThisFrame 
                || Keyboard.current.enterKey.wasReleasedThisFrame
                || Keyboard.current.eKey.wasReleasedThisFrame)   //Zを離した時
                {
                    StopInhaling(); //吸い込み終了の処理
                }
                break;

            case PlayerState.Selecting: //呪文選択中
                rb.linearVelocity = new Vector2(0, rb.linearVelocityY);
                HandleSpellSelection();
                break;
        }
    }

    void FixedUpdate()  //衝突や移動の演算時に一定のタイミングで呼ばれるらしい
    {
        if(currentState == PlayerState.Normal)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocityY);
        }
    }

    void OpenSpellMenu()
    {
        currentState = PlayerState.Selecting;
        spellUI.SetActive(true);    //パネルの表示
        currentSpellIndex = 0;  //パネルの一番上に
        UpdateSpellTextColor(); //文字色の更新
        Time.timeScale = selectTime;  //スローモーション処理

        float direction = Mathf.Sign(transform.localScale.x);

        Vector3 offset = new Vector3(direction * 2.0f, 2.5f, 0);
        Vector3 targetWorldPos = transform.position + offset;

        if(Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(targetWorldPos);
            spellUI.GetComponent<RectTransform>().position = screenPos;
        }
    }

    public void CloseSpellMenu()
    {
        currentState = PlayerState.Normal;
        spellUI.SetActive(false);
        Time.timeScale = 1f;
    }

    void HandleSpellSelection() //パネルの操作
    {
        if(Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
        {
            currentSpellIndex++;
            if(currentSpellIndex > 5)
                currentSpellIndex = 0;
            UpdateSpellTextColor();
        }
        else if(Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
        {
            currentSpellIndex--;
            if(currentSpellIndex < 0)
                currentSpellIndex = 5;
            UpdateSpellTextColor();
        }

        if(Keyboard.current.zKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
        {
            ExecuteSpell(currentSpellIndex);
        }
    }

    void UpdateSpellTextColor()
    {
        for(int i=0; i < spellTexts.Length; i++)
        {
            if(i == currentSpellIndex)
                spellTexts[i].color = selectedColor;
            else
                spellTexts[i].color = normalColor;
        }
    }

    void ExecuteSpell(int index) //呪文の実行処理
    {
        switch(index)
        {
            case 0:
                if(currentStarStock >= attackCost)
                {
                    currentStarStock -= attackCost;
                    Instantiate(startProjectilePrefab, spitPoint.position, transform.rotation);
                    CloseSpellMenu();
                }
                break;
            case 1:
                if(currentStarStock >= speedCost)
                {
                    currentStarStock -= speedCost;
                    StartCoroutine(SpeedBuffRoutine());
                    CloseSpellMenu();
                }
                break;
            case 2:
                if(currentStarStock >= healCost)
                {
                    currentStarStock -= healCost;
                    GetComponent<HP>().Heal(healAmount);
                    CloseSpellMenu();
                }
                break;
            case 3:
                if(currentStarStock >= InvincibleCost)
                {
                    currentStarStock -= InvincibleCost;
                    StartCoroutine(InvincibleBuffRoutine());
                    CloseSpellMenu();
                }
                break;
            case 4:
                if(currentStarStock >= jumpCost)
                {
                    currentStarStock -= jumpCost;
                    StartCoroutine(JumpBuffRoutine());
                    CloseSpellMenu();
                }
                break;
            case 5:
                if(currentStarStock >= gravityCost)
                {
                    currentStarStock -= gravityCost;
                    StartCoroutine(FloatBuffRoutine());
                    CloseSpellMenu();
                }
                break;
        }
        UpdateStockUI();
    }

//ここからバフの処理
    IEnumerator SpeedBuffRoutine()
    {
        StartBuffBlink();
        moveSpeed = defaultMoveSpeed * speedBuffMultiplier;
        yield return new WaitForSecondsRealtime(buffDuration);
        moveSpeed = defaultMoveSpeed;
        StopBuffBlink();
    }

    IEnumerator JumpBuffRoutine()
    {
        StartBuffBlink();
        jumpForce = defaultJumpForce * jumpBuffMultiplier;
        rb.gravityScale *= jumpBuffMultiplier;
        yield return new WaitForSecondsRealtime(buffDuration);
        jumpForce = defaultJumpForce;
        rb.gravityScale = defaultGravityScale;
        StopBuffBlink();
    }

    IEnumerator FloatBuffRoutine()
    {
        StartBuffBlink();
        rb.gravityScale = gravityReduction;
        yield return new WaitForSecondsRealtime(buffDuration);
        rb.gravityScale = defaultGravityScale;
        StopBuffBlink();
    }

    IEnumerator InvincibleBuffRoutine()
    {
        HP myHealth = GetComponent<HP>();
        myHealth.isInvincible = true;
        
        int playerLayer = LayerMask.NameToLayer("Player");
        int bossLayer = LayerMask.NameToLayer("Boss");
        Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, true);

        GetComponent<SpriteRenderer>().color = Color.yellow;

        yield return new WaitForSecondsRealtime(buffDuration);

        Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, false);
        myHealth.isInvincible = false;
        currentState = PlayerState.Normal;
        GetComponent<SpriteRenderer>().color = Color.white;
    }

    IEnumerator BlinkRoutine()
    {
        while(true)
        {
            sr.enabled  = !sr.enabled;
            yield return new WaitForSecondsRealtime(blinkInterval);
        }
    }

    void StartBuffBlink()
    {
        activeBuffCount++;
        if(activeBuffCount == 1)
        {
            if(blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);
            
            blinkCoroutine = StartCoroutine(BlinkRoutine());
        }
    }

    void StopBuffBlink()
    {
        activeBuffCount--;
        if(activeBuffCount <= 0)
        {
            activeBuffCount = 0;
            if(blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);

                sr.enabled = true;
        }
    }
//ここまでバフの処理
    void HandleMovement()   //移動処理
    {
        //moveInput = Input.GetAxisRaw("Horizontal"); //左右or"A"or"D"の入力を-1,1,0(入力なし)で格納
        moveInput = 0f;
        if(Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
        {
            moveInput = 1f;
        }
        if(Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
        {
            moveInput = -1f;
        }


        float currentSizeX = Mathf.Abs(transform.localScale.x);
        float currentSizeY = Mathf.Abs(transform.localScale.y);

        if(moveInput > 0)
        {
            transform.localScale = new Vector3(currentSizeX, currentSizeY, 1);    //右入力時にキャラクターの向きを右にする
        }
        else if(moveInput < 0)
        {
            transform.localScale = new Vector3(-currentSizeX, currentSizeY, 1);   //左入力時にキャラクターの向きを左にする
        }

    }

    void HandleJump()   //ジャンプ処理
    {
        if((jumpCounter <= 2) 
        && (Keyboard.current.upArrowKey.wasPressedThisFrame 
        || Keyboard.current.wKey.wasPressedThisFrame
        || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            jumpCounter++;
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce); //スペースキー入力時に上方向にjump方向加速
            //rb.gravityScale = hoverGravityScale;    //ジャンプ入力時に重力が軽くなりふわふわする
        }

    
        /*if(rb.linearVelocityY == 0)
        {
            jumpCounter = 0;
            //rb.gravityScale = defaultGravityScale;  //上下方向の加速度がないときデフォルトの重力にする
        }*/
    }

    void StartInhaling()    //吸い込み開始時
    {
        currentState = PlayerState.Inhaling;    //吸い込み中に状態を変更
        inhaleArea.SetActive(true); //吸い込み判定をオン
    }

    void StopInhaling() //吸い込み停止時
    {
        currentState = PlayerState.Normal;  //通常に状態を変更
        inhaleArea.SetActive(false);    //吸い込み判定をオフ
    }

    public void Swallow()   //吸い込み成功時
    {
        inhaleArea.SetActive(false);    //吸い込み判定をオフ
        
        if(currentStarStock <= maxStarStock)
        {
            currentStarStock++;
            UpdateStockUI();
        }

        currentState = PlayerState.Normal;
    }

    /*void SpitOut()  //吐き出し処理
    {
        currentState = PlayerState.Normal;  //通常に状態を変更

        if((startProjectilePrefab != null) && currentStarStock >= 1)   //星型弾の処理がunityで存在すれば
        {
            //指定した位置に星型弾を生成
            Instantiate(startProjectilePrefab, spitPoint.position, transform.rotation);
            currentStarStock--;
            UpdateStockUI();
        }

        //transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x), 1, 1);   //見た目を通常に戻す
    }*/

    void startParry()   //パリィ開始処理
    {
        if(currentStarStock >= 1)
        {
            currentState = PlayerState.Parry;
            currentStarStock--;
            UpdateStockUI();

            //パリィの見た目
            sr.color = Color.cyan;

            int playerLayer = LayerMask.NameToLayer("Player");
            int bossLayer = LayerMask.NameToLayer("Boss");
            Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, true);

            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;

            //パリィの持続時間
            Invoke("EndParry", ParryTime);
        }
    }

    void EndParry() //パリィ終了処理
    {
        if(currentState == PlayerState.Parry)
        {
            currentState = PlayerState.Normal;
            sr.color = originalColor;

            int playerLayer = LayerMask.NameToLayer("Player");
            int bossLayer = LayerMask.NameToLayer("Boss");
            Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, false);

            rb.gravityScale = defaultGravityScale;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //ジャンプの回数制限の処理
        if(collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if(collision.contacts[0].normal.y > 0.5f)
            {
                jumpCounter = 0;
            }
        }

        //ボスに衝突したときの処理
        if(collision.gameObject.layer == LayerMask.NameToLayer("Boss"))
        {
            HP myHealth = GetComponent<HP>();

            if((myHealth != null) && !myHealth.isInvincible)
            {
                myHealth.TakeDamage(1);
                ApplyKnockback(collision.transform.position);   //ノックバック発生
            }
        }
    }

    void ApplyKnockback(Vector3 bossPosition)
    {
        currentState = PlayerState.Damaged; //状態をダメージ中にする
        inhaleArea.SetActive(false);    //吸い込み中ならキャンセル

        //ボスの位置の逆を計算
        float konockbackDir = Mathf.Sign(transform.position.x - bossPosition.x);

        //今の速度をゼロにしてから、斜め上に弾き飛ばす
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(konockbackDir * 7f, 7f), ForceMode2D.Impulse);

        //0.5秒後に操作可能に戻すタイマーを起動
        Invoke("RecoverFromDamage", 0.5f);
    }

    void RecoverFromDamage()
    {
        if(GetComponent<HP>().isInvincible)
        {
            currentState = PlayerState.Normal;
        }
    }

    void UpdateStockUI()
    {
        if(stockText != null)
        {
            stockText.text = "";
            for(int i=0; i<currentStarStock; i++)
            {
                stockText.text += "★";
            }
            for(int i=0; i<(maxStarStock - currentStarStock); i++)
            {
                stockText.text += "☆";
            }
        }
    }
}
