using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
	public Subject<int> Die = new();


	public enum EPlayerState { IDLE, RUN, JUMP, CLIMB, ATTACK, DIE }
	[SerializeField] EPlayerState _ePlayerState;
	public EPlayerState ePlayerState
	{
		get => _ePlayerState;
		set
		{
			_ePlayerState = value;

			switch (_ePlayerState)
			{
				case EPlayerState.IDLE:
					animator.Play("Player_Idle");
					break;
				case EPlayerState.RUN:
					animator.Play("Player_Run");
					break;
				case EPlayerState.JUMP:
					animator.Play("Player_Jump");
					break;
				case EPlayerState.CLIMB:
					animator.Play("Player_Climb");
					break;
				case EPlayerState.ATTACK:
					animator.Play("Player_Attack");
					break;
				case EPlayerState.DIE:
					animator.Play("Player_Die");
					break;
			}
		}
	}
	
	[Header("Fields")]
	[SerializeField] bool isDebugLine;
	[SerializeField] float jumpPower;
	[SerializeField] float climbJumpPower;
	[SerializeField] float speed;
	[SerializeField] float groundDistance;
	[SerializeField] float climbDistance;
	[SerializeField] float climbFallSpeed;
	[SerializeField] float attackTime;
	[SerializeField] float dieTime;
	[SerializeField] float knockbackTime;
	[SerializeField] Vector2 knockback;

	[Header("Properties")]
	[SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody2D rbody;
	[SerializeField] Damaged damaged;

	int tileLayer;
	int enemyLayer;
	bool isJump;
	bool isRun;
	bool isAttack;
	bool isClimb;
	bool isClimbJump;
	bool isDie;
	bool isRight = true;
	bool isKnockback;

	int rightValue => isRight ? 1 : -1;


	void Start()
	{
		tileLayer = LayerMask.GetMask("Tile");
		enemyLayer = LayerMask.GetMask("Enemy");

		InputManager.JoyStick.Subscribe(Move).AddTo(this);
		InputManager.Jump.Subscribe(Jump).AddTo(this);
		InputManager.Attack.Subscribe(Attack).AddTo(this);
		this.ObserveEveryValueChanged(x => x.isRight).Subscribe(x => spriteRenderer.flipX = !x).AddTo(this);
		damaged.Defect += Defect;
	}

	void OnDestroy()
	{
		damaged.Defect -= Defect;
	}

	void Update()
	{
		if (!isAttack) 
		{
			ePlayerState = isDie ? EPlayerState.DIE : isClimb ? EPlayerState.CLIMB : isJump ? EPlayerState.JUMP : isRun ? EPlayerState.RUN : EPlayerState.IDLE;
		}
	}

	void FixedUpdate()
	{
		GroundCheck();
		ClimbCheck();
	}

	void OnTriggerEnter2D(Collider2D collision)
	{
		// 클리어
		if (collision.CompareTag("EndFlag")) 
		{
			print("Win");
			FindObjectOfType<GamePanel>().StopStopWatch();
			UIManager.Inst.ShowEndPanel(false);
			gameObject.SetActive(false);
		}
	}


	void Move(Vector2 dir)
	{
		if (isClimbJump || isDie || isKnockback) return;

		isRun = dir.x != 0;
		rbody.velocity = new Vector2(dir.x * speed, rbody.velocity.y);
		if (dir.x > 0)
		{
			isRight = true;
		}
		else if (dir.x < 0)
		{
			isRight = false;
		}
	}

	void Jump(int a) 
	{
		if (isDie || isKnockback) return;

		if (!isJump) 
		{
			SoundManager.Instance.PlaySFXSound("Jump");
			rbody.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
		}
		isClimbJump = false;
		if (isClimb)
		{
			// 벽 점프
			SoundManager.Instance.PlaySFXSound("Jump");
			isClimbJump = true;
			DOVirtual.DelayedCall(0.3f, () => isClimbJump = false);
			rbody.velocity = new Vector2(-rightValue * climbJumpPower, 0.9f * climbJumpPower);
			isRight = !isRight;
		}
	}

	void Attack(int a) 
	{
		if (isDie || isKnockback) return;

		if (!isAttack && !isClimb) 
		{
			StopAllCoroutines();
			StartCoroutine(nameof(AttackCo));
			SoundManager.Instance.PlaySFXSound("Player_Attack");
		}
	}

	IEnumerator AttackCo() 
	{
		isAttack = true;
		ePlayerState = EPlayerState.ATTACK;

		yield return new WaitForSeconds(attackTime / 6f);

		// 박스 영역 검사 적일경우 데미지
		var hits = Physics2D.BoxCastAll(rbody.position + new Vector2(1.16f, 0.21f) * rightValue, new Vector2(1.68f, 2.68f), 0, Vector2.right * rightValue, 1, enemyLayer);
		for (int i = 0; i < hits.Length; i++)
		{
			if (hits[i].collider.TryGetComponent(out Damaged damaged))
			{
				damaged.Damage(1);
			}
		}

		yield return new WaitForSeconds(attackTime * 5f / 6f);
		isAttack = false; 
		ePlayerState = EPlayerState.IDLE;
	}

	void Defect(bool isDie, int health, int damageDir) 
	{
		if (isKnockback) return;

		if (!this.isDie && isDie)
		{
			// 죽음
			this.isDie = true;
			ePlayerState = EPlayerState.DIE;
			FindObjectOfType<GamePanel>().StopStopWatch();
			DOVirtual.DelayedCall(dieTime, () => { Die.OnNext(0); print("Die"); UIManager.Inst.ShowEndPanel(true); });
			SoundManager.Instance.PlaySFXSound("Player_Die");
		}
		else 
		{
			// 넉백
			isKnockback = true;
			rbody.AddForce(new Vector2(knockback.x * -damageDir, knockback.y), ForceMode2D.Impulse);
			DOVirtual.DelayedCall(knockbackTime, () => isKnockback = false);
			SoundManager.Instance.PlaySFXSound("Player_Damage");
		}
	}


	void GroundCheck() 
	{
		if (isDebugLine)
		{
			Debug.DrawLine(rbody.position, rbody.position + Vector2.down * groundDistance);
		}
		isJump = Physics2D.Raycast(rbody.position, Vector2.down, groundDistance, tileLayer).collider == null;
	}

	void ClimbCheck() 
	{
		if (isDebugLine)
		{
			Debug.DrawLine(rbody.position, rbody.position + Vector2.right * rightValue * climbDistance);
		}
		isClimb = Physics2D.Raycast(rbody.position, Vector2.right * rightValue, climbDistance, tileLayer) && isJump;

		if (isClimb) 
		{
			rbody.velocity = new Vector2(rbody.velocity.x, rbody.velocity.y * climbFallSpeed);
		}
	}
}
