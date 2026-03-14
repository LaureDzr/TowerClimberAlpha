using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using UnityEngine;

public class SqliteDb : MonoBehaviour
{
    public static SqliteDb Instance { get; private set; }

    private string _dbPath;
    private string _connString;

    private void Awake()
    {
        // 单例初始化：场景中只保留一个数据库管理器
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 数据库文件放在 persistentDataPath，便于本地保存
        _dbPath = Path.Combine(Application.persistentDataPath, "game.db");
        _connString = "URI=file:" + _dbPath;

        Debug.Log("Database path: " + _dbPath);

        Init();
    }

    public void Init()
    {
        // 初始化数据库表结构
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS save_profile (
    save_id      INTEGER PRIMARY KEY,
    save_name    TEXT NOT NULL,
    health       INTEGER NOT NULL,
    max_health   INTEGER NOT NULL,
    attack       INTEGER NOT NULL,
    defense      INTEGER NOT NULL,
    gold         INTEGER NOT NULL,
    keys         INTEGER NOT NULL,
    floor        INTEGER NOT NULL,
    x            INTEGER NOT NULL,
    y            INTEGER NOT NULL,
    updated_at   TEXT NOT NULL
);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS monster_def (
    monster_id    TEXT PRIMARY KEY,
    name          TEXT NOT NULL,
    kind          TEXT NOT NULL,
    health        INTEGER NOT NULL,
    attack        INTEGER NOT NULL,
    defense       INTEGER NOT NULL,
    gold_reward   INTEGER NOT NULL
);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS floor_tiles_v2 (
    save_id      INTEGER NOT NULL,
    floor        INTEGER NOT NULL,
    x            INTEGER NOT NULL,
    y            INTEGER NOT NULL,
    tile_type    TEXT NOT NULL,
    content_id   TEXT NOT NULL DEFAULT '',
    is_cleared   INTEGER NOT NULL DEFAULT 0,
    hp           INTEGER NOT NULL DEFAULT 0,
    state_json   TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (save_id, floor, x, y)
);";
        cmd.ExecuteNonQuery();
    }

    public void RebuildMonsterDefs(GameBalanceConfig cfg)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM monster_def;";
        cmd.ExecuteNonQuery();

        InsertMonster(cmd, "normal", "Normal", "normal", cfg.normalHp, cfg.normalAtk, cfg.normalDef, cfg.normalGold);
        InsertMonster(cmd, "elite", "Elite", "elite", cfg.eliteHp, cfg.eliteAtk, cfg.eliteDef, cfg.eliteGold);
        InsertMonster(cmd, "boss", "Boss", "boss", cfg.bossHp, cfg.bossAtk, cfg.bossDef, cfg.bossGold);

        tx.Commit();
    }

    private void InsertMonster(IDbCommand cmd, string id, string name, string kind, int hp, int atk, int def, int gold)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = @"
INSERT INTO monster_def (monster_id, name, kind, health, attack, defense, gold_reward)
VALUES (@id, @name, @kind, @hp, @atk, @def, @gold);";

        AddParam(cmd, "@id", id);
        AddParam(cmd, "@name", name);
        AddParam(cmd, "@kind", kind);
        AddParam(cmd, "@hp", hp);
        AddParam(cmd, "@atk", atk);
        AddParam(cmd, "@def", def);
        AddParam(cmd, "@gold", gold);
        cmd.ExecuteNonQuery();
    }

    public bool SaveExists(int saveId)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM save_profile WHERE save_id=@id;";
        AddParam(cmd, "@id", saveId);

        var count = Convert.ToInt32(cmd.ExecuteScalar());
        return count > 0;
    }

    public void CreateNewSave(int saveId, string saveName, GameBalanceConfig cfg)
    {
        DeleteSave(saveId);

        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO save_profile
(save_id, save_name, health, max_health, attack, defense, gold, keys, floor, x, y, updated_at)
VALUES
(@save_id, @save_name, @health, @max_health, @attack, @defense, @gold, @keys, @floor, @x, @y, @updated_at);";

        AddParam(cmd, "@save_id", saveId);
        AddParam(cmd, "@save_name", saveName);
        AddParam(cmd, "@health", cfg.playerStartHp);
        AddParam(cmd, "@max_health", cfg.playerStartMaxHp);
        AddParam(cmd, "@attack", cfg.playerStartAtk);
        AddParam(cmd, "@defense", cfg.playerStartDef);
        AddParam(cmd, "@gold", cfg.playerStartGold);
        AddParam(cmd, "@keys", cfg.playerStartKeys);
        AddParam(cmd, "@floor", 1);
        AddParam(cmd, "@x", cfg.spawnX);
        AddParam(cmd, "@y", cfg.spawnY);
        AddParam(cmd, "@updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();
    }

    public void DeleteSave(int saveId)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM save_profile WHERE save_id=@id;";
        AddParam(cmd, "@id", saveId);
        cmd.ExecuteNonQuery();

        cmd.Parameters.Clear();
        cmd.CommandText = "DELETE FROM floor_tiles_v2 WHERE save_id=@id;";
        AddParam(cmd, "@id", saveId);
        cmd.ExecuteNonQuery();

        tx.Commit();
    }

    public SaveProfile LoadSave(int saveId)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT save_id, save_name, health, max_health, attack, defense, gold, keys, floor, x, y, updated_at
