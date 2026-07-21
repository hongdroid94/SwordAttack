using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ASCII 텍스트 맵을 Tilemap에 찍는다.
///
/// '#' 벽 / '.' 빈칸 / 'P' 시작 지점 / 'G' 도착 지점 (P·G는 빈칸으로 취급)
/// 텍스트는 사람이 읽는 순서(위 -> 아래)로 저장하고, 여기서 뒤집어 월드 y로 바꾼다.
///
/// 어떤 벽 스프라이트를 쓸지는 StageAutotile이 이웃 배치를 보고 결정하므로,
/// 맵 텍스트에는 "여기가 벽인가"만 적으면 된다.
/// </summary>
public class StageBuilder
{
	public readonly bool[,] Wall;      // [x, y] — y는 월드 기준(위로 증가)
	public readonly int Width;
	public readonly int Height;
	public Vector2Int Spawn { get; private set; } = new Vector2Int(2, 2);
	public Vector2Int Goal { get; private set; } = new Vector2Int(10, 2);

	static Sprite[] sprites;
	static readonly Dictionary<int, TileBase> tileCache = new();


	public StageBuilder(string ascii)
	{
		string[] lines = ascii.Replace("\r", "").TrimEnd('\n').Split('\n');
		Height = lines.Length;
		Width = 0;
		for (int i = 0; i < lines.Length; i++) Width = Mathf.Max(Width, lines[i].Length);

		Wall = new bool[Width, Height];
		for (int row = 0; row < Height; row++)
		{
			// 텍스트 첫 줄이 가장 높은 곳이므로 뒤집는다.
			int y = Height - 1 - row;
			string line = lines[row];
			for (int x = 0; x < line.Length; x++)
			{
				char c = line[x];
				if (c == '#') Wall[x, y] = true;
				else if (c == 'P') Spawn = new Vector2Int(x, y);
				else if (c == 'G') Goal = new Vector2Int(x, y);
			}
		}
	}

	public bool IsWall(int x, int y)
	{
		// 맵 밖은 벽으로 친다. 그래야 가장자리에 불필요한 외곽선이 생기지 않는다.
		if (x < 0 || y < 0 || x >= Width || y >= Height) return true;
		return Wall[x, y];
	}

	public void Paint(Tilemap tilemap, Vector2Int origin)
	{
		List<Vector3Int> positions = new List<Vector3Int>();
		List<TileBase> tiles = new List<TileBase>();

		for (int y = 0; y < Height; y++)
		{
			for (int x = 0; x < Width; x++)
			{
				if (!Wall[x, y]) continue;
				positions.Add(new Vector3Int(origin.x + x, origin.y + y, 0));
				tiles.Add(GetTile(StageAutotile.Resolve(x, y, IsWall)));
			}
		}

		// 한 칸씩 SetTile 하면 매번 콜라이더가 다시 만들어진다. 한 번에 넘긴다.
		tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
	}

	/// <summary>
	/// 스프라이트 번호로 Tile을 만든다. 같은 번호는 재사용한다.
	/// 타일 시트는 Resources에 둬서 런타임에 불러올 수 있게 했다.
	/// </summary>
	static TileBase GetTile(int spriteIndex)
	{
		if (tileCache.TryGetValue(spriteIndex, out TileBase cached)) return cached;

		if (sprites == null) LoadSprites();

		Sprite sprite = (spriteIndex >= 0 && spriteIndex < sprites.Length) ? sprites[spriteIndex] : null;
		if (sprite == null)
		{
			// 스프라이트가 없으면 눈에는 안 보이는데 콜라이더만 남아
			// "투명한 벽"이 된다. 조용히 넘어가면 원인을 못 찾는다.
			Debug.LogError($"[StageBuilder] 스프라이트 {spriteIndex}번을 찾지 못했습니다. (불러온 개수 {sprites.Length})");
		}

		Tile tile = ScriptableObject.CreateInstance<Tile>();
		tile.sprite = sprite;
		// 아래 네 줄은 원본 Tile 에셋(cavesofgallet_tiles_*.asset)의 값과 맞춘 것이다.
		// 에디터로 만든 에셋은 이 값들이 직렬화돼 있지만
		// CreateInstance로 만든 Tile은 기본값에 의존하게 되므로 명시한다.
		tile.color = Color.white;
		tile.transform = Matrix4x4.identity;
		tile.flags = TileFlags.LockColor;
		tile.colliderType = Tile.ColliderType.Sprite;
		tileCache[spriteIndex] = tile;
		return tile;
	}

	static void LoadSprites()
	{
		Sprite[] loaded = Resources.LoadAll<Sprite>("Tiles/cavesofgallet_tiles");

		// 배열 크기는 불러온 개수가 아니라 이름에 붙은 최대 번호로 잡아야 한다.
		// 개수로 잡으면 번호가 큰 스프라이트가 조용히 누락된다.
		int max = -1;
		for (int i = 0; i < loaded.Length; i++)
		{
			if (TryParseIndex(loaded[i].name, out int n)) max = Mathf.Max(max, n);
		}

		sprites = new Sprite[max + 1];
		// LoadAll의 순서는 보장되지 않으므로 이름 뒤 숫자로 자리를 잡는다.
		for (int i = 0; i < loaded.Length; i++)
		{
			if (TryParseIndex(loaded[i].name, out int n)) sprites[n] = loaded[i];
		}

		if (loaded.Length == 0) Debug.LogError("[StageBuilder] Resources/Tiles/cavesofgallet_tiles 를 불러오지 못했습니다.");
	}

	static bool TryParseIndex(string name, out int index)
	{
		index = -1;
		int underscore = name.LastIndexOf('_');
		if (underscore < 0) return false;
		return int.TryParse(name.Substring(underscore + 1), out index) && index >= 0;
	}
}
