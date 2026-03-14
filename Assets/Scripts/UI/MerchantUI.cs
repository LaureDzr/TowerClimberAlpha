using TMPro;
using UnityEngine;

public class MerchantUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameBalanceConfig config;
    [SerializeField] private TMP_Text descText;

    public void Open()
    {
        // 打开商店面板并刷新当前价格/玩家金币显示
        panel.SetActive(true);
        RefreshText();
    }

    public void Close()
    {
        // 关闭商店面板
        panel.SetActive(false);
    }

    public void BuyHeal()
    {
        var p = playerController.CurrentSave;
        if (p.gold < config.healCostGold) return;

        // 花金币回血，但不会超过最大生命
        p.gold -= config.healCostGold;
        p.health += config.healAmount;
        if (p.health > p.maxHealth) p.health = p.maxHealth;

        playerController.SaveNow();
        PlayerController.NotifyPlayerDataChanged();
        RefreshText();
    }

    public void BuyAttack()
    {
        var p = playerController.CurrentSave;
        if (p.gold < config.atkCostGold) return;

        // 花金币提升攻击
        p.gold -= config.atkCostGold;
        p.attack += config.atkGain;

        playerController.SaveNow();
        PlayerController.NotifyPlayerDataChanged();
        RefreshText();
    }

    public void BuyDefense()
    {
        var p = playerController.CurrentSave;
        if (p.gold < config.defCostGold) return;

        // 花金币提升防御
        p.gold -= config.defCostGold;
        p.defense += config.defGain;

        playerController.SaveNow();
        PlayerController.NotifyPlayerDataChanged();
        RefreshText();
    }

    private void RefreshText()
    {
        if (descText == null || playerController.CurrentSave == null) return;

        var p = playerController.CurrentSave;
        descText.text =
            $"Gold: {p.gold}\n" +
            $"Heal: {config.healCostGold} G -> +{config.healAmount} HP\n" +
            $"Attack: {config.atkCostGold} G -> +{config.atkGain} ATK\n" +
            $"Defense: {config.defCostGold} G -> +{config.defGain} DEF";
    }
}