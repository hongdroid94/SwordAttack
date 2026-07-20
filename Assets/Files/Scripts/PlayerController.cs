using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
	public Subject<int> Die = new();


	public enum EPlayerState { IDLE, RUN, JUMP, CLIMB, ATTACK, DIE, DASH }
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
				case EPlayerState.DASH:
					animator.Play("Player_Dash");
					break;
			}
		}
	}
	
	[Header("Fields")]
	[SerializeField] bool isDebugLine;
	[SerializeField] float jumpPower;
	[SerializeField] float speed;
	[SerializeField] float groundDistance;
	[SerializeField] float climbDistance;
	[SerializeField] float attackTime;
	[SerializeField] float dieTime;
	[SerializeField] float knockbackTime;
	[SerializeField] Vector2 knockback;

	[Header("Wall Climb (록맨X 삼각차기)")]
	// 벽에 매달릴 때 초당 몇 유닛씩 미끄러져 내려갈지. 0이면 완전히 정지.
	[SerializeField] float wallSlideSpeed = 3f;
	// 벽점프 시 벽 반대 방향으로 밀어내는 속도.
	[SerializeField] float wallJumpHorizontal = 11f;
	// 벽점프 시 위로 솟구치는 속도. jumpPower(지상 점프)보다 약간 낮게 잡는다.
	[SerializeField] float wallJumpVertical = 16f;
	// 벽점프 직후 조작이 잠기는 시간.
	// 이 값이 곧 "벽에서 얼마나 떨어지느냐"를 결정한다.
	// 너무 짧으면 벽에 붙은 채 제자리 무한점프가 되고,
	// 너무 길면 되돌아왔을 때 이미 하강 중이라 벽을 오를 수 없다.
	[SerializeField] float wallJumpControlLock = 0.1f;
	// 벽 쪽 입력을 놓은 뒤에도 매달림을 유지해 주는 유예 시간.
	// 모바일 가상 조이스틱은 입력이 튀기 때문에 0이면 자꾸 떨어진다.
	[SerializeField] float wallStickGrace = 0.12f;
	// 조이스틱이 이 값 이상 기울어야 "벽 쪽으로 미는 중"으로 인정한다.
	[SerializeField] float wallInputThreshold = 0.2f;
	// 벽 감지 레이를 몸통 위/아래로 얼마나 벌려서 쏠지.
	[SerializeField] float wallRayOffset = 0.8f;

	[Header("Dash (록맨X 대시)")]
	// 대시 중 수평 속도. speed(8)의 2배 남짓이 록맨X 체감에 가깝다.
	[SerializeField] float dashSpeed = 18f;
	// 버튼을 끝까지 누르고 있을 때의 최대 대시 시간. Player_Dash 클립 길이와 맞춰뒀다.
	[SerializeField] float dashTime = 0.3f;
	// 대시가 끝난 뒤 다시 대시할 수 있을 때까지의 간격.
	[SerializeField] float dashCooldown = 0.15f;
	// 공중 대시 허용 여부. 착지하면 다시 1회 충전된다.
	[SerializeField] bool allowAirDash = true;
	// 대시가 끝난 직후 관성으로 미끄러지는 시간.
	// 버튼을 일찍 떼면 그 자리에서 이 구간을 거쳐 서서히 멈춘다.
	// 감속이 끝나기 전에 시간이 다하면 속도가 툭 끊기므로
	// dashSpeed / dashSlideDeceleration 보다 넉넉하게 잡아야 한다.
	// 감속 거리는 dashSpeed^2 / (2 * dashSlideDeceleration).
	// 기본값 기준 18^2 / 180 = 1.8유닛(약 1.8타일)을 미끄러지고 멈춘다.
	[SerializeField] float dashSlideTime = 0.3f;
	// 관성 구간의 감속률(초당 속도 변화량). 낮을수록 더 길게 미끄러진다.
	[SerializeField] float dashSlideDeceleration = 90f;
	// 벽에 매달린 채 대시를 누르고 점프하면 수평 도약이 이만큼 배로 늘어난다.
	[SerializeField] float wallDashJumpMultiplier = 1.6f;

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
	bool isDie;
	bool isRight = true;
	bool isKnockback;

	// 접촉 중인 벽의 방향. +1 = 오른쪽 벽, -1 = 왼쪽 벽, 0 = 벽 없음.
	// 캐릭터가 바라보는 방향과 무관하게 판정하므로,
	// 벽점프로 몸이 뒤집혀도 같은 벽을 계속 인식할 수 있다.
	int wallDir;
	Vector2 moveInput;
	float controlLockTimer;
	float wallStickTimer;

	bool isDash;
	// 대시 시작 시점의 방향으로 고정한다. 록맨X처럼 대시 중에는 방향을 바꿀 수 없다.
	int dashDir;
	float dashTimer;
	float dashCooldownTimer;
	bool airDashUsed;
	// 대시 버튼을 누르고 있는 중인가. 벽 대시점프 판정에도 쓰인다.
	bool dashHeld;
	float dashSlideTimer;

	int rightValue => isRight ? 1 : -1;


	void Start()
	{
		tileLayer = LayerMask.GetMask("Tile");
		enemyLayer = LayerMask.GetMask("Enemy");

		InputManager.JoyStick.Subscribe(Move).AddTo(this);
		InputManager.Jump.Subscribe(Jump).AddTo(this);
		InputManager.Attack.Subscribe(Attack).AddTo(this);
		InputManager.Dash.Subscribe(Dash).AddTo(this);
		InputManager.DashRelease.Subscribe(DashRelease).AddTo(this);
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
			ePlayerState = isDie ? EPlayerState.DIE : isDash ? EPlayerState.DASH : isClimb ? EPlayerState.CLIMB : isJump ? EPlayerState.JUMP : isRun ? EPlayerState.RUN : EPlayerState.IDLE;
		}
	}

	void FixedUpdate()
	{
		if (controlLockTimer > 0f) controlLockTimer -= Time.fixedDeltaTime;

		GroundCheck();
		ClimbCheck();
		DashUpdate();
	}

	void OnTriggerEnter2D(Collider2D collision)
	{
		// Ŭ����
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
		// 벽 판정에 쓰려면 조작 불능 상태에서도 입력값 자체는 계속 받아둬야 한다.
		moveInput = dir;

		if (isDie || isKnockback) return;
		// 벽점프 직후 잠깐. 이 동안 수평 속도를 덮어쓰지 않아야
		// 벽에서 제대로 튕겨나간다.
		if (controlLockTimer > 0f) return;
		// 대시 중에는 방향 전환도, 감속도 불가. DashUpdate가 속도를 전담한다.
		if (isDash) return;

		isRun = dir.x != 0;

		float targetX = dir.x * speed;
		if (dashSlideTimer > 0f)
		{
			// 대시 직후 관성 구간. 목표 속도로 천천히 수렴한다.
			// 입력이 없으면 0으로 미끄러지며 멈추고, 방향을 계속 누르고 있으면
			// 대시 속도에서 달리기 속도로 자연스럽게 떨어진다.
			targetX = Mathf.MoveTowards(rbody.linearVelocity.x, targetX, dashSlideDeceleration * Time.fixedDeltaTime);
		}
		rbody.linearVelocity = new Vector2(targetX, rbody.linearVelocity.y);

		// 매달린 동안의 방향은 ClimbCheck가 벽 위치로 결정한다.
		// 유예 시간 중 입력이 튀어도 스프라이트가 홱 돌지 않도록 여기서 막는다.
		if (isClimb) return;

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

		// 삼각차기: 벽 반대 방향으로 비스듬히 도약한다.
		// 조작 잠금이 짧기 때문에 곧바로 벽 쪽으로 방향을 꺾어
		// "같은 벽"에 더 높은 위치로 다시 붙을 수 있다. → 벽 하나로 등반 가능
		if (isClimb)
		{
			SoundManager.Instance.PlaySFXSound("Jump");

			int away = -wallDir;
			// 대시를 누른 채 벽점프하면 반대편으로 더 멀리 날아간다.
			// 삼각차기 반경이 커져 넓은 간격을 건널 수 있다.
			bool dashJump = dashHeld;
			float horizontal = wallJumpHorizontal * (dashJump ? wallDashJumpMultiplier : 1f);
			rbody.linearVelocity = new Vector2(away * horizontal, wallJumpVertical);
			isRight = away > 0;

			// 조작 잠금이 풀리는 순간 Move가 속도를 speed로 덮어쓴다.
			// 대시점프는 잠금을 같은 비율로 늘려서 빠른 속도를 더 오래 유지시키고,
			// 이어서 관성 구간으로 서서히 죽인다. 이 둘이 합쳐져야 실제로 멀리 난다.
			// 잠금이 길어지는 만큼 벽으로 되돌아오기는 어려워지는데,
			// 대시점프는 등반이 아니라 간격을 건너는 용도이므로 의도된 맞바꿈이다.
			// 일반 벽점프는 기존 삼각차기 감각을 그대로 유지한다.
			if (dashJump) dashSlideTimer = dashSlideTime;

			controlLockTimer = wallJumpControlLock * (dashJump ? wallDashJumpMultiplier : 1f);
			wallStickTimer = 0f;
			isClimb = false;
			return;
		}

		if (!isJump)
		{
			SoundManager.Instance.PlaySFXSound("Jump");
			rbody.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
		}
	}

	void Dash(int a)
	{
		// 버튼을 누르고 있다는 사실 자체는 항상 기록한다.
		// 벽에 매달려 대시가 발동하지 않는 상황에서도 대시점프 판정에 필요하다.
		dashHeld = true;

		if (isDie || isKnockback || isAttack || isClimb) return;
		if (isDash || dashCooldownTimer > 0f) return;

		// 공중 대시는 착지 전까지 1회만.
		if (isJump)
		{
			if (!allowAirDash || airDashUsed) return;
			airDashUsed = true;
		}

		isDash = true;
		dashTimer = dashTime;
		dashDir = rightValue;
		SoundManager.Instance.PlaySFXSound("Jump");
	}

	void DashRelease(int a)
	{
		dashHeld = false;
		// 누르는 동안만 대시가 지속된다. 일찍 떼면 그 자리에서 관성 구간으로 넘어간다.
		if (isDash) EndDash();
	}

	void DashUpdate()
	{
		if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.fixedDeltaTime;
		if (dashSlideTimer > 0f) dashSlideTimer -= Time.fixedDeltaTime;

		if (!isDash) return;

		dashTimer -= Time.fixedDeltaTime;

		// 최대 시간이 다 됐거나, 벽에 매달리거나, 피격·사망하면 종료한다.
		if (dashTimer <= 0f || isClimb || isDie || isKnockback)
		{
			EndDash();
			return;
		}

		// 수직 속도는 건드리지 않는다. 대시 도중 점프하면
		// 수평 속도가 그대로 유지돼 록맨X의 대시점프가 된다.
		rbody.linearVelocity = new Vector2(dashDir * dashSpeed, rbody.linearVelocity.y);
	}

	void EndDash()
	{
		isDash = false;
		dashCooldownTimer = dashCooldown;
		// 속도를 끊지 않고 넘긴다. 이후 Move가 관성 구간으로 이어받아 서서히 줄인다.
		dashSlideTimer = dashSlideTime;
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

		// �ڽ� ���� �˻� ���ϰ�� ������
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
			// ����
			this.isDie = true;
			ePlayerState = EPlayerState.DIE;
			FindObjectOfType<GamePanel>().StopStopWatch();
			DOVirtual.DelayedCall(dieTime, () => { Die.OnNext(0); print("Die"); UIManager.Inst.ShowEndPanel(true); });
			SoundManager.Instance.PlaySFXSound("Player_Die");
		}
		else 
		{
			// �˹�
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

		// 착지하면 공중 대시를 다시 충전한다.
		if (!isJump) airDashUsed = false;
	}

	void ClimbCheck()
	{
		wallDir = DetectWall();

		// 벽 쪽으로 방향키를 밀고 있는가. 이게 매달림의 유일한 조건이다.
		// (예전에는 벽에 닿기만 하면 무조건 붙었다)
		bool pressingIntoWall = wallDir != 0
			&& Mathf.Abs(moveInput.x) >= wallInputThreshold
			&& (int)Mathf.Sign(moveInput.x) == wallDir;

		if (pressingIntoWall) wallStickTimer = wallStickGrace;
		else if (wallStickTimer > 0f) wallStickTimer -= Time.fixedDeltaTime;

		// 벽점프 직후에는 재부착을 막는다. 안 그러면 벽에 붙은 채 제자리 무한점프가 된다.
		isClimb = wallDir != 0
			&& isJump
			&& controlLockTimer <= 0f
			&& ShouldStayOnWall(pressingIntoWall);

		if (!isClimb) return;

		// 벽을 마주 본다. (벽점프 순간 반대편으로 뒤집히며 차고 나간다)
		isRight = wallDir > 0;

		// 하강할 때만 감속. 상승 중(벽점프 직후 재부착)에는 건드리지 않는다.
		if (rbody.linearVelocity.y < -wallSlideSpeed)
		{
			rbody.linearVelocity = new Vector2(rbody.linearVelocity.x, -wallSlideSpeed);
		}
	}

	/// <summary>
	/// 지금 이 프레임에 벽에 계속 매달려 있어야 하는지 판정한다.
	///
	/// pressingIntoWall : 이번 프레임에 벽 쪽으로 방향키를 밀고 있는가
	/// wallStickTimer   : 벽 쪽 입력을 놓은 뒤 남은 유예 시간 (wallStickGrace에서 감소)
	///
	/// TODO: 여기를 채워주세요.
	/// </summary>
	bool ShouldStayOnWall(bool pressingIntoWall)
	{
		return pressingIntoWall;
	}

	/// <summary>
	/// 좌우 양쪽으로 레이를 쏴서 접촉 중인 벽의 방향을 찾는다.
	/// 몸통 위/아래 두 지점에서 쏘므로 모서리에서 판정이 끊기지 않는다.
	/// </summary>
	int DetectWall()
	{
		// 입력이 있으면 그쪽 벽을 우선 검사한다. 양쪽이 다 벽인 좁은 통로에서
		// 플레이어 의도대로 붙게 하기 위함.
		int preferred = Mathf.Abs(moveInput.x) >= wallInputThreshold
			? (int)Mathf.Sign(moveInput.x)
			: rightValue;

		if (CastWall(preferred)) return preferred;
		if (CastWall(-preferred)) return -preferred;
		return 0;
	}

	bool CastWall(int dir)
	{
		Vector2 origin = rbody.position;
		Vector2 offset = Vector2.up * wallRayOffset;
		Vector2 direction = Vector2.right * dir;

		if (isDebugLine)
		{
			Debug.DrawLine(origin + offset, origin + offset + direction * climbDistance, Color.cyan);
			Debug.DrawLine(origin - offset, origin - offset + direction * climbDistance, Color.cyan);
		}

		return Physics2D.Raycast(origin + offset, direction, climbDistance, tileLayer)
			|| Physics2D.Raycast(origin - offset, direction, climbDistance, tileLayer);
	}
}
