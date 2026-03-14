using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TMP_Text saveInfoText;

    private void Update()
    {
        // ESC 打开/关闭暂停菜单
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Toggle();
        }

        // F11 执行手动保存
        if (Input.GetKeyDown(KeyCode.F11))
        {
            ManualSave();
        }
    }

    public void Toggle()
    {
        bool active = !panel.activeSelf;
        panel.SetActive(active);

        // 通过 Time.timeScale 实现简单暂停
        Time.timeScale = active ? 0f : 1f;

        if (active)
        {
            RefreshInfo();
        }
    }

    public void ContinueGame()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ManualSave()
    {
        if (playerController.CurrentSave == null) return;

        // 当前原型里大部分数据本来就是实时保存，F11 用于手动确认存档
        playerController.SaveNow();
        RefreshInfo();
        Debug.Log("Manual save completed.");
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void RefreshInfo()
    {
        var p = playerController.CurrentSave;
        if (p == null) return;

        // 统计当前层怪物信息，显示在暂停面板
        int monsters = SqliteDb.Instance.CountRemainingMonsters(p.saveId, p.floor);
        int elites = SqliteDb.Instance.CountRemainingElites(p.saveId, p.floor);
        bool bossAlive = SqliteDb.Instance.IsBossAlive(p.saveId, p.floor);

        saveInfoText.text =
            $"Save: {p.saveName}\n" +
            $"Floor: {p.floor}\n" +
            $"HP: {p.health}/{p.maxHealth}\n" +
            $"ATK: {p.attack}\n" +
            $"DEF: {p.defense}\n" +
            $"Gold: {p.gold}\n" +
            $"Key: {p.keys}\n\n" +
            $"Monsters: {monsters}\n" +
            $"Elites: {elites}\n" +
            $"Boss Alive: {bossAlive}";
    }
}