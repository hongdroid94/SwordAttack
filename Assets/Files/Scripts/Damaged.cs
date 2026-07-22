using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class Damaged : MonoBehaviour
{
    /// <summary> isDie, health, damageDir </summary>
    public event System.Action<bool, int, int> Defect;
    /// <summary> 체력이 바뀔 때마다(피격·회복 공통) 현재 체력을 알린다. UI 갱신용. </summary>
    public event System.Action<int> HealthChanged;

    [SerializeField] float flashTime;
    [SerializeField] int health;

    int maxHealth;
    SpriteRenderer spriteRenderer;

	public int Health => health;
	public int MaxHealth => maxHealth;


	void Awake()
	{
        // GamePanel.Start가 Health를 읽어 하트를 만들기 전에 최대치를 확정해 둔다.
        maxHealth = health;
    }

	void Start()
	{
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary> 라이프 아이템 회복. 최대치를 넘지 않으며, 죽은 뒤에는 회복되지 않는다. </summary>
    public void Heal(int amount)
    {
        if (health <= 0 || health >= maxHealth) return;

        health = Mathf.Min(health + amount, maxHealth);
        HealthChanged?.Invoke(health);
    }

    public void Damage(int damage, int damageDir = 0) 
    {
        bool isDie = false;
        health -= damage;

        spriteRenderer.material.SetFloat("_IsFlash", 1);
        DOVirtual.DelayedCall(flashTime * 0.1f * 2, () => spriteRenderer.material.SetFloat("_IsFlash", 0));
        DOVirtual.DelayedCall(flashTime * 0.1f * 4, () => spriteRenderer.material.SetFloat("_IsFlash", 1));
        DOVirtual.DelayedCall(flashTime * 0.1f * 6, () => spriteRenderer.material.SetFloat("_IsFlash", 0));
        DOVirtual.DelayedCall(flashTime * 0.1f * 8, () => spriteRenderer.material.SetFloat("_IsFlash", 1));
        DOVirtual.DelayedCall(flashTime * 0.1f * 10, () => spriteRenderer.material.SetFloat("_IsFlash", 0));

        if (health <= 0)
        {
            // ����
            isDie = true;
            health = 0;
        }
        Defect?.Invoke(isDie, health, damageDir);
        HealthChanged?.Invoke(health);
    }

	void OnTriggerEnter2D(Collider2D col)
	{
        // �÷��̾ ������ �ǰݴ���
        if (gameObject.CompareTag("Player") && col.gameObject.layer == LayerMask.NameToLayer("Enemy")) 
        {
            int damageDir = 0;
            if (transform.position.x < col.transform.position.x) 
            {
                damageDir = 1;
            }
            else if (transform.position.x > col.transform.position.x)
            {
                damageDir = -1;
            }
            Damage(1, damageDir);
        }
	}
}
