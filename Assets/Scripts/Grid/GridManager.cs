using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameBalanceConfig config;
    [SerializeField] private GameObject tilePrefab;

    private readonly Dictionary<(int, int), TileView2D> _tileViews = new();

    public int CurrentFloor { get; private set; }
    public int CurrentSaveId { get; private set; }

    public void LoadFloor(int saveId, int floor)
    {
        // 记录当前加载的是哪个存档、哪一层
        CurrentSaveId = saveId;
        CurrentFloor = floor;

        ClearAll();

        // 读取楼层全部格子并实例化显示
        List<TileData> tiles = SqliteDb.Instance.LoadFloorTiles(saveId, floor);
        foreach (var tile in tiles)
        {
            var go = Instantiate(tilePrefab, GridToWorld(tile.x, tile.y), Quaternion.identity, transform);
            var view = go.GetComponent<TileView2D>();
            view.SetData(tile);
            _tileViews[(tile.x, tile.y)] = view;
        }
    }

    public void RefreshTile(int x, int y)
    {
        // 单独刷新一个格子，适合怪物死亡、开门、捡道具等情况
        var data = SqliteDb.Instance.GetTile(CurrentSaveId, CurrentFloor, x, y);
        if (data == null) return;

        if (_tileViews.TryGetValue((x, y), out var view))
        {
            view.SetData(data);
        }
    }

    public TileData GetTile(int x, int y)
    {
        // 从数据库读取当前层指定格子
        return SqliteDb.Instance.GetTile(CurrentSaveId, CurrentFloor, x, y);
    }

    public Vector3 GridToWorld(int x, int y)
    {
        // 格子坐标直接映射到世界坐标，原型阶段最简单
        return new Vector3(x, y, 0f);
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < config.mapWidth && y >= 0 && y < config.mapHeight;
    }

    private void ClearAll()
    {
        // 清空当前层已经生成的全部格子对象
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        _tileViews.Clear();
    }
}