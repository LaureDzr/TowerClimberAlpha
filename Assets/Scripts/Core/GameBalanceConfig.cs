using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "TowerLike/Game Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("Map")]
    public int mapWidth = 15;
    public int mapHeight = 15;
    public int treasureRoomMinX = 9;
    public int treasureRoomMaxX = 13;
    public int treasureRoomMinY = 9;
    public int treasureRoomMaxY = 13;
    public int treasureDoorX = 9;
    public int treasureDoorY = 11;
    public int spawnX = 1;
    public int spawnY = 1;
    public int stairsDownX = 12;
    public int stairsDownY = 11;
    public int treasureItemX = 11;
    public int treasureItemY = 11;

    [Header("Player Start")]
    public int playerStartHp = 100;
    public int playerStartMaxHp = 100;
    public int playerStartAtk = 12;
    public int playerStartDef = 4;
    public int playerStartGold = 0;
    public int playerStartKeys = 0;

    [Header("Treasure Item")]
    public int treasureBonusMaxHp = 20;
    public int treasureBonusHeal = 20;

    [Header("Monster Spawn")]
    public int normalMonsterCount = 8;
    [Range(0f, 1f)] public float eliteReplaceChance = 0.2f;
    [Range(0f, 1f)] public float merchantSpawnChance = 0.25f;
    public int bossEveryFloors = 10;

    [Header("Merchant")]
    public int healCostGold = 10;
    public int healAmount = 30;
    public int atkCostGold = 20;
    public int atkGain = 2;
    public int defCostGold = 20;
    public int defGain = 2;

    [Header("Monster Stats - Normal")]
    public int normalHp = 25;
    public int normalAtk = 8;
    public int normalDef = 2;
    public int normalGold = 5;

    [Header("Monster Stats - Elite")]
    public int eliteHp = 45;
    public int eliteAtk = 12;
    public int eliteDef = 5;
    public int eliteGold = 12;

    [Header("Monster Stats - Boss")]
    public int bossHp = 100;
    public int bossAtk = 18;
    public int bossDef = 8;
    public int bossGold = 40;
}