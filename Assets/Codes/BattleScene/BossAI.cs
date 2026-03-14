using System.Collections;
using Unity.VisualScripting;

//using UnityEditorInternal;
using UnityEngine;

public class BossAI :MonoBehaviour
{
    [Header("ステータス設定")]
    public float moveSpeed = 2f;    //歩くスピ－ド
    public float jumpForce = 12f;   //ジャンプ力
    public float tackleSpeed = 10f; //突進
    public float idleTime = 1.0f;  //待機時間
    public float WalkTime = 3.0f;  //歩きの持続時間
    public float JumpTime = 0.5f;  //ジャンプまでの猶予時間
    public float tackleChargeTime = 0.7f; //突進の予備動作時間
    

    [Header("ギミック設定")]
    public GameObject starItemPrefab;   //吸い込み用アイテム
    public Transform leftStarPoint; //左の星
    public Transform rightStarPoint;    //右の星

    [Header("ビーム攻撃の設定")]
    public float beamChargeTime = 2.0f; //予備動作時間
    public float beamFireTime = 1.0f;   //打ち続ける時間
    public GameObject beamPrefab;
    public Transform beamPoint; //ビームの発射位置
    public float starSpawnInterval = 0.4f;  //星を出す間隔


    //ボスの状態(待機、歩き、ジャンプ)
    public enum BossState{Idle, Walk, Jump, TackleCharge, TackleDash, BeamCharge, BeamFire}
    public BossState currentState = BossState.Idle;

    private Rigidbody2D rb;
    private Transform player; //プレイヤーの位置情報保管場所
    private SpriteRenderer sr;
    private Color originalColor;    //オリジナルの色を保持
    private Coroutine chargecCoroutine; //タックル予備動作コルーチン
    private Coroutine beamChargeCoroutine;  //ビーム予備動作コルーチン
    private float stateTimer; //行動を切り替える残り時間
    private float dashDirection;    //突進の方向を保持する変数
    

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;

        //画面内のplayerを探す
        GameObject playerObj = GameObject.Find("Player");
        if(playerObj != null)
        {
            player = playerObj.transform;
        }

