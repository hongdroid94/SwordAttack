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

    [SerializeField] float flashTime;
    [SerializeField] int health;
    
    SpriteRenderer spriteRenderer;

	public int Health => health;


	void Start()
	{
        spriteRenderer = GetComponent<SpriteRenderer>();
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
            // 죽음
            isDie = true;
            health = 0;
        }
        Defect?.Invoke(isDie, health, damageDir);
    }

	void OnTriggerEnter2D(Collider2D col)
	{
        // 플레이어가 적한테 피격당함
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
