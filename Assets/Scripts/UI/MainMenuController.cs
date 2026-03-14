using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private GameBalanceConfig config;

    [Header("Panels")]
    [SerializeField] private GameObject panelMain;
    [SerializeField] private GameObject panelNewGame;
    [SerializeField] private GameObject panelLoadGame;

    [Header("New Game UI")]
    [SerializeField] private TMP_InputField saveNameInput;
    [SerializeField] private Button btnNewSlot1;
    [SerializeField] private Button btnNewSlot2;
    [SerializeField] private Button btnNewSlot3;
    [SerializeField] private TMP_Text txtNewSlot1;
    [SerializeField] private TMP_Text txtNewSlot2;
    [SerializeField] private TMP_Text txtNewSlot3;

    [Header("Load UI")]
    [SerializeField] private Button btnLoadSlot1;
    [SerializeField] private Button btnLoadSlot2;
    [SerializeField] private Button btnLoadSlot3;
    [SerializeField] private TMP_Text txtLoadSlot1;
    [SerializeField] private TMP_Text txtLoadSlot2;
    [SerializeField] private TMP_Text txtLoadSlot3;

    // 记录从 Load 面板点击到的空槽位，当前版本只做记录，后续可用于高亮 UI
    private int _preferredNewGameSlot = -1;

    private void Start()
    {
        // 场景进入时默认显示主菜单
        ShowMain();
    }

    public void ShowMain()
    {
        panelMain.SetActive(true);
        panelNewGame.SetActive(false);
        panelLoadGame.SetActive(false);

        // 每次回到主菜单时刷新 slot 显示内容
        RefreshAllSlotTexts();
    }

    public void ShowNewGame()
    {
        panelMain.SetActive(false);
        panelNewGame.SetActive(true);
        panelLoadGame.SetActive(false);

        // 进入 New Game 面板时刷新 slot 文本
        RefreshNewGameSlots();
    }

    public void ShowLoadGame()
    {
        panelMain.SetActive(false);
        panelNewGame.SetActive(false);
        panelLoadGame.SetActive(true);

        // 进入 Load 面板时刷新 slot 文本
        RefreshLoadSlots();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ClickNewSlot1() => StartNewGameInSlot(1);
    public void ClickNewSlot2() => StartNewGameInSlot(2);
    public void ClickNewSlot3() => StartNewGameInSlot(3);

    public void ClickLoadSlot1() => LoadOrRedirectToNewGame(1);
    public void ClickLoadSlot2() => LoadOrRedirectToNewGame(2);
    public void ClickLoadSlot3() => LoadOrRedirectToNewGame(3);

    private void StartNewGameInSlot(int slot)
    {
        // 读取输入框里的存档名；为空时给一个默认名
        string saveName = saveNameInput.text.Trim();
        if (string.IsNullOrEmpty(saveName))
        {
            saveName = $"Save_{slot}";
        }

        // 重建怪物定义表，保证 Inspector 配置同步到数据库
        SqliteDb.Instance.RebuildMonsterDefs(config);

        // 创建新存档；如果该 slot 已有存档，则会覆盖
        SqliteDb.Instance.CreateNewSave(slot, saveName, config);

        // 记录当前进入的存档槽位
        SessionState.SelectedSaveId = slot;

        // 切到正式游戏场景
        SceneManager.LoadScene("GameScene");
    }

    private void LoadOrRedirectToNewGame(int slot)
    {
        // 如果该 slot 有存档，则直接进入
        if (SqliteDb.Instance.SaveExists(slot))
        {
            SessionState.SelectedSaveId = slot;
            SceneManager.LoadScene("GameScene");
            return;
        }

        // 如果该 slot 没有存档，则转入 New Game 流程
        _preferredNewGameSlot = slot;
        ShowNewGame();

        // 自动填一个默认存档名，减少输入步骤
        if (string.IsNullOrWhiteSpace(saveNameInput.text))
        {
            saveNameInput.text = $"Save_{slot}";
        }
    }

    private void RefreshAllSlotTexts()
    {
        RefreshNewGameSlots();
        RefreshLoadSlots();
    }

    private void RefreshNewGameSlots()
    {
        SetNewSlotText(txtNewSlot1, 1);
        SetNewSlotText(txtNewSlot2, 2);
        SetNewSlotText(txtNewSlot3, 3);
    }

    private void RefreshLoadSlots()
    {
        SetLoadSlotText(txtLoadSlot1, 1);
        SetLoadSlotText(txtLoadSlot2, 2);
        SetLoadSlotText(txtLoadSlot3, 3);
    }

    private void SetNewSlotText(TMP_Text textComp, int slot)
    {
        if (textComp == null) return;

        // New Game 面板里，已有存档显示为 overwrite 提示
        if (SqliteDb.Instance.SaveExists(slot))
        {
            var save = SqliteDb.Instance.LoadSave(slot);
            textComp.text = $"Slot {slot} (Overwrite)\n{save.saveName} - Floor {save.floor}";
        }
        else
        {
            textComp.text = $"Slot {slot}\nEmpty Slot";
        }
    }

    private void SetLoadSlotText(TMP_Text textComp, int slot)
    {
        if (textComp == null) return;

        // Load 面板里，有存档显示存档名和楼层；没有则显示 Empty Slot
        if (SqliteDb.Instance.SaveExists(slot))
        {
            var save = SqliteDb.Instance.LoadSave(slot);
            textComp.text = $"Slot {slot}\n{save.saveName} - Floor {save.floor}";
        }
        else
        {
            textComp.text = $"Slot {slot}\nEmpty Slot";
        }
    }
}