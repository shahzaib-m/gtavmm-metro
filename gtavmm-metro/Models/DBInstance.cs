using System;
using System.Threading.Tasks;

using System.Data.Common;
using System.Data.SQLite;

namespace gtavmm_metro.Models
{
    public class DBInstance
    {
        public SQLiteConnection Connection { get; private set; }

        public static readonly string DBFileName = "data.gtavmm-metro";
        private static readonly string DbConnectionString = "Data Source={0}\\{1};Version=3;";

        public DBInstance(string modsFolderRoot)
        {
            this.Connection = new SQLiteConnection(String.Format(DbConnectionString, modsFolderRoot, DBFileName));
        }

        public async Task VerifyTablesState()
        {
            await this.VerifyScriptModTable();
            await this.VerifyAssetModTable();
        }

        private async Task VerifyScriptModTable()
        {
            await this.Connection.OpenAsync();

            string sql = "SELECT * FROM sqlite_master WHERE type=@type AND name=@name";
            SQLiteCommand command = new SQLiteCommand(sql, this.Connection);
            command.Parameters.AddWithValue("type", "table");
            command.Parameters.AddWithValue("name", "ScriptMod");

            DbDataReader reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                this.Connection.Close();
                
                await this.CreateScriptModTable();
            }
            else
            {
                this.Connection.Close();
            }
        }
        private async Task CreateScriptModTable()
        {
            await this.Connection.OpenAsync();

            string sql = @"CREATE TABLE ScriptMod (
                            id INTEGER PRIMARY KEY,
                            name VARCHAR(30),
                            description VARCHAR(600),
                            isEnabled INT NOT NULL,
                            isInserted INT NOT NULL,
                            filesWithPath VARCHAR,
                            orderIndex INT NOT NULL
                         );";
            SQLiteCommand command = new SQLiteCommand(sql, this.Connection);
            command.ExecuteNonQuery();

            this.Connection.Close();
        }

        private async Task VerifyAssetModTable()
        {
            await this.Connection.OpenAsync();

            string sql = "SELECT * FROM sqlite_master WHERE type=@type AND name=@name";
            SQLiteCommand command = new SQLiteCommand(sql, this.Connection);
            command.Parameters.AddWithValue("type", "table");
            command.Parameters.AddWithValue("name", "AssetMod");

            DbDataReader reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                this.Connection.Close();
                await this.CreateAssetModsTable();
            }
            else
            {
                this.Connection.Close();
            }
        }
        private async Task CreateAssetModsTable()
        {
            await this.Connection.OpenAsync();

            string sql = @"CREATE TABLE AssetMod (
                            id INTEGER PRIMARY KEY,
                            name VARCHAR(30),
                            description VARCHAR(600),
                            isEnabled INT NOT NULL,
                            isInserted INT NOT NULL,
                            targetRPF VARCHAR(260),
                            isUsableAssetMod INT NOT NULL,
                            orderIndex INT NOT NULL
                         );";
            SQLiteCommand command = new SQLiteCommand(sql, this.Connection);
            command.ExecuteNonQuery();

            this.Connection.Close();
        }
    }
}