FROM save_profile
WHERE save_id=@id;";
        AddParam(cmd, "@id", saveId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new SaveProfile
        {
            saveId = reader.GetInt32(0),
            saveName = reader.GetString(1),
            health = reader.GetInt32(2),
            maxHealth = reader.GetInt32(3),
            attack = reader.GetInt32(4),
            defense = reader.GetInt32(5),
            gold = reader.GetInt32(6),
            keys = reader.GetInt32(7),
            floor = reader.GetInt32(8),
            x = reader.GetInt32(9),
            y = reader.GetInt32(10),
            updatedAt = reader.GetString(11)
        };
    }

    public List<SaveProfile> GetAllSaves()
    {
        var list = new List<SaveProfile>();

        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT save_id, save_name, health, max_health, attack, defense, gold, keys, floor, x, y, updated_at
FROM save_profile
ORDER BY save_id;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new SaveProfile
            {
                saveId = reader.GetInt32(0),
                saveName = reader.GetString(1),
                health = reader.GetInt32(2),
                maxHealth = reader.GetInt32(3),
                attack = reader.GetInt32(4),
                defense = reader.GetInt32(5),
                gold = reader.GetInt32(6),
                keys = reader.GetInt32(7),
                floor = reader.GetInt32(8),
                x = reader.GetInt32(9),
                y = reader.GetInt32(10),
                updatedAt = reader.GetString(11)
            });
        }

        return list;
    }

    public void UpdateSave(SaveProfile save)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE save_profile
SET save_name=@save_name,
    health=@health,
    max_health=@max_health,
    attack=@attack,
    defense=@defense,
    gold=@gold,
    keys=@keys,
    floor=@floor,
    x=@x,
    y=@y,
    updated_at=@updated_at
WHERE save_id=@save_id;";

        AddParam(cmd, "@save_name", save.saveName);
        AddParam(cmd, "@health", save.health);
        AddParam(cmd, "@max_health", save.maxHealth);
        AddParam(cmd, "@attack", save.attack);
        AddParam(cmd, "@defense", save.defense);
        AddParam(cmd, "@gold", save.gold);
        AddParam(cmd, "@keys", save.keys);
        AddParam(cmd, "@floor", save.floor);
        AddParam(cmd, "@x", save.x);
        AddParam(cmd, "@y", save.y);
        AddParam(cmd, "@updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        AddParam(cmd, "@save_id", save.saveId);

        cmd.ExecuteNonQuery();
    }

    public bool FloorExists(int saveId, int floor)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM floor_tiles_v2 WHERE save_id=@save_id AND floor=@floor;";
        AddParam(cmd, "@save_id", saveId);
        AddParam(cmd, "@floor", floor);

        var count = Convert.ToInt32(cmd.ExecuteScalar());
        return count > 0;
    }

    public void SaveFloorTiles(int saveId, int floor, List<TileData> tiles)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM floor_tiles_v2 WHERE save_id=@save_id AND floor=@floor;";
        AddParam(cmd, "@save_id", saveId);
        AddParam(cmd, "@floor", floor);
        cmd.ExecuteNonQuery();

        cmd.Parameters.Clear();
        cmd.CommandText = @"
