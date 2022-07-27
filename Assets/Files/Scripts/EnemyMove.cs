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

        // AI ����
        Invoke("Think", 5);

        // ���� �ݺ����� �������� �߻�
        // InvokeRepeating("Shot", 3.0f, 1.0f); // �����ð� ��ŭ ������ �� �ݺ��ֱ� ��ŭ ��� �ݺ�.
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
        nextMove = Random.Range(-1, 2); // �ּ� ���� ���� ���������� �ִ� ���� ���� ������ ���ܵȴ�.
        
        // Sprite Animation
        anim.SetInteger("WalkSpeed", nextMove);
        // flip sprite (��������Ʈ ���� ��ȯ)
        if (nextMove != 0)
            spriteRenderer.flipX = nextMove == -1;

        // Recurive (���)
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
