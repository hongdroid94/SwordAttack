using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyController : MonoBehaviour
{
    public enum EEnemyState { IDLE, MOVE, ATTACK, DIE };
    [Serializable]
    public class EnemyInfo
    {
        public EEnemyState eEnemyState;
        public float interval;
        public Sprite[] sprites;
    }
    
    [SerializeField] EnemyInfo[] enemyInfos;
    [SerializeField] EEnemyState _eEnemyState;
    [SerializeField] float playerDetectDistance;
    [SerializeField] float groundDistance;
    [SerializeField] float moveSpeed;
    [SerializeField] float wallDistance;
    [SerializeField] int attackSpriteIndex;
    [SerializeField] bool isProjectileAttack;

    [Header("Attack")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform projectilePos;
    [SerializeField] Vector2 attackSize;
    [SerializeField] Transform groundPos;

    [Header("Knockback")]
    // 플레이어에게 맞았을 때 밀려나는 세기. Rigidbody linearDrag(2)로 서서히 감쇠한다.
    [SerializeField] float knockbackPower = 4f;
    // 밀려나는 동안 이동 AI를 멈추는 시간.
    [SerializeField] float knockbackTime = 0.15f;

    Rigidbody2D rbody;
    SpriteRenderer spriteRenderer;
    Dictionary<float, WaitForSeconds> waitDic;
    bool isAttack;
    bool isDie;
    bool isRight;
    bool isKnockback;
    int knockbackDir;
    int nextMove;
    int tileLayer;
    int playerLayer;
    bool initialized;

    int rightValue => isRight ? 1 : -1;


    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rbody = GetComponent<Rigidbody2D>();
        waitDic = new();
        tileLayer = LayerMask.GetMask("Tile");
        playerLayer = LayerMask.GetMask("Player");
        initialized = true;

        StartCoroutine(nameof(EnemyAnimCo));
        Invoke("Think", 5);

		GetComponent<Damaged>().Defect += Defect;
    }

    void OnEnable()
    {
        // GameObject를 껐다 켜면 코루틴과 Invoke가 모두 취소되고 저절로 되살아나지 않는다.
        // Start는 한 번만 돌기 때문에 애니메이션(EnemyAnimCo)과 AI(Think)가 영영 멈춘 채
        // FixedUpdate만 계속 돌아서 "스프라이트가 멈춘 채 미끄러지는" 상태가 된다.
        // 첫 활성화 때는 OnEnable이 Start보다 먼저 도므로 Start에 맡긴다.
        if (!initialized || isDie) return;

        StartCoroutine(nameof(EnemyAnimCo));
        Invoke("Think", 2);
    }

	void OnDestroy()
	{
        GetComponent<Damaged>().Defect -= Defect;
    }

	WaitForSeconds Wait(float seconds)
    {
        if (!waitDic.ContainsKey(seconds))
            waitDic.Add(seconds, new WaitForSeconds(seconds));
        return waitDic[seconds];
    }

    IEnumerator EnemyAnimCo() 
    {
        while (true) 
        {
            EnemyInfo curEnemyInfo = Array.Find(enemyInfos, x => x.eEnemyState == _eEnemyState);
            if (curEnemyInfo == null)
            {
                // yield 없이 continue 하면 이 while이 한 프레임 안에서 무한히 돌아 에디터가 멈춘다.
                yield return null;
                continue;
            }

            for (int i = 0; i < curEnemyInfo.sprites.Length; i++)
			{
                if (curEnemyInfo.eEnemyState != _eEnemyState) break;
                spriteRenderer.sprite = curEnemyInfo.sprites[i];
                if (curEnemyInfo.eEnemyState == EEnemyState.ATTACK && attackSpriteIndex == i) 
                {
                    CheckAttack();
                }
                yield return Wait(curEnemyInfo.interval);
            }
            if (curEnemyInfo.eEnemyState == EEnemyState.ATTACK) 
            {
                isAttack = false;
            }
		}
    }

    void FixedUpdate()
    {
        if (isDie) return;

        // 넉백 중에는 이동 AI가 속도를 덮어쓰지 않게 둔다. drag로 알아서 감쇠한다.
        if (isKnockback)
        {
            // 적 콜라이더는 트리거라 지형과 물리 충돌하지 않는다. 그냥 밀면 벽을 뚫는다.
            // 넉백 방향에 벽이 있으면 직접 멈춰 세운다.
            Vector2 wallOrigin = groundPos.position + Vector3.up * 0.5f;
            if (Physics2D.Raycast(wallOrigin, Vector2.right * knockbackDir, wallDistance, tileLayer))
            {
                rbody.linearVelocity = Vector2.zero;
                isKnockback = false;
            }
            return;
        }

        // ������
        if (!isAttack)
        {
            rbody.linearVelocity = new Vector2(nextMove * moveSpeed, rbody.linearVelocity.y);
        }
       
        // �ٴ� üũ
        Vector2 frontVec = new Vector2(rbody.position.x + nextMove * 0.3f, rbody.position.y);
        Debug.DrawRay(frontVec, Vector3.down * groundDistance, new Color(0, 1, 0));
        RaycastHit2D rayHit = Physics2D.Raycast(frontVec, Vector3.down, groundDistance, tileLayer);
        if (rayHit.collider == null) 
        {
            Turn();
        }
            
        // �÷��̾� ������ ����
        isRight = !spriteRenderer.flipX;
        Debug.DrawRay(rbody.position, Vector3.right * rightValue * playerDetectDistance, new Color(0, 1, 0));
        var rayHitPlayer = Physics2D.Raycast(rbody.position, Vector3.right * rightValue, playerDetectDistance, playerLayer);
        if (rayHitPlayer.collider != null && !isAttack) 
        {
            isAttack = true;
            _eEnemyState = EEnemyState.ATTACK;
        }

        // �� üũ 
        Debug.DrawRay(groundPos.position + Vector3.up * 0.5f, Vector3.right * rightValue * wallDistance, new Color(1, 1, 0));
        RaycastHit2D rayWallHit = Physics2D.Raycast(groundPos.position + Vector3.up * 0.5f, Vector3.right * rightValue * wallDistance, wallDistance, tileLayer);
        if (rayWallHit.collider != null) 
        {
            Turn();
        }
        
    }


    void Think()
    {
        if (isDie) return;
        if (!isAttack) 
        {
            nextMove = Random.Range(-1, 2);
            _eEnemyState = nextMove == 0 ? EEnemyState.IDLE : EEnemyState.MOVE;

            if (nextMove != 0)
            {
                spriteRenderer.flipX = nextMove == -1;
            }
        }
        
        Invoke("Think", Random.Range(2f, 5f));
    }

    void Turn()
    {
        nextMove *= -1;
        spriteRenderer.flipX = nextMove == -1;

        CancelInvoke("Think");
        Invoke("Think", 2);
    }

    void CheckAttack() 
    {
        SoundManager.Instance.PlaySFXSound("Monster_Attack");

        if (isProjectileAttack)
        {
            Instantiate(projectilePrefab, projectilePos.position, Quaternion.identity).GetComponent<Bullet>().Init(rightValue);
        }
        else 
        {
            Vector2 originPos = new Vector2(transform.position.x + projectilePos.localPosition.x * rightValue, projectilePos.localPosition.y);
            var players = Physics2D.BoxCastAll(originPos, attackSize, 0, Vector2.right * rightValue, 1, playerLayer);
            if (players != null) 
            {
				for (int i = 0; i < players.Length; i++)
				{
                    int damageDir = (players[i].transform.position - projectilePos.position).x < 0 ? 1 : -1;
                    players[i].collider.GetComponent<Damaged>().Damage(1, damageDir);
                }
            }
        }
    }

    void Defect(bool isDie, int health, int damageDir)
    {
        SoundManager.Instance.PlaySFXSound("Monster_Damage");

        if (isDie)
        {
            this.isDie = true;
            _eEnemyState = EEnemyState.DIE;
            var dieEnemyInfo = Array.Find(enemyInfos, x => x.eEnemyState == EEnemyState.DIE);
            float dieTime = dieEnemyInfo.interval * dieEnemyInfo.sprites.Length;
            GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject, dieTime);
        }
        else if (damageDir != 0)
        {
            // 죽지 않았으면 맞은 방향으로 밀려난다.
            StopCoroutine(nameof(KnockbackCo));
            StartCoroutine(KnockbackCo(damageDir));
        }
    }

    IEnumerator KnockbackCo(int dir)
    {
        isKnockback = true;
        knockbackDir = dir;
        // Y는 고정(FreezePositionY)이라 수평 속도만 준다. drag가 서서히 줄인다.
        rbody.linearVelocity = new Vector2(dir * knockbackPower, 0f);
        yield return Wait(knockbackTime);
        isKnockback = false;
    }
}
