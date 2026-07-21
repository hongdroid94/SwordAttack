using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Decoration 타일맵의 타일을 배경이 비치는 버전으로 바꾼다.
///
/// cavesofgallet 타일셋은 "장식은 암반 위에 놓인다"는 전제로 그려져 있어서,
/// 597개 장식 타일이 전부 불투명한 8x8 정사각형이고 대부분 암반색(33,38,63)을
/// 배경으로 깔고 있다. 예전에는 플레이 배경이 없어 카메라 클리어 컬러가
/// 같은 색이었기 때문에 그 네모가 보이지 않았을 뿐이다.
///
/// 패럴랙스 배경이 생기면서 공중에 떠 있는 장식들이 어두운 네모로 드러났다.
/// 암반색만 투명하게 뺀 시트(Resources/Tiles/cavesofgallet_deco)를 써서
/// 같은 스프라이트 번호로 갈아끼운다. 암반에 붙은 장식은 뒤에 실제 암반이
/// 있으므로 보이는 결과가 같고, 공중에 뜬 장식만 장식 모양대로 보이게 된다.
/// </summary>
public static class DecorationTiles
{
	static Sprite[] sprites;
	static readonly Dictionary<int, TileBase> cache = new();


	public static void Apply(Tilemap map)
	{
		if (map == null) return;
		if (!LoadSprites()) return;

		map.CompressBounds();
		BoundsInt bounds = map.cellBounds;
		TileBase[] block = map.GetTilesBlock(bounds);

		int replaced = 0;
		for (int i = 0; i < block.Length; i++)
		{
			if (block[i] is not Tile tile || tile.sprite == null) continue;

			int index = ParseIndex(tile.sprite.name);
			if (index < 0 || index >= sprites.Length || sprites[index] == null) continue;

			block[i] = GetTile(index);
			replaced++;
		}

		// 한 칸씩 바꾸면 콜라이더가 매번 다시 생긴다. 한 번에 넘긴다.
		if (replaced > 0) map.SetTilesBlock(bounds, block);
	}

	static bool LoadSprites()
	{
		if (sprites != null) return sprites.Length > 0;

		Sprite[] loaded = Resources.LoadAll<Sprite>("Tiles/cavesofgallet_deco");
		int max = -1;
		for (int i = 0; i < loaded.Length; i++)
		{
			int n = ParseIndex(loaded[i].name);
			if (n > max) max = n;
		}

		sprites = new Sprite[max + 1];
		for (int i = 0; i < loaded.Length; i++)
		{
			int n = ParseIndex(loaded[i].name);
			if (n >= 0) sprites[n] = loaded[i];
		}

		if (loaded.Length == 0) Debug.LogWarning("[DecorationTiles] Resources/Tiles/cavesofgallet_deco 를 불러오지 못했습니다.");
		return loaded.Length > 0;
	}

	static TileBase GetTile(int index)
	{
		if (cache.TryGetValue(index, out TileBase cached)) return cached;

		Tile tile = ScriptableObject.CreateInstance<Tile>();
		tile.sprite = sprites[index];
		tile.color = Color.white;
		tile.transform = Matrix4x4.identity;
		tile.flags = TileFlags.LockColor;
		// 장식 레이어는 원래 충돌이 없다.
		tile.colliderType = Tile.ColliderType.None;
		cache[index] = tile;
		return tile;
	}

	static int ParseIndex(string name)
	{
		int underscore = name.LastIndexOf('_');
		if (underscore < 0) return -1;
		return int.TryParse(name.Substring(underscore + 1), out int n) ? n : -1;
	}
}
