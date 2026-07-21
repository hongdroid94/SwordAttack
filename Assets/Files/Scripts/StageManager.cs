using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Cinemachine;

/// <summary>
/// 스테이지 전환을 담당한다.
///
/// 스테이지 1은 손으로 찍어 씬에 들어 있는 타일맵이므로, 시작할 때 그대로
/// 스냅샷해 두고 되돌아올 때 복원한다. 스테이지 2부터는 Resources의
/// ASCII 맵을 StageBuilder로 찍는다.
///
/// 씬 YAML을 건드리지 않으려고 RuntimeInitializeOnLoadMethod로 스스로 생성되며,
/// 필요한 오브젝트는 이름·타입으로 찾는다.
/// </summary>
public class StageManager : MonoBehaviour
{
	public const int LastStage = 2;

	public static StageManager Instance { get; private set; }
	public int CurrentStage { get; private set; } = 1;

	// 스테이지 2 맵을 찍기 시작할 셀 좌표.
	static readonly Vector2Int Stage2Origin = new Vector2Int(0, 0);

	Tilemap wallMap;
	Tilemap decorationMap;
	Transform player;
	Rigidbody2D playerBody;
	Transform endFlag;
	GameObject enemies;
	// 종류별 대표 적. 스테이지 2에서 이걸 복제해 배치한다.
	readonly Dictionary<char, GameObject> enemyPrototypes = new();
	// 스테이지 2에서 스폰한 적. 스테이지를 떠날 때 정리한다.
	readonly List<GameObject> spawnedEnemies = new();
	PolygonCollider2D cameraBounds;
	CinemachineConfiner2D confiner;
	CinemachineVirtualCameraBase vcam;

