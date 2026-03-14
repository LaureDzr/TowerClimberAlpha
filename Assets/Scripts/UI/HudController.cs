using TMPro;
using UnityEngine;

public class HudController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerController playerController;

    [Header("Player UI")]
    [SerializeField] private TMP_Text playerStatsText;
    [SerializeField] private TMP_Text floorText;

    [Header("Monster Hover UI")]
    [SerializeField] private GameObject monsterInfoPanel;
    [SerializeField] private TMP_Text monsterInfoText;

    private void OnEnable()
    {
        // 订阅玩家数据变化事件，用于自动刷新 HUD
        PlayerController.OnPlayerDataChanged += RefreshPlayerHud;
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerDataChanged -= RefreshPlayerHud;
    }

    private void Start()
    {
        RefreshPlayerHud();
    }

    private void Update()
    {
        // 每帧更新鼠标悬停怪物信息
        RefreshMonsterHover();
    }

    public void RefreshPlayerHud()
    {
        var p = playerController.CurrentSave;
        if (p == null) return;

        playerStatsText.text =
            $"Items: Key x{p.keys}\n" +
            $"HP: {p.health}/{p.maxHealth}\n" +
            $"Attack: {p.attack}\n" +
            $"Armor: {p.defense}\n" +
            $"Gold: {p.gold}";

        floorText.text = $"Floor {p.floor}";
    }

    private void RefreshMonsterHover()
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(world.x, world.y);
        RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero);

        if (!hit.collider)
        {
            monsterInfoPanel.SetActive(false);
            return;
        }

        var tileView = hit.collider.GetComponent<TileView2D>();
        if (tileView == null)
        {
            monsterInfoPanel.SetActive(false);
            return;
        }

        var tile = gridManager.GetTile(tileView.x, tileView.y);
        if (tile == null || tile.tileType != TileType.Monster)
        {
            monsterInfoPanel.SetActive(false);
            return;
        }

        var monster = SqliteDb.Instance.GetMonsterDef(tile.contentId);
        if (monster == null)
        {
            monsterInfoPanel.SetActive(false);
            return;
        }

        // 鼠标悬停在怪物上时，显示怪物当前信息
        monsterInfoPanel.SetActive(true);
        monsterInfoText.text =
            $"{monster.name}\n" +
            $"HP: {tile.hp}\n" +
            $"Attack: {monster.attack}\n" +
            $"Armor: {monster.defense}\n" +
            $"Gold drop: {monster.goldReward}";
    }
}