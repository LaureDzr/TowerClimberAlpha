using System.Collections.Generic;
using UnityEngine;

public class FloorGenerator : MonoBehaviour
{
    [SerializeField] private GameBalanceConfig config;

    public void GenerateFloor(int saveId, int floor)
    {
        var tiles = new List<TileData>();
        int width = config.mapWidth;
        int height = config.mapHeight;

        // 1. 先铺满地板
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tiles.Add(new TileData
                {
                    saveId = saveId,
                    floor = floor,
                    x = x,
                    y = y,
                    tileType = TileType.Floor,
                    contentId = "",
                    isCleared = 0,
                    hp = 0,
                    stateJson = ""
                });
            }
        }

        // 2. 外圈变成墙
        for (int x = 0; x < width; x++)
        {
            SetTile(tiles, saveId, floor, x, 0, TileType.Wall);
            SetTile(tiles, saveId, floor, x, height - 1, TileType.Wall);
        }
        for (int y = 0; y < height; y++)
        {
            SetTile(tiles, saveId, floor, 0, y, TileType.Wall);
            SetTile(tiles, saveId, floor, width - 1, y, TileType.Wall);
        }

        // 3. 宝物区外围墙
        for (int x = config.treasureRoomMinX; x <= config.treasureRoomMaxX; x++)
        {
            for (int y = config.treasureRoomMinY; y <= config.treasureRoomMaxY; y++)
            {
                bool isEdge =
                    x == config.treasureRoomMinX || x == config.treasureRoomMaxX ||
                    y == config.treasureRoomMinY || y == config.treasureRoomMaxY;

                if (isEdge)
                {
                    SetTile(tiles, saveId, floor, x, y, TileType.Wall);
                }
            }
        }

        // 4. 宝物区入口门
        SetTile(tiles, saveId, floor, config.treasureDoorX, config.treasureDoorY, TileType.Door);

        // 5. 楼层入口：第一层为出生点，其他层为上楼点
        if (floor == 1)
        {
            SetTile(tiles, saveId, floor, config.spawnX, config.spawnY, TileType.Spawn);
        }
        else
        {
            SetTile(tiles, saveId, floor, config.spawnX, config.spawnY, TileType.StairsUp);
        }

        // 6. 宝物区内放宝物和下楼传送点
        SetTile(tiles, saveId, floor, config.treasureItemX, config.treasureItemY, TileType.TreasureItem);
        SetTile(tiles, saveId, floor, config.stairsDownX, config.stairsDownY, TileType.StairsDown);

        // 7. 收集非宝物区的可生成怪物候选点
        var candidates = new List<Vector2Int>();
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                if (IsTreasureArea(x, y)) continue;
                if (x == config.spawnX && y == config.spawnY) continue;
                if (x == config.treasureDoorX && y == config.treasureDoorY) continue;

                var t = FindTile(tiles, x, y);
                if (t != null && t.tileType == TileType.Floor)
                {
                    candidates.Add(new Vector2Int(x, y));
                }
            }
        }

        Shuffle(candidates);

        bool bossFloor = floor % config.bossEveryFloors == 0;
        int placed = 0;

        // 8. Boss 层：在宝物区门前固定放 Boss
        if (bossFloor)
        {
            int bossX = config.treasureDoorX - 1;
            int bossY = config.treasureDoorY;

            SetMonster(tiles, saveId, floor, bossX, bossY, "boss", config.bossHp, "");
        }

        // 9. 刷普通怪 / 精英怪
        for (int i = 0; i < candidates.Count && placed < config.normalMonsterCount; i++)
        {
            var p = candidates[i];

            if (bossFloor && p.x == config.treasureDoorX - 1 && p.y == config.treasureDoorY)
                continue;

            string monsterId = Random.value < config.eliteReplaceChance ? "elite" : "normal";
            int hp = monsterId == "elite" ? config.eliteHp : config.normalHp;

            SetMonster(tiles, saveId, floor, p.x, p.y, monsterId, hp, "");
            placed++;
        }

        // 10. Boss 层：随机指定一个非 Boss 怪掉钥匙
        if (bossFloor)
        {
            var monsterTiles = new List<TileData>();
            foreach (var t in tiles)
            {
                if (t.tileType == TileType.Monster && t.contentId != "boss")
                {
                    monsterTiles.Add(t);
                }
            }

            if (monsterTiles.Count > 0)
            {
                int idx = Random.Range(0, monsterTiles.Count);
                monsterTiles[idx].stateJson = "drop_key=1";
            }
        }
        else
        {
            // 非 Boss 层直接在地上刷一把钥匙
            foreach (var p in candidates)
            {
                var tile = FindTile(tiles, p.x, p.y);
                if (tile != null && tile.tileType == TileType.Floor)
                {
                    SetTile(tiles, saveId, floor, p.x, p.y, TileType.Key);
                    break;
                }
            }
        }

        // 11. 随机刷商人
        if (Random.value < config.merchantSpawnChance)
        {
            foreach (var p in candidates)
            {
                var tile = FindTile(tiles, p.x, p.y);
                if (tile != null && tile.tileType == TileType.Floor)
                {
                    SetTile(tiles, saveId, floor, p.x, p.y, TileType.Merchant, "merchant_basic");
                    break;
                }
            }
        }

        // 12. 保存整层数据
        SqliteDb.Instance.SaveFloorTiles(saveId, floor, tiles);
    }

    private bool IsTreasureArea(int x, int y)
    {
        return x >= config.treasureRoomMinX && x <= config.treasureRoomMaxX &&
               y >= config.treasureRoomMinY && y <= config.treasureRoomMaxY;
    }

    private TileData FindTile(List<TileData> tiles, int x, int y)
    {
        return tiles.Find(t => t.x == x && t.y == y);
    }

    private void SetTile(List<TileData> tiles, int saveId, int floor, int x, int y, TileType type, string contentId = "")
    {
        var t = FindTile(tiles, x, y);
        if (t == null) return;

        t.saveId = saveId;
        t.floor = floor;
        t.tileType = type;
        t.contentId = contentId;
        t.isCleared = 0;
        t.hp = 0;
        t.stateJson = "";
    }

    private void SetMonster(List<TileData> tiles, int saveId, int floor, int x, int y, string monsterId, int hp, string state)
    {
        var t = FindTile(tiles, x, y);
        if (t == null) return;

        t.saveId = saveId;
        t.floor = floor;
        t.tileType = TileType.Monster;
        t.contentId = monsterId;
        t.isCleared = 0;
        t.hp = hp;
        t.stateJson = state;
    }

    private void Shuffle(List<Vector2Int> list)
    {
        // 洗牌，保证随机分布
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}