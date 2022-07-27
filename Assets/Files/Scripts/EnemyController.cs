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
    [SerializeField] int attackSpriteIndex;
    [SerializeField] bool isProjectileAttack;

    [Header("Attack")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform projectilePos;
    [SerializeField] Vector2 attackSize;

    Rigidbody2D rbody;
    SpriteRenderer spriteRenderer;
    Dictionary<float, WaitForSeconds> waitDic;
    bool isAttack;
    bool isDie;
    bool isRight;
    int nextMove;
    int tileLayer;
    int playerLayer;
    
    int rightValue => isRight ? 1 : -1;


    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rbody = GetComponent<Rigidbody2D>();
        waitDic = new();
        tileLayer = LayerMask.GetMask("Tile");
        playerLayer = LayerMask.GetMask("Player");

        StartCoroutine(nameof(EnemyAnimCo));
        Invoke("Think", 5);

		GetComponent<Damaged>().Defect += Defect;
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
            if (curEnemyInfo == null) continue;

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

        // 움직임
        if (!isAttack) 
        {
            rbody.velocity = new Vector2(nextMove * moveSpeed, rbody.velocity.y);
        }
       
        // 바닥 체크
        Vector2 frontVec = new Vector2(rbody.position.x + nextMove * 0.3f, rbody.position.y);
        Debug.DrawRay(frontVec, Vector3.down * groundDistance, new Color(0, 1, 0));
        RaycastHit2D rayHit = Physics2D.Raycast(frontVec, Vector3.down, groundDistance, tileLayer);
        if (rayHit.collider == null) 
        {
            Turn();
        }
            
        // 플레이어 가까우면 공격
        isRight = !spriteRenderer.flipX;
        Debug.DrawRay(rbody.position, Vector3.right * rightValue * playerDetectDistance, new Color(0, 1, 0));
        var rayHitPlayer = Physics2D.Raycast(rbody.position, Vector3.right * rightValue, playerDetectDistance, playerLayer);
        if (rayHitPlayer.collider != null && !isAttack) 
        {
            isAttack = true;
            _eEnemyState = EEnemyState.ATTACK;
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
        if (isDie) 
        {
            this.isDie = true;
            _eEnemyState = EEnemyState.DIE;
            var dieEnemyInfo = Array.Find(enemyInfos, x => x.eEnemyState == EEnemyState.DIE);
            float dieTime = dieEnemyInfo.interval * dieEnemyInfo.sprites.Length;
            GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject, dieTime);
        }
    }
}
