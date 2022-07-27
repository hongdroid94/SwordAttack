using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    [SerializeField] int nextMove;                   
    [SerializeField] float playerDetectDistance;     
    [SerializeField] float groundDistance;     
    [SerializeField] GameObject bullet;              
    [SerializeField] Transform bulletPos;            
    

    Rigidbody2D rigid;    
    Animator anim;                  
    SpriteRenderer spriteRenderer;  
    bool isCheckDelay;
    bool isDirectionLeft;

    public bool IsDirectionLeft => isDirectionLeft;


	void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // AI 동작
        Invoke("Think", 5);

        // 무한 반복으로 에너지볼 발사
        // InvokeRepeating("Shot", 3.0f, 1.0f); // 지연시간 만큼 지연된 후 반복주기 만큼 계속 반복.
    }
   

    void FixedUpdate()
    {
        // Move
        rigid.velocity = new Vector2(nextMove, rigid.velocity.y);

        // Check Platform
        Vector2 frontVec = new Vector2(rigid.position.x + nextMove * 0.3f, rigid.position.y);
        Debug.DrawRay(frontVec, Vector3.down * groundDistance, new Color(0, 1, 0));
        RaycastHit2D rayHit = Physics2D.Raycast(frontVec, Vector3.down, groundDistance, LayerMask.GetMask("Tile"));
        if (rayHit.collider == null)
            Turn();

        // Check Player       
        isDirectionLeft = spriteRenderer.flipX;       
        Vector2 playerOrigin = rigid.position;
        Debug.DrawRay(playerOrigin, IsDirectionLeft ? Vector3.left * playerDetectDistance : Vector3.right * playerDetectDistance, new Color(0, 1, 0));
        RaycastHit2D rayHitPlayer = Physics2D.Raycast(playerOrigin, IsDirectionLeft ? Vector3.left : Vector3.right, playerDetectDistance, LayerMask.GetMask("Player"));
        if (rayHitPlayer.collider != null)
        {
            // detected player
            if (!isCheckDelay)
            {
                Shot();
                isCheckDelay = true;
                StartCoroutine(WaitForShot());                
            }
            
        }        
    }

    void Think()
    {
        // Set Next Active
        nextMove = Random.Range(-1, 2); // 최소 값은 랜덤 범위이지만 최댓 값은 랜덤 값에서 제외된다.
        
        // Sprite Animation
        anim.SetInteger("WalkSpeed", nextMove);
        // flip sprite (스프라이트 방향 전환)
        if (nextMove != 0)
            spriteRenderer.flipX = nextMove == -1;

        // Recurive (재귀)
        float nextThinkTime = Random.Range(2f, 5f);
        Invoke("Think", nextThinkTime);
    }

    void Turn()
    {
        nextMove *= -1;
        spriteRenderer.flipX = nextMove == -1;        

        CancelInvoke("Think");
        Invoke("Think", 2);
    }

    void Shot()
    {
        //Instantiate(bullet, bulletPos.position, transform.rotation).GetComponent<Bullet>().Init(this);
    }

    IEnumerator WaitForShot()
    {
        yield return new WaitForSeconds(1.0f);
        isCheckDelay = false;
    }
}
