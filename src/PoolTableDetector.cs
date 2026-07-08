using Microsoft.Xna.Framework;
using StardewValley;
using xTile.Layers;
using xTile.Tiles;

namespace StardropPoolMinigameRev
{
	internal static class PoolTableDetector
	{
		private static readonly HashSet<int> BuildingTileIds = new()
		{
			1447, 1448, 1449, 1450, 1451,
			1479, 1480, 1481, 1482, 1483
		};

		private static readonly HashSet<int> FrontTileIds = new()
		{
			1415, 1416, 1417, 1418, 1419
		};

		public static bool IsInteractingWithPoolTable(Farmer player, GameLocation location, Vector2 cursorTile)
		{
			if (location.NameOrUniqueName != "Saloon")
			{
				return false;
			}

			Vector2 grabTile = player.GetGrabTile();
			if (IsPoolTableTile(location, (int)grabTile.X, (int)grabTile.Y))
			{
				return true;
			}

			return IsPoolTableTile(location, (int)cursorTile.X, (int)cursorTile.Y);
		}

		private static bool IsPoolTableTile(GameLocation location, int x, int y)
		{
			return HasTileId(location, "Buildings", x, y, BuildingTileIds)
				|| HasTileId(location, "Front", x, y, FrontTileIds);
		}

		private static bool HasTileId(GameLocation location, string layerName, int x, int y, HashSet<int> tileIds)
		{
			Layer? layer = location.Map.GetLayer(layerName);
			if (layer == null || x < 0 || y < 0 || x >= layer.LayerWidth || y >= layer.LayerHeight)
			{
				return false;
			}

			Tile? tile = layer.Tiles[x, y];
			return tile != null && tileIds.Contains(tile.TileIndex);
		}
	}
}
