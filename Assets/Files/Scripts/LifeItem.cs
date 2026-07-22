using UnityEngine;

/// <summary>
/// 플레이어가 닿으면 체력을 1 회복하는 하트 아이템.
///
/// 스테이지에 흩뿌리기 위해 코드로 생성한다(StageManager). 씬/프리팹을 만들지 않고
/// SpriteRenderer + 트리거 콜라이더만 붙여 완성한다.
/// </summary>
public class LifeItem : MonoBehaviour
{
	static Sprite heartSprite;

	/// <summary>월드 좌표에 하트 아이템을 만든다.</summary>
	public static GameObject Spawn(Vector3 position, Transform parent = null)
	{
		if (heartSprite == null) heartSprite = Resources.Load<Sprite>("UI/Icon_Life");

		GameObject go = new GameObject("LifeItem");
		go.transform.SetParent(parent);
		go.transform.position = position;

		SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
		sr.sprite = heartSprite;
		// Default 레이어에 두면 Tile 레이어(지형)보다 뒤에 그려져 벽에 가린다.
		// Player 레이어로 올려 지형·배경 앞에 확실히 보이게 한다.
		sr.sortingLayerName = "Player";
		sr.sortingOrder = -1;
		// 원본 아이콘이 89px이라 타일 크기(1유닛)에 맞춰 줄인다.
		if (heartSprite != null)
		{
			float target = 1.2f;
			go.transform.localScale = Vector3.one * (target / heartSprite.bounds.size.y);
		}

		CircleCollider2D col = go.AddComponent<CircleCollider2D>();
		col.isTrigger = true;
		col.radius = 0.6f;

		go.AddComponent<LifeItem>();
		return go;
	}

	void OnTriggerEnter2D(Collider2D col)
	{
		if (!col.CompareTag("Player")) return;

		Damaged damaged = col.GetComponent<Damaged>();
		if (damaged == null) return;

		// 이미 체력이 가득이면 아이템을 남겨 둔다(낭비 방지).
		if (damaged.Health >= damaged.MaxHealth) return;

		damaged.Heal(1);
		if (SoundManager.Instance != null) SoundManager.Instance.PlaySFXSound("Jump");
		Destroy(gameObject);
	}
}
