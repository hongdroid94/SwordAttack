using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{    
    [SerializeField] float speed;
    [SerializeField] Sprite[] hitSprites;
    SpriteRenderer spriteRenderer;

    int rightValue;
    bool isHit;

    public void Init(int rightValue)
    {
        this.rightValue = rightValue;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.flipX = rightValue == -1;
    }

	void Update()
	{
		if (!isHit) 
		{
			transform.Translate(Vector3.right * rightValue * speed * Time.deltaTime);
		}
	}

	IEnumerator OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
		{
			isHit = true;
			for (int i = 0; i < hitSprites.Length; i++)
			{
				spriteRenderer.sprite = hitSprites[i];
				yield return new WaitForSeconds(0.05f);
			}
			Destroy(gameObject);
		}
		else if (collision.gameObject.layer == LayerMask.NameToLayer("Tile"))
		{
			Destroy(gameObject);
		}
		yield return null;
		
	}
}