	// 스테이지 1 원본
	BoundsInt wallBounds, decorationBounds;
	TileBase[] wallTiles, decorationTiles;
	Vector3 stage1Spawn, stage1Goal;
	List<Vector2[]> stage1CameraPaths;
	bool captured;

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
		if (FindObjectOfType<StageManager>() != null) return;
		new GameObject(nameof(StageManager)).AddComponent<StageManager>();
	}

	void Awake()
	{
		Instance = this;
		Acquire();
		// 스냅샷보다 먼저 해야 스테이지 1로 되돌아올 때도 바뀐 타일이 유지된다.
		DecorationTiles.Apply(decorationMap);
		CaptureStage1();
	}

	void Acquire()
	{
		Tilemap[] maps = FindObjectsOfType<Tilemap>(true);
		for (int i = 0; i < maps.Length; i++)
		{
			if (maps[i].name == "Tilemap_Wall") wallMap = maps[i];
			else if (maps[i].name == "Tilemap_Decoration") decorationMap = maps[i];
		}

		PlayerController pc = FindObjectOfType<PlayerController>(true);
		if (pc != null)
		{
			player = pc.transform;
			playerBody = pc.GetComponent<Rigidbody2D>();
		}

		GameObject flag = GameObject.Find("EndFlag");
		if (flag != null) endFlag = flag.transform;
		enemies = GameObject.Find("Enemies");

		// 씬에 배치된 적들에서 종류별 대표를 하나씩 잡아둔다.
		// 아직 활성 상태인 지금(Awake) 잡아야 나중에 복제할 수 있다.
		if (enemies != null)
		{
			foreach (Transform child in enemies.transform)
			{
				char kind = KindOf(child.name);
				if (kind != '\0' && !enemyPrototypes.ContainsKey(kind)) enemyPrototypes[kind] = child.gameObject;
			}
		}

		GameObject range = GameObject.Find("CMRange");
		if (range != null) cameraBounds = range.GetComponent<PolygonCollider2D>();
		confiner = FindObjectOfType<CinemachineConfiner2D>(true);
		vcam = FindObjectOfType<CinemachineVirtualCameraBase>(true);
	}

	void CaptureStage1()
	{
		if (wallMap == null) return;

		wallMap.CompressBounds();
		wallBounds = wallMap.cellBounds;
		wallTiles = wallMap.GetTilesBlock(wallBounds);

		if (decorationMap != null)
		{
			decorationMap.CompressBounds();
			decorationBounds = decorationMap.cellBounds;
			decorationTiles = decorationMap.GetTilesBlock(decorationBounds);
		}

		if (player != null) stage1Spawn = player.position;
		if (endFlag != null) stage1Goal = endFlag.position;

		if (cameraBounds != null)
		{
			stage1CameraPaths = new List<Vector2[]>();
			for (int i = 0; i < cameraBounds.pathCount; i++) stage1CameraPaths.Add(cameraBounds.GetPath(i));
		}

		captured = true;
	}

	/// <summary>깃발에 닿았을 때. 다음 스테이지가 있으면 넘어가고, 없으면 클리어 처리한다.</summary>
	public void OnGoalReached()
	{
		if (CurrentStage < LastStage)
		{
			LoadStage(CurrentStage + 1);
			return;
		}

		// 마지막 스테이지 — 기존 클리어 처리. 타이머는 여기서 멈춘다.
		GamePanel panel = FindObjectOfType<GamePanel>(true);
		if (panel != null) panel.StopStopWatch();
		UIManager.Inst.ShowEndPanel(false);
		if (player != null) player.gameObject.SetActive(false);
	}

	public void LoadStage(int stage)
	{
		if (!captured) return;

		CurrentStage = stage;
		if (stage == 1) BuildStage1();
		else BuildStage2();

		if (player != null) player.gameObject.SetActive(true);
	}

	void BuildStage1()
	{
		wallMap.ClearAllTiles();
		wallMap.SetTilesBlock(wallBounds, wallTiles);
		if (decorationMap != null)
		{
			decorationMap.ClearAllTiles();
			decorationMap.SetTilesBlock(decorationBounds, decorationTiles);
		}

		if (endFlag != null) endFlag.position = stage1Goal;

		ClearSpawnedEnemies();
		if (enemies != null) enemies.SetActive(true);

		if (cameraBounds != null && stage1CameraPaths != null)
		{
			cameraBounds.pathCount = stage1CameraPaths.Count;
			for (int i = 0; i < stage1CameraPaths.Count; i++) cameraBounds.SetPath(i, stage1CameraPaths[i]);
		}

		Teleport(stage1Spawn);
	}

	void BuildStage2()
	{
		TextAsset asset = Resources.Load<TextAsset>("Stages/Stage2");
		if (asset == null)
		{
			Debug.LogError("[StageManager] Resources/Stages/Stage2.txt를 찾지 못했습니다.");
			return;
		}

		StageBuilder builder = new StageBuilder(asset.text);

		wallMap.ClearAllTiles();
		if (decorationMap != null) decorationMap.ClearAllTiles();
		builder.Paint(wallMap, Stage2Origin);

		// 스테이지 1 적은 감추고, 이 맵에 적힌 위치대로 새로 배치한다.
		if (enemies != null) enemies.SetActive(false);
		ClearSpawnedEnemies();   // Shift+2 재진입 시 중복 방지
		SpawnEnemies(builder.EnemySpawns);

		Vector3 spawn = CellToWorld(builder.Spawn + Stage2Origin);
		if (endFlag != null) endFlag.position = CellToWorld(builder.Goal + Stage2Origin);

		if (cameraBounds != null)
		{
			// 카메라가 맵 밖을 비추지 않도록 경계를 맵 크기에 맞춘다.
			Vector3 min = CellToWorld(Stage2Origin);
			Vector3 max = CellToWorld(Stage2Origin + new Vector2Int(builder.Width, builder.Height));
			cameraBounds.pathCount = 1;
			cameraBounds.SetPath(0, new[]
			{
				new Vector2(min.x, min.y),
				new Vector2(max.x, min.y),
				new Vector2(max.x, max.y),
				new Vector2(min.x, max.y),
			});
		}

		Teleport(spawn);
	}

	void SpawnEnemies(List<(char kind, Vector2Int cell)> spawns)
	{
		for (int i = 0; i < spawns.Count; i++)
		{
			if (!enemyPrototypes.TryGetValue(spawns[i].kind, out GameObject prototype) || prototype == null) continue;

			GameObject enemy = Instantiate(prototype, CellToWorld(spawns[i].cell + Stage2Origin), Quaternion.identity);
			enemy.SetActive(true);   // 원본이 비활성이어도 복제본은 켠다.
			spawnedEnemies.Add(enemy);
		}
	}

	void ClearSpawnedEnemies()
	{
		for (int i = 0; i < spawnedEnemies.Count; i++)
		{
			if (spawnedEnemies[i] != null) Destroy(spawnedEnemies[i]);
		}
		spawnedEnemies.Clear();
	}

	/// <summary>인스턴스 이름(Enemy_Bat 등)을 맵 마커 문자로 바꾼다.</summary>
	static char KindOf(string name)
	{
		if (name.Contains("Bat")) return 'b';
		if (name.Contains("Wolf")) return 'w';
		if (name.Contains("Golem")) return 'g';
		if (name.Contains("Witch")) return 't';
		return '\0';
	}

	Vector3 CellToWorld(Vector2Int cell)
	{
		// 타일 앵커가 가운데(0.5, 0.5)라 셀 좌표에 반 칸을 더해야 칸 중앙이 된다.
		return wallMap.CellToWorld(new Vector3Int(cell.x, cell.y, 0)) + new Vector3(0.5f, 0.5f, 0f);
	}

	void Teleport(Vector3 position)
	{
		if (player == null) return;

		Vector3 delta = position - player.position;
		player.position = position;
		if (playerBody != null) playerBody.linearVelocity = Vector2.zero;

		// 카메라가 맵을 가로질러 따라오지 않고 즉시 붙게 한다.
		if (vcam != null)
		{
			vcam.OnTargetObjectWarped(player, delta);
			vcam.PreviousStateIsValid = false;
		}
		// 경계 폴리곤이 바뀌었으므로 캐시를 버려야 새 범위가 반영된다.
		if (confiner != null) confiner.InvalidateCache();
	}

#if UNITY_EDITOR
	void Update()
	{
		// 에디터 전용 치트: Shift + 1 / Shift + 2
		if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) return;

		if (Input.GetKeyDown(KeyCode.Alpha1)) LoadStage(1);
		else if (Input.GetKeyDown(KeyCode.Alpha2)) LoadStage(2);
	}
#endif
}
