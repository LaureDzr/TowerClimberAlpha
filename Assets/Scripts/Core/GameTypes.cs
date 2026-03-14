using System;

public enum TileType
{
    Floor,
    Wall,
    Door,
    Key,
    Monster,
    Merchant,
    TreasureItem,
    StairsUp,
    StairsDown,
    Spawn
}

[Serializable]
public class SaveProfile
{
    public int saveId;
    public string saveName = "";
    public int health;
    public int maxHealth;
    public int attack;
    public int defense;
    public int gold;
    public int keys;
    public int floor;
    public int x;
    public int y;
    public string updatedAt = "";
}

[Serializable]
public class TileData
{
    public int saveId;
    public int floor;
    public int x;
    public int y;
    public TileType tileType;
    public string contentId = "";
    public int isCleared;
    public int hp;
    public string stateJson = "";
}

[Serializable]
public class MonsterDef
{
    public string monsterId = "";
    public string name = "";
    public string kind = "";
    public int health;
    public int attack;
    public int defense;
    public int goldReward;
}