        ChangeState(BossState.Walk);
    }

    void Update()
    {
        //毎フレームタイマーを減らす
        stateTimer -= Time.deltaTime;

        switch(currentState)
        {
            case BossState.Idle:    //待機中
                rb.linearVelocity = new Vector2(0, rb.linearVelocityY); //止まる
                if(stateTimer <= 0)
                {
                    ChooseNextState();
                }
                break;

            case BossState.Walk:    //歩き中
                WalkTowardsPlayer();
                if(stateTimer <= 0)
                {
                    ChangeState(BossState.Idle);
                    
                    //ChooseNextState();
                }
                break;

            case BossState.Jump:    //ジャンプ中
            //ここでジャンプの関数を入れると関数が呼ばれ続けて飛んでしまう
                /*if(stateTimer <= 0 && Mathf.Abs(rb.linearVelocityY) < 0.01f)    //着地の判定
                {
                    LandingImpact();    //星を出す
                    ChangeState(BossState.Idle);
                }*/
                break;

            case BossState.TackleCharge:
                if(player != null)
                {
                    //プレイヤーの逆を検知
                    float awayDir = -Mathf.Sign(player.position.x - transform.position.x);

                    //予備動作部分
                    //ChargeRoutine();
                    transform.localScale = new Vector3(-awayDir * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
                    rb.linearVelocity = new Vector2(awayDir * (moveSpeed * 0.5f), rb.linearVelocityY);
                }

                if(stateTimer <= 0)
                {
                    ChangeState(BossState.TackleDash);
                }
                break;

            case BossState.TackleDash:
               rb.linearVelocity = new Vector2(dashDirection * tackleSpeed, rb.linearVelocityY);

                //保険の処理
                if(stateTimer <= 0)
                {
                    ChangeState(BossState.Idle);
                }
                break;

            case BossState.BeamCharge:
                rb.linearVelocity = new Vector2(0, rb.linearVelocityY);
                if(stateTimer <= 0)
                    ChangeState(BossState.BeamFire);
                break;
            
            case BossState.BeamFire:
                rb.linearVelocity = new Vector2(0, rb.linearVelocityY);
                if(stateTimer <= 0)
                    ChangeState(BossState.Idle);
                break;
        }
    }

    void ChooseNextState()
    {   
        //全ステータス数を取得
        int stateCount = System.Enum.GetValues(typeof(BossAI.BossState)).Length;
        int chargeStateCount = 0;
        string targetWord = "Charge";
        //全てのステータス名を取得
        string[] stateNames = System.Enum.GetNames(typeof(BossAI.BossState));
        for(int i=0; i < stateNames.Length; i++)
        {
            if(stateNames[i].Contains(targetWord))
                chargeStateCount++;
        }


        int rand = Random.Range(0, stateCount - chargeStateCount);
        
        switch(rand)
        {
            case 0:
                ChangeState(BossState.Walk);
                break;
            case 1:
                ChangeState(BossState.Jump);
                break;
            case 2:
                ChangeState(BossState.TackleCharge);
                break;
            case 3:
                ChangeState(BossState.BeamCharge);
                break;
        }
    }

    void ChangeState(BossState newState)
    {
        if(chargecCoroutine != null)
            StopCoroutine(chargecCoroutine);
        if(beamChargeCoroutine != null)
            StopCoroutine(beamChargeCoroutine);

        sr.color = originalColor;

        currentState = newState;

        switch(newState)
        {
            case BossState.Idle:
                stateTimer = idleTime;  //1秒間待機
                break;

            case BossState.Walk:
                stateTimer = WalkTime;  //3秒間歩き続ける
                break;

            case BossState.Jump:
                stateTimer = JumpTime;  //飛ぶまでの猶予時間
                JumpTowardsPlayer();    //上方向に加速度を与える
                break;

            case BossState.TackleCharge:
                //予備動作時間
                stateTimer = tackleChargeTime;
                chargecCoroutine = StartCoroutine(ChargeRoutine());
                break;

            case BossState.TackleDash:
                stateTimer = 5.0f; //壁にぶつからなかったときの保険

                //プレイヤーがいる方向を固定する
                if(player != null)
                {
                    dashDirection = Mathf.Sign(player.position.x - transform.position.x);
                    transform.localScale = new Vector3(dashDirection * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
                }
                break;

            case BossState.BeamCharge:
                stateTimer = beamChargeTime;
                if(player != null)
                {
                    float dir = Mathf.Sign(player.position.x - transform.position.x);
                    transform.localScale = new Vector3(dir * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
                }
                beamChargeCoroutine = StartCoroutine(BeamChargeCoroutine());
                break;

            case BossState.BeamFire:
                stateTimer = beamFireTime;
                if((beamPrefab != null) && (beamPoint != null))
                {
                    Instantiate(beamPrefab, beamPoint.position, transform.rotation);
                }
                break;
            

        }
        
    }

    void WalkTowardsPlayer()
    {
        if(player == null)
            return;
        
        //プレイヤーがいる方向を検知(右：１,左：-1)
        float direction = Mathf.Sign(player.position.x - transform.position.x);

        //プレイヤー方向を向く
        transform.localScale = new Vector3(direction * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);

        //プレイヤー方向へ歩く
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocityY);
    }

    void JumpTowardsPlayer()
    {
        if(player == null)
            return;

        float direction = Mathf.Sign(player.position.x - transform.position.x);
        transform.localScale = new Vector3(direction * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);

        rb.linearVelocity = new Vector2(direction * (moveSpeed * 1.5f), jumpForce);
    }

    void LandingImpact()
    {
        if(starItemPrefab != null)
        {
            Instantiate(starItemPrefab, leftStarPoint.position, Quaternion.identity);
            Instantiate(starItemPrefab, rightStarPoint.position, Quaternion.identity);
        }
    }

    private IEnumerator ChargeRoutine()
    {
        while(stateTimer > 0)
        {
            sr.color = new Color(1f, 1f, 1f, originalColor.a); //白にする
            yield return new WaitForSeconds(0.1f);
            sr.color = originalColor;   //元の色にする
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator BeamChargeCoroutine()
    {
        while(stateTimer > 0)
        {
            if(starItemPrefab != null)
            {
                //星をボスの頭上にだす
                Vector3 spawnPos = transform.position + new Vector3(0, 0.1f, 0);
                GameObject star = Instantiate(starItemPrefab, spawnPos, Quaternion.identity);

                //星を山なりにランダムに散らす
                Rigidbody2D starRb = star.GetComponent<Rigidbody2D>();
                if(starRb != null)
                {
                    float randomX = Random.Range(-4f, 4f);
                    starRb.AddForce(new Vector2(randomX, 5f), ForceMode2D.Impulse);
                }
            }

            yield return new WaitForSeconds(starSpawnInterval);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //ジャンプの着地処理
        if(currentState == BossState.Jump && collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if(collision.contacts[0].normal.y > 0.5f)
            {
                LandingImpact();
                int rand = Random.Range(0, 11);
                    if(rand > 3)
                    {
                        ChangeState(BossState.Idle);
                    }
                    else
                    {
                        ChangeState(BossState.TackleCharge);
                    }
            }
        }

        //突進の処理
        if(currentState == BossState.TackleDash && collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if(Mathf.Abs(collision.contacts[0].normal.x) > 0.5f)
            {
                LandingImpact();
                ChangeState(BossState.Idle);
            }
        }
    }
}
