using TMPro;
using UnityEngine;

public class BattlePreviewController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerController playerController;

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text textInfo;

    private void Update()
    {
        // 如果玩家数据尚未加载，则隐藏预览面板
        if (playerController.CurrentSave == null)
        {
            panel.SetActive(false);
            return;
        }

        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(world.x, world.y);
        RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero);

        if (!hit.collider)
        {
            panel.SetActive(false);
            return;
        }

        var tileView = hit.collider.GetComponent<TileView2D>();
        if (tileView == null)
        {
            panel.SetActive(false);
            return;
        }

        var tile = gridManager.GetTile(tileView.x, tileView.y);
        if (tile == null || tile.tileType != TileType.Monster)
        {
            panel.SetActive(false);
            return;
        }

        // 只有当玩家与怪物相邻时，才显示战斗预测
        if (!playerController.IsAdjacent(tile.x, tile.y))
        {
            panel.SetActive(false);
            return;
        }

        var player = playerController.CurrentSave;
        var monster = SqliteDb.Instance.GetMonsterDef(tile.contentId);
        if (monster == null)
        {
            panel.SetActive(false);
            return;
        }

        panel.SetActive(true);

        // 无法破防时直接提示无法战胜
        if (player.attack <= monster.defense)
        {
            textInfo.text =
                $"Enemy: {monster.name}\n" +
                $"Enemy HP: {tile.hp}  ATK: {monster.attack}  DEF: {monster.defense}\n\n" +
                $"Player HP: {player.health}/{player.maxHealth}  ATK: {player.attack}  DEF: {player.defense}  G: {player.gold}\n\n" +
                "Cannot win";
            return;
        }

        // 预测一次点击后的数值变化
        int damageToPlayer = Mathf.Max(monster.attack - player.defense, 0);
        int damageToMonster = Mathf.Max(player.attack - monster.defense, 0);
        int remainingMonsterHp = tile.hp - damageToMonster;
        bool kill = remainingMonsterHp <= 0;

        string result = kill
            ? $"After this hit: Player -{damageToPlayer} HP, +{monster.goldReward} G, kill and move"
            : $"After this hit: Player -{damageToPlayer} HP, Enemy -{damageToMonster} HP";

        textInfo.text =
            $"Enemy: {monster.name}\n" +
            $"Enemy HP: {tile.hp}  ATK: {monster.attack}  DEF: {monster.defense}\n\n" +
            $"Player HP: {player.health}/{player.maxHealth}  ATK: {player.attack}  DEF: {player.defense}  G: {player.gold}\n\n" +
            result;
    }
}