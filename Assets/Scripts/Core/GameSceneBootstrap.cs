using UnityEngine;

public class GameSceneBootstrap : MonoBehaviour
{
    [SerializeField] private GameBalanceConfig config;
    [SerializeField] private FloorGenerator floorGenerator;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private HudController hudController;

    private void Start()
    {
        // 读取从主菜单传入的存档槽位
        int saveId = SessionState.SelectedSaveId;

        // 如果 slot 不存在存档，则创建一个默认存档
        if (!SqliteDb.Instance.SaveExists(saveId))
        {
            SqliteDb.Instance.RebuildMonsterDefs(config);
            SqliteDb.Instance.CreateNewSave(saveId, $"Save_{saveId}", config);
        }

        var save = SqliteDb.Instance.LoadSave(saveId);

        // 如果当前楼层尚未生成，则先生成楼层
        if (!SqliteDb.Instance.FloorExists(saveId, save.floor))
        {
            floorGenerator.GenerateFloor(saveId, save.floor);
        }

        // 按存档楼层加载地图和玩家
        gridManager.LoadFloor(saveId, save.floor);
        playerController.LoadCurrentSave(saveId);

        // 初始刷新 HUD
        hudController.RefreshPlayerHud();
    }
}