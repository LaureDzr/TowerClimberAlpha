using UnityEngine;
using Mono.Data.Sqlite;
using System.IO;

public class SqliteSmokeTest : MonoBehaviour
{
    private void Start()
    {
        string dbPath = Path.Combine(Application.persistentDataPath, "smoke.db");
        string connStr = "URI=file:" + dbPath;

        using var conn = new SqliteConnection(connStr);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS t(id INTEGER PRIMARY KEY, name TEXT);";
        cmd.ExecuteNonQuery();

        Debug.Log("SQLite test OK. DB = " + dbPath);
    }
}