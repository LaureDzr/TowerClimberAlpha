using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // 玩家数据变化事件：HUD、暂停菜单等都可以订阅它
    public static event Action OnPlayerDataChanged;

    [SerializeField] private GameBalanceConfig config;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private FloorGenerator floorGenerator;

    public SaveProfile CurrentSave { get; private set; }

    public static void NotifyPlayerDataChanged()
    {
        // 统一由 PlayerController 内部触发事件，避免外部直接 Invoke 报错
        OnPlayerDataChanged?.Invoke();
    }

    public void LoadCurrentSave(int saveId)
    {
        // 从数据库读取当前存档并同步玩家位置
        CurrentSave = SqliteDb.Instance.LoadSave(saveId);
        transform.position = gridManager.GridToWorld(CurrentSave.x, CurrentSave.y);
        NotifyPlayerDataChanged();
    }

    private void Update()
    {
        if (CurrentSave == null) return;
        if (Time.timeScale == 0f) return;

        // WASD 单步移动
        if (Input.GetKeyDown(KeyCode.W)) TryMove(0, 1);
        else if (Input.GetKeyDown(KeyCode.S)) TryMove(0, -1);
        else if (Input.GetKeyDown(KeyCode.A)) TryMove(-1, 0);
        else if (Input.GetKeyDown(KeyCode.D)) TryMove(1, 0);
    }

    public bool IsAdjacent(int targetX, int targetY)
    {
        // 判断目标格是否与玩家曼哈顿距离为 1
        int dx = Mathf.Abs(CurrentSave.x - targetX);
        int dy = Mathf.Abs(CurrentSave.y - targetY);
        return dx + dy == 1;
    }

    public void MoveDirectTo(int x, int y)
    {
        // 战斗击杀后直接移动到怪物原位置
        CurrentSave.x = x;
        CurrentSave.y = y;
        SaveNow();
        transform.position = gridManager.GridToWorld(x, y);
        NotifyPlayerDataChanged();
    }

    public void SaveNow()
    {
        SqliteDb.Instance.UpdateSave(CurrentSave);
    }

    private void TryMove(int dx, int dy)
    {
        int tx = CurrentSave.x + dx;
        int ty = CurrentSave.y + dy;

        if (!gridManager.IsInBounds(tx, ty)) return;

        var tile = gridManager.GetTile(tx, ty);
        if (tile == null) return;

        switch (tile.tileType)
        {
            case TileType.Wall:
                return;

            case TileType.Monster:
            case TileType.Merchant:
                // 怪物和商人不能直接走上去，必须点击交互
                return;

            case TileType.Door:
                // 有钥匙则自动开门并通过
                if (CurrentSave.keys <= 0) return;

                CurrentSave.keys -= 1;
                tile.tileType = TileType.Floor;
                tile.contentId = "";
                tile.stateJson = "";
                SqliteDb.Instance.UpdateTile(tile);
                gridManager.RefreshTile(tx, ty);

                DoMove(tx, ty);
                break;

            case TileType.Key:
                // 拾取钥匙并清空格子
                CurrentSave.keys += 1;
                tile.tileType = TileType.Floor;
                tile.contentId = "";
                tile.stateJson = "";
                SqliteDb.Instance.UpdateTile(tile);
                gridManager.RefreshTile(tx, ty);

                DoMove(tx, ty);
                break;

            case TileType.TreasureItem:
                // 宝物：增加最大生命并回复对应生命
                CurrentSave.maxHealth += config.treasureBonusMaxHp;
                CurrentSave.health += config.treasureBonusHeal;
                if (CurrentSave.health > CurrentSave.maxHealth)
                    CurrentSave.health = CurrentSave.maxHealth;

                tile.tileType = TileType.Floor;
                tile.contentId = "";
                tile.stateJson = "";
                SqliteDb.Instance.UpdateTile(tile);
                gridManager.RefreshTile(tx, ty);

                DoMove(tx, ty);
                break;

            case TileType.StairsDown:
                // 下楼
                GoToFloor(CurrentSave.floor + 1);
                break;

            case TileType.StairsUp:
                // 第一层的出生点允许正常走；其他层则回上一层
                if (CurrentSave.floor == 1)
                {
                    DoMove(tx, ty);
                }
                else
                {
                    GoToFloor(CurrentSave.floor - 1);
                }
                break;

            case TileType.Spawn:
            case TileType.Floor:
            default:
                DoMove(tx, ty);
                break;
        }
    }

    private void GoToFloor(int targetFloor)
    {
        // 如果目标层还没生成，则先生成
        if (!SqliteDb.Instance.FloorExists(CurrentSave.saveId, targetFloor))
        {
            floorGenerator.GenerateFloor(CurrentSave.saveId, targetFloor);
        }

        // 切层后把玩家放到该层的出生/上楼点
        CurrentSave.floor = targetFloor;
        CurrentSave.x = config.spawnX;
        CurrentSave.y = config.spawnY;
        SaveNow();

        gridManager.LoadFloor(CurrentSave.saveId, CurrentSave.floor);
        transform.position = gridManager.GridToWorld(CurrentSave.x, CurrentSave.y);
        NotifyPlayerDataChanged();
    }

    private void DoMove(int x, int y)
    {
        // 普通移动：写回坐标并同步 Transform
        CurrentSave.x = x;
        CurrentSave.y = y;
        SaveNow();
        transform.position = gridManager.GridToWorld(x, y);
        NotifyPlayerDataChanged();
    }
}