using UnityEngine;

public class BattleController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private MerchantUI merchantUI;

    private void Update()
    {
        // 暂停时不处理点击输入
        if (Time.timeScale == 0f) return;

        if (Input.GetMouseButtonDown(0))
        {
            TryClickWorld();
        }
    }

    private void TryClickWorld()
    {
        // 屏幕坐标转世界坐标，并向 2D 世界发射点击检测
        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(world.x, world.y);
        RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero);

        if (!hit.collider) return;

        var tileView = hit.collider.GetComponent<TileView2D>();
        if (tileView == null) return;

        var tile = gridManager.GetTile(tileView.x, tileView.y);
        if (tile == null) return;

        // 点到商人：要求玩家与商人相邻，然后弹出商店
        if (tile.tileType == TileType.Merchant)
        {
            if (playerController.IsAdjacent(tile.x, tile.y))
            {
                merchantUI.Open();
            }
            return;
        }

        // 非怪物不做战斗
        if (tile.tileType != TileType.Monster) return;

        // 只有与怪物相邻时才能攻击
        if (!playerController.IsAdjacent(tile.x, tile.y)) return;

        var player = playerController.CurrentSave;
        MonsterDef monster = SqliteDb.Instance.GetMonsterDef(tile.contentId);
        if (monster == null) return;

        // 玩家攻击力不大于怪物防御时，无法造成伤害
        if (player.attack <= monster.defense)
        {
            return;
        }

        // 单次点击交换一轮伤害
        int damageToPlayer = Mathf.Max(monster.attack - player.defense, 0);
        int damageToMonster = Mathf.Max(player.attack - monster.defense, 0);

        player.health -= damageToPlayer;
        if (player.health < 0) player.health = 0;

        tile.hp -= damageToMonster;

        if (tile.hp <= 0)
        {
            // 怪物死亡：给金币；如果它携带钥匙则发钥匙
            player.gold += monster.goldReward;

            if (!string.IsNullOrEmpty(tile.stateJson) && tile.stateJson.Contains("drop_key=1"))
            {
                player.keys += 1;
            }

            // 怪物格清空为地板
            tile.tileType = TileType.Floor;
            tile.contentId = "";
            tile.isCleared = 1;
            tile.hp = 0;
            tile.stateJson = "";

            SqliteDb.Instance.UpdateTile(tile);

            // 玩家移动到怪物原位置
            playerController.MoveDirectTo(tile.x, tile.y);
        }
        else
        {
            // 怪物没死，写回剩余 HP；同时保存玩家掉血结果
            SqliteDb.Instance.UpdateTile(tile);
            playerController.SaveNow();
            PlayerController.NotifyPlayerDataChanged();
        }

        if (player.health <= 0)
        {
            Debug.Log("Player died. Death screen is not implemented in this prototype.");
        }

        // 刷新该格的显示颜色/信息
        gridManager.RefreshTile(tile.x, tile.y);
    }
}