using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이 중 배경을 깔아준다.
///
/// 원래 이 게임은 플레이 중 배경이 없어서 카메라 클리어 컬러(32,37,62)가 그대로 보였는데,
/// 암반 내부 타일 색(33,38,63)과 채널당 1 차이라 벽과 빈 공간이 구분되지 않았다.
/// (스테이지 1은 통로가 좁아 외곽선 타일이 화면을 채워서 가려져 있었을 뿐이다.)
///
/// red cliff 배경 5장은 프로젝트에 있지만 EndPanel 장식으로만 쓰이고 있어서,
/// 그 Image들이 참조하는 Sprite를 그대로 빌려 월드 패럴랙스 레이어로 만든다.
/// 에셋을 복제하지 않고 씬도 건드리지 않는다.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
	// 뒤에서 앞 순서. 계수가 0이면 카메라에 완전히 붙어 무한히 먼 것처럼 보이고,
	// 1이면 월드에 고정되어 지형과 똑같이 움직인다.
	static readonly (string name, float factor)[] LayerSetup =
	{
		("bg5", 0.03f),   // 밤하늘 + 별
		("bg4", 0.10f),   // 먼 바위
		("bg3", 0.20f),   // 아치 바위
		("bg2", 0.35f),   // 중간 절벽
		("bg",  0.55f),   // 가까운 절벽
	};

	// 암반(33,38,63)보다 확실히 어두워야 벽이 벽으로 읽힌다.
	static readonly Color BackdropColor = new Color32(14, 16, 30, 255);

	const int BaseSortingOrder = -110;
	const float LayerZ = 20f;

	class Layer
	{
		public Transform root;
		public float factor;
		public float width;
		public float slackY;
	}

	Camera cam;
	Transform backdrop;
	readonly List<Layer> layers = new();

	static bool sceneHookInstalled;


	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static void Bootstrap()
	{
		Spawn();
		if (sceneHookInstalled) return;
		sceneHookInstalled = true;
		SceneManager.sceneLoaded += (_, __) => Spawn();
	}

	static void Spawn()
	{
		if (FindObjectOfType<ParallaxBackground>() != null) return;
		new GameObject(nameof(ParallaxBackground)).AddComponent<ParallaxBackground>();
	}

	void Awake()
	{
		cam = Camera.main;
		if (cam == null) cam = FindObjectOfType<Camera>();
		if (cam == null) { enabled = false; return; }

		BuildBackdrop();
		BuildLayers();
	}

	/// <summary>가장 뒤에 깔리는 단색 판. 이게 있어야 배경 그림의 투명한 부분도 암반과 구분된다.</summary>
	void BuildBackdrop()
	{
		Texture2D tex = new Texture2D(1, 1);
		tex.SetPixel(0, 0, Color.white);
		tex.Apply();

		GameObject go = new GameObject("Backdrop");
		go.transform.SetParent(transform);
		SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
		sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
		sr.color = BackdropColor;
		sr.sortingLayerID = 0;                 // Default — Tile 레이어보다 뒤
		sr.sortingOrder = BaseSortingOrder - 1;
		backdrop = go.transform;
	}

	void BuildLayers()
	{
		Dictionary<string, Sprite> found = CollectSprites();

		for (int i = 0; i < LayerSetup.Length; i++)
		{
			if (!found.TryGetValue(LayerSetup[i].name, out Sprite sprite) || sprite == null) continue;

			GameObject root = new GameObject("Layer_" + LayerSetup[i].name);
			root.transform.SetParent(transform);

			float width = sprite.bounds.size.x;
			float height = sprite.bounds.size.y;

			// 좌우로 3장 이어 붙여 두면 카메라가 어디로 가도 빈틈이 생기지 않는다.
			for (int copy = -1; copy <= 1; copy++)
			{
				GameObject piece = new GameObject("Piece" + copy);
				piece.transform.SetParent(root.transform);
				piece.transform.localPosition = new Vector3(copy * width, 0f, 0f);
				SpriteRenderer sr = piece.AddComponent<SpriteRenderer>();
				sr.sprite = sprite;
				sr.sortingLayerID = 0;
				sr.sortingOrder = BaseSortingOrder + i;
			}

			float camHeight = cam.orthographicSize * 2f;
			layers.Add(new Layer
			{
				root = root.transform,
				factor = LayerSetup[i].factor,
				width = width,
				// 세로로 밀 수 있는 여유. 이걸 넘기면 그림 바깥의 빈 공간이 보인다.
				slackY = Mathf.Max(0f, (height - camHeight) * 0.5f - 0.5f),
			});
		}

		if (layers.Count == 0) Debug.LogWarning("[ParallaxBackground] 배경 스프라이트를 찾지 못했습니다.");
	}

	/// <summary>
	/// EndPanel 아래에 있는 bg~bg5 Image에서 Sprite만 빌려온다.
	/// EndPanel은 시작 시 비활성이라 FindObjectsOfTypeAll로 훑어야 한다.
	/// </summary>
	static Dictionary<string, Sprite> CollectSprites()
	{
		Dictionary<string, Sprite> result = new();
		Image[] all = Resources.FindObjectsOfTypeAll<Image>();
		for (int i = 0; i < all.Length; i++)
		{
			if (!all[i].gameObject.scene.IsValid()) continue;
			string n = all[i].gameObject.name;
			for (int s = 0; s < LayerSetup.Length; s++)
			{
				if (n == LayerSetup[s].name && !result.ContainsKey(n)) result[n] = all[i].sprite;
			}
		}
		return result;
	}

	void LateUpdate()
	{
		if (cam == null) return;

		Vector3 camPos = cam.transform.position;

		if (backdrop != null)
		{
			backdrop.position = new Vector3(camPos.x, camPos.y, LayerZ + 1f);
			// 회전 없는 직교 카메라 기준으로 화면을 넉넉히 덮는다.
			float h = cam.orthographicSize * 2f;
			backdrop.localScale = new Vector3(h * cam.aspect + 2f, h + 2f, 1f);
		}

		for (int i = 0; i < layers.Count; i++)
		{
			Layer l = layers[i];
			float offsetX = camPos.x * l.factor;
			float offsetY = Mathf.Clamp(camPos.y * l.factor, -l.slackY, l.slackY);

			// Repeat로 한 장 폭만큼씩 되감아 무한히 이어지게 만든다.
			l.root.position = new Vector3(
				camPos.x - Mathf.Repeat(offsetX, l.width),
				camPos.y - offsetY,
				LayerZ);
		}
	}
}
