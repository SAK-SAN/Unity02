using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController   :   MonoBehaviour
{
    [Header("移動・ジャンプ設定")]  //Unity上で見出しを表示
    public float moveSpeed = 5f;
    public float jumpForce = 3f;
    //public float hoverGravityScale = 0.5f;  //ホバリング中の重力
    private float defaultGravityScale;

    [Header("吸い込み・吐き出し設定")]
    public GameObject inhaleArea;   //吸い込み判定
    public GameObject startProjectilePrefab;    //吐き出す星形弾
    public Transform spitPoint; //星形弾の位置

    //状態管理
    public enum PlayerState {Normal, Inhaling, Full, Damaged}
    public PlayerState currentState = PlayerState.Normal;

    private Rigidbody2D rb;     //プレイヤーの重力や速度を操作する
    private float moveInput;    //プレイヤーの左右入力を覚えておく

    private int jumpCounter = 0;    //ジャンプ回数を記録する変数

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
        inhaleArea.SetActive(false);
    }

    void Update()
    {
        switch(currentState)
        {
            case PlayerState.Normal:    //通常時
                HandleMovement();   //移動の処理
                HandleJump(); //ジャンプの処理
                if(Keyboard.current.zKey.wasPressedThisFrame) //Z入力時
                {
                    StartInhaling();    //吸い込み開始の処理
                }
                break;

            case PlayerState.Inhaling:  //吸い込み時
                rb.linearVelocity = new Vector2(0, rb.linearVelocityY);
                if(Keyboard.current.zKey.wasReleasedThisFrame)   //Zを離した時
                {
                    StopInhaling(); //吸い込み終了の処理
                }
                break;

            case PlayerState.Full:  //ほおばり時
                HandleMovement();   //移動の処理
                HandleJump(); //ジャンプの処理
                if(Keyboard.current.zKey.wasPressedThisFrame) //Z入力時
                {
                    SpitOut();    //吐き出しの処理
                }
            break;
        }
    }

    void FixedUpdate()  //衝突や移動の演算時に一定のタイミングで呼ばれるらしい
    {
        if((currentState != PlayerState.Inhaling) && (currentState != PlayerState.Damaged))
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocityY);
        }
    }

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
        if((jumpCounter <= 2) && (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame))
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

    public void Swallow()   //吸い込み成功時?
    {
        currentState = PlayerState.Full;    //ほうばり中に状態を変更
        inhaleArea.SetActive(false);    //吸い込み判定をオフ
        transform.localScale = new Vector3(transform.localScale.x * 1.5f, 1.5f, 1);   //見た目を太らせる処理
    }

    void SpitOut()  //吐き出し処理
    {
        currentState = PlayerState.Normal;  //通常に状態を変更

        if(startProjectilePrefab != null)   //星型弾の処理がunityで存在すれば
        {
            //指定した位置に星型弾を生成
            Instantiate(startProjectilePrefab, spitPoint.position, transform.rotation);
        }

        transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x), 1, 1);   //見た目を通常に戻す
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
}