INSERT INTO floor_tiles_v2
(save_id, floor, x, y, tile_type, content_id, is_cleared, hp, state_json)
VALUES
(@save_id, @floor, @x, @y, @tile_type, @content_id, @is_cleared, @hp, @state_json);";

        foreach (var tile in tiles)
        {
            cmd.Parameters.Clear();
            AddParam(cmd, "@save_id", tile.saveId);
            AddParam(cmd, "@floor", tile.floor);
            AddParam(cmd, "@x", tile.x);
            AddParam(cmd, "@y", tile.y);
            AddParam(cmd, "@tile_type", tile.tileType.ToString());
            AddParam(cmd, "@content_id", tile.contentId ?? "");
            AddParam(cmd, "@is_cleared", tile.isCleared);
            AddParam(cmd, "@hp", tile.hp);
            AddParam(cmd, "@state_json", tile.stateJson ?? "");
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public List<TileData> LoadFloorTiles(int saveId, int floor)
    {
        var list = new List<TileData>();

        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT save_id, floor, x, y, tile_type, content_id, is_cleared, hp, state_json
FROM floor_tiles_v2
WHERE save_id=@save_id AND floor=@floor
ORDER BY y, x;";
        AddParam(cmd, "@save_id", saveId);
        AddParam(cmd, "@floor", floor);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            Enum.TryParse(reader.GetString(4), out TileType tileType);

            list.Add(new TileData
            {
                saveId = reader.GetInt32(0),
                floor = reader.GetInt32(1),
                x = reader.GetInt32(2),
                y = reader.GetInt32(3),
                tileType = tileType,
                contentId = reader.GetString(5),
                isCleared = reader.GetInt32(6),
                hp = reader.GetInt32(7),
                stateJson = reader.GetString(8)
            });
        }

        return list;
    }

    public TileData GetTile(int saveId, int floor, int x, int y)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT save_id, floor, x, y, tile_type, content_id, is_cleared, hp, state_json
FROM floor_tiles_v2
WHERE save_id=@save_id AND floor=@floor AND x=@x AND y=@y;";
        AddParam(cmd, "@save_id", saveId);
        AddParam(cmd, "@floor", floor);
        AddParam(cmd, "@x", x);
        AddParam(cmd, "@y", y);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        Enum.TryParse(reader.GetString(4), out TileType tileType);

        return new TileData
        {
            saveId = reader.GetInt32(0),
            floor = reader.GetInt32(1),
            x = reader.GetInt32(2),
            y = reader.GetInt32(3),
            tileType = tileType,
            contentId = reader.GetString(5),
            isCleared = reader.GetInt32(6),
            hp = reader.GetInt32(7),
            stateJson = reader.GetString(8)
        };
    }

    public void UpdateTile(TileData tile)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE floor_tiles_v2
SET tile_type=@tile_type,
    content_id=@content_id,
    is_cleared=@is_cleared,
    hp=@hp,
    state_json=@state_json
WHERE save_id=@save_id AND floor=@floor AND x=@x AND y=@y;";

        AddParam(cmd, "@tile_type", tile.tileType.ToString());
        AddParam(cmd, "@content_id", tile.contentId ?? "");
        AddParam(cmd, "@is_cleared", tile.isCleared);
        AddParam(cmd, "@hp", tile.hp);
        AddParam(cmd, "@state_json", tile.stateJson ?? "");
        AddParam(cmd, "@save_id", tile.saveId);
        AddParam(cmd, "@floor", tile.floor);
        AddParam(cmd, "@x", tile.x);
        AddParam(cmd, "@y", tile.y);

        cmd.ExecuteNonQuery();
    }

    public MonsterDef GetMonsterDef(string monsterId)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT monster_id, name, kind, health, attack, defense, gold_reward
FROM monster_def
WHERE monster_id=@id;";
        AddParam(cmd, "@id", monsterId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new MonsterDef
        {
            monsterId = reader.GetString(0),
            name = reader.GetString(1),
            kind = reader.GetString(2),
            health = reader.GetInt32(3),
            attack = reader.GetInt32(4),
            defense = reader.GetInt32(5),
            goldReward = reader.GetInt32(6)
        };
    }

    public int CountRemainingMonsters(int saveId, int floor)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT COUNT(*)
FROM floor_tiles_v2
WHERE save_id=@save_id AND floor=@floor AND tile_type='Monster' AND is_cleared=0;";
        AddParam(cmd, "@save_id", saveId);
        AddParam(cmd, "@floor", floor);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int CountRemainingElites(int saveId, int floor)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT COUNT(*)
FROM floor_tiles_v2
WHERE save_id=@save_id AND floor=@floor AND tile_type='Monster' AND content_id='elite' AND is_cleared=0;";
        AddParam(cmd, "@save_id", saveId);
        AddParam(cmd, "@floor", floor);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public bool IsBossAlive(int saveId, int floor)
    {
        using var conn = new SqliteConnection(_connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT COUNT(*)
FROM floor_tiles_v2
WHERE save_id=@save_id AND floor=@floor AND tile_type='Monster' AND content_id='boss' AND is_cleared=0;";
        AddParam(cmd, "@save_id", saveId);
        AddParam(cmd, "@floor", floor);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private void AddParam(IDbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}