using System;
using System.IO;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace gtavmm_metro.Models
{
    public class ScriptModAPI
    {
        private DirectoryInfo ScriptModsRootFolder;
        private SQLiteConnection ScriptModsDb;
        private string ScriptModsDbConnectionString = "Data Source={0}\\data.gtavmm-metro;Version=3;";

        public ScriptModAPI(string scriptModsRootFolder)
        {
            this.ScriptModsRootFolder = new DirectoryInfo(scriptModsRootFolder);
            this.ScriptModsDb = new SQLiteConnection(String.Format(this.ScriptModsDbConnectionString, scriptModsRootFolder));
            if (!File.Exists(Path.Combine(scriptModsRootFolder, "data.gtavmm-metro")))
            {
                SQLiteConnection.CreateFile(Path.Combine(scriptModsRootFolder, "data.gtavmm-metro"));
                this.ScriptModsDb.Open();

                string sql = "CREATE TABLE ScriptMod (id INT, name VARCHAR(30), description VARCHAR(600), isEnabled INT, orderIndex INT)";
                SQLiteCommand command = new SQLiteCommand(sql, this.ScriptModsDb);
                command.ExecuteNonQuery();

                this.ScriptModsDb.Close();
            }
        }

        public async Task<ScriptMod> CreateScriptMod(string Name, int orderIndex, string Description = null, bool IsEnabled = true)
        {
            Random randgen = new Random(Guid.NewGuid().GetHashCode()); // https://stackoverflow.com/questions/1785744/how-do-i-seed-a-random-class-to-avoid-getting-duplicate-random-values
            int randomId;
            do
            {
                randomId = randgen.Next(100000000, 999999999);
            } while (await this.GetScriptModById(randomId) != null);

            ScriptMod newScriptMod = new ScriptMod
            {
                Id = randomId,
                Name = Name,
                Description = Description,
                IsEnabled = IsEnabled,
                OrderIndex = orderIndex
            };

            if (Directory.Exists(Path.Combine(this.ScriptModsRootFolder.FullName, Name)))
            {
                int i = 1;
                string numberAppendedDir = Path.Combine(this.ScriptModsRootFolder.FullName, String.Format("{0} ({1})", newScriptMod.Name, i));
                while (Directory.Exists(numberAppendedDir))
                {
                    i++;
                    numberAppendedDir = Path.Combine(this.ScriptModsRootFolder.FullName, String.Format("{0} ({1})", newScriptMod.Name, i));
                }

                Directory.CreateDirectory(numberAppendedDir);
                newScriptMod.Name = String.Format("{0} ({1})", newScriptMod.Name, i);
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(this.ScriptModsRootFolder.FullName, newScriptMod.Name));
            }


            this.ScriptModsDb.Open();

            string sql = "INSERT into ScriptMod (id, name, description, isEnabled, orderIndex) VALUES (@id, @name, @description, @isEnabled, @orderIndex)";
            SQLiteCommand command = new SQLiteCommand(sql, this.ScriptModsDb);
            command.Parameters.AddWithValue("id", newScriptMod.Id);
            command.Parameters.AddWithValue("name", newScriptMod.Name);
            command.Parameters.AddWithValue("description", newScriptMod.Description);
            command.Parameters.AddWithValue("isEnabled", Convert.ToInt32(newScriptMod.IsEnabled));
            command.Parameters.AddWithValue("orderIndex", newScriptMod.OrderIndex);
            await command.ExecuteNonQueryAsync();

            this.ScriptModsDb.Close();


            return newScriptMod;
        }

        public async Task<List<ScriptMod>> GetAllScriptMods()
        {
            List<ScriptMod> allScriptMods = new List<ScriptMod>();
            List<int> modsByIdWithNoFolders = new List<int>();

            this.ScriptModsDb.Open();

            string sql = "SELECT * FROM ScriptMod ORDER BY orderIndex ASC";
            SQLiteCommand command = new SQLiteCommand(sql, this.ScriptModsDb);

            DbDataReader reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                this.ScriptModsDb.Close();
                return null;
            }
            while (await reader.ReadAsync())
            {
                int id = (int)reader["id"];
                string name = (string)reader["name"];
                string description = (string)reader["description"];
                bool isEnabled = Convert.ToBoolean((int)reader["isEnabled"]);
                int orderIndex = (int)reader["orderIndex"];


                if (!Directory.Exists(Path.Combine(this.ScriptModsRootFolder.FullName, name)))
                {
                    modsByIdWithNoFolders.Add(id);
                }
                else
                {
                    ScriptMod scriptMod = new ScriptMod();
                    allScriptMods.Add(new ScriptMod()
                    {
                        Id = id,
                        Name = name,
                        Description = description,
                        IsEnabled = isEnabled,
                        OrderIndex = orderIndex
                    });
                }
            }

            this.ScriptModsDb.Close();


            if (modsByIdWithNoFolders.Count != 0)
                foreach (int scriptModId in modsByIdWithNoFolders)
                    await this.RemoveAndDeleteScriptMod(scriptModId, false);

            return allScriptMods;
        }

        public async Task<ScriptMod> GetScriptModById(int scriptModId)
        {
            this.ScriptModsDb.Open();

            string sql = "SELECT * FROM ScriptMod WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ScriptModsDb);
            command.Parameters.AddWithValue("id", scriptModId);

            ScriptMod resultScriptMod = new ScriptMod();
            DbDataReader reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                this.ScriptModsDb.Close();
                return null;
            }
            while (await reader.ReadAsync())
            {
                resultScriptMod.Id = (int)reader["id"];
                resultScriptMod.Name = (string)reader["name"];
                resultScriptMod.Description = (string)reader["description"];
                resultScriptMod.IsEnabled = Convert.ToBoolean((int)reader["isEnabled"]);
            }

            this.ScriptModsDb.Close();


            return resultScriptMod;
        }

        public async Task<string> GetOldNameBeforeIllegalEdit(int scriptModId)
        {
            ScriptMod result = await this.GetScriptModById(scriptModId);
            return result.Name;
        }

        public async Task<bool> UpdateScriptModName(int scriptModId, string newName)
        {
            ScriptMod oldScriptMod = await this.GetScriptModById(scriptModId);
            string oldDirectoryName = oldScriptMod.Name;
            try
            {
                Directory.Move(Path.Combine(this.ScriptModsRootFolder.FullName, oldDirectoryName),
                    Path.Combine(this.ScriptModsRootFolder.FullName, newName));
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException)
                    return false;

                throw;
            }


            this.ScriptModsDb.Open();

            string sql = "UPDATE ScriptMod SET name=@name WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ScriptModsDb);
            command.Parameters.AddWithValue("name", newName);
            command.Parameters.AddWithValue("id", scriptModId);
            await command.ExecuteNonQueryAsync();

            this.ScriptModsDb.Close();


            return true;
        }

        public async Task UpdateScriptModDescription(int scriptModId, string newDescription)
        {
            this.ScriptModsDb.Open();

            string sql = "UPDATE ScriptMod SET description=@description WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ScriptModsDb);
            command.Parameters.AddWithValue("description", newDescription);
            command.Parameters.AddWithValue("id", scriptModId);
            await command.ExecuteNonQueryAsync();

            this.ScriptModsDb.Close();
        }

        public async Task UpdateScriptModIsEnabled(int scriptModId, bool newIsEnabled)
        {
            this.ScriptModsDb.Open();

            string sql = "UPDATE ScriptMod SET isEnabled=@isEnabled WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ScriptModsDb);
            command.Parameters.AddWithValue("isEnabled", Convert.ToInt32(newIsEnabled));
            command.Parameters.AddWithValue("id", scriptModId);
            await command.ExecuteNonQueryAsync();

            this.ScriptModsDb.Close();
        }

        private async Task UpdateScriptModOrderIndex(int scriptModId, int newIndex)
        {
            this.ScriptModsDb.Open();

            string sql = "UPDATE ScriptMod SET orderIndex=@orderIndex WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ScriptModsDb);
            command.Parameters.AddWithValue("orderIndex", newIndex);
            command.Parameters.AddWithValue("id", scriptModId);
            await command.ExecuteNonQueryAsync();

            this.ScriptModsDb.Close();
        }

        public async void UpdateScriptModOrderIndexes(IList<ScriptMod> scriptMods)
        {
            for (int i = 0; i < scriptMods.Count; i++)
            {
                await this.UpdateScriptModOrderIndex(scriptMods[i].Id, i);
            }
        }

        public async Task<bool> RemoveAndDeleteScriptMod(int scriptModId, bool isManualDelete = true)
        {
            if (isManualDelete)
            {
                string fullFolderPath = Path.Combine(this.ScriptModsRootFolder.FullName, (await this.GetScriptModById(scriptModId)).Name);
                if (Directory.Exists(fullFolderPath))
                {
                    try
                    {
                        Directory.Delete(fullFolderPath, true);
                    }
                    catch (Exception ex)
                    {
                        if (ex is IOException || ex is UnauthorizedAccessException)
                        {
                            return false;
                        }

                        throw;
                    }
                }
            }


            this.ScriptModsDb.Open();

            string sql = "DELETE FROM ScriptMod WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ScriptModsDb);
            command.Parameters.AddWithValue("id", scriptModId);
            await command.ExecuteNonQueryAsync();

            this.ScriptModsDb.Close();


            return true;
        }
    }
}
