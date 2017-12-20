using System;
using System.Data.Common;
using System.Data.SQLite;

namespace gtavmm_metro.Models
{
    public class DBInstance
    {
        public SQLiteConnection Connection { get; private set; }
        private static readonly string DbConnectionString = "Data Source={0}\\data.gtavmm-metro;Version=3;";

        public DBInstance(string modsFolderRoot)
        {
            this.Connection = new SQLiteConnection(String.Format(DbConnectionString, modsFolderRoot));

            this.VerifyScriptModTable();
            this.VerifyAssetModTable();
        }

        private async void VerifyScriptModTable()
        {
            await this.Connection.OpenAsync();

            string sql = "SELECT * FROM sqlite_master WHERE type=@type AND name=@name";
            SQLiteCommand command = new SQLiteCommand(sql, this.Connection);
            command.Parameters.AddWithValue("type", "table");
            command.Parameters.AddWithValue("name", "ScriptMod");

            ScriptMod resultScriptMod = new ScriptMod();
            DbDataReader reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                this.Connection.Close();
                this.CreateScriptModTable();
            }
            else
            {
                this.Connection.Close();
            }
        }
        private async void CreateScriptModTable()
        {
            await this.Connection.OpenAsync();

            string sql = @"CREATE TABLE ScriptMod (id INTEGER PRIMARY KEY, name VARCHAR(30), description VARCHAR(600), isEnabled INT, orderIndex INT)";
            SQLiteCommand command = new SQLiteCommand(sql, this.Connection);
            command.ExecuteNonQuery();

            this.Connection.Close();
        }

        private async void VerifyAssetModTable()
        {
            await this.Connection.OpenAsync();

            string sql = "SELECT * FROM sqlite_master WHERE type=@type AND name=@name";
            SQLiteCommand command = new SQLiteCommand(sql, this.Connection);
            command.Parameters.AddWithValue("type", "table");
            command.Parameters.AddWithValue("name", "AssetMod");

            ScriptMod resultScriptMod = new ScriptMod();
            DbDataReader reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                this.Connection.Close();
                this.CreateAssetModsTable();
            }
            else
            {
                this.Connection.Close();
            }
        }
        private async void CreateAssetModsTable()
        {
            await this.Connection.OpenAsync();

            string sql = "CREATE TABLE AssetMod (id INTEGER PRIMARY KEY, name VARCHAR(30), description VARCHAR(600), isEnabled INT, targetRPF VARCHAR(260), isUsableAssetMod INT, orderIndex INT)";
            SQLiteCommand command = new SQLiteCommand(sql, this.Connection);
            command.ExecuteNonQuery();

            this.Connection.Close();
        }
    }
}
