using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Data.Common;
using System.Data.SQLite;

namespace gtavmm_metro.Models
{
    public class ScriptModAPI
    {
        private DirectoryInfo ModsRootFolder;
        private DBInstance ModsDb;

        public ScriptModAPI(string ModsRootFolder, DBInstance modsDbConection)
        {
            this.ModsRootFolder = new DirectoryInfo(ModsRootFolder);
            this.ModsDb = modsDbConection;
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

            if (Directory.Exists(Path.Combine(this.ModsRootFolder.FullName, Name)))
            {
                int i = 1;
                string numberAppendedDir = Path.Combine(this.ModsRootFolder.FullName, String.Format("{0} ({1})", newScriptMod.Name, i));
                while (Directory.Exists(numberAppendedDir))
                {
                    i++;
                    numberAppendedDir = Path.Combine(this.ModsRootFolder.FullName, String.Format("{0} ({1})", newScriptMod.Name, i));
                }

                Directory.CreateDirectory(numberAppendedDir);
                newScriptMod.Name = String.Format("{0} ({1})", newScriptMod.Name, i);
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(this.ModsRootFolder.FullName, newScriptMod.Name));
            }


            await this.ModsDb.Connection.OpenAsync();

            string sql = "INSERT into ScriptMod (id, name, description, isEnabled, orderIndex) VALUES (@id, @name, @description, @isEnabled, @orderIndex)";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("id", newScriptMod.Id);
            command.Parameters.AddWithValue("name", newScriptMod.Name);
            command.Parameters.AddWithValue("description", newScriptMod.Description);
            command.Parameters.AddWithValue("isEnabled", Convert.ToInt32(newScriptMod.IsEnabled));
            command.Parameters.AddWithValue("orderIndex", newScriptMod.OrderIndex);
            await command.ExecuteNonQueryAsync();

            this.ModsDb.Connection.Close();


            return newScriptMod;
        }

        public async Task<List<ScriptMod>> GetAllScriptMods()
        {
            List<ScriptMod> allScriptMods = new List<ScriptMod>();
            List<int> modsByIdWithNoFolders = new List<int>();

            await this.ModsDb.Connection.OpenAsync();

            string sql = "SELECT * FROM ScriptMod ORDER BY orderIndex ASC";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);

            DbDataReader reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                this.ModsDb.Connection.Close();
                return null;
            }
            while (await reader.ReadAsync())
            {
                int id = (int)reader["id"];
                string name = (string)reader["name"];
                string description = (string)reader["description"];
                bool isEnabled = Convert.ToBoolean((int)reader["isEnabled"]);
                int orderIndex = (int)reader["orderIndex"];


                if (!Directory.Exists(Path.Combine(this.ModsRootFolder.FullName, name)))
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

            this.ModsDb.Connection.Close();


            if (modsByIdWithNoFolders.Count != 0)
                foreach (int scriptModId in modsByIdWithNoFolders)
                    await this.RemoveAndDeleteScriptMod(scriptModId, false);

            return allScriptMods;
        }

        public async Task<ScriptMod> GetScriptModById(int scriptModId)
        {
            await this.ModsDb.Connection.OpenAsync();

            string sql = "SELECT * FROM ScriptMod WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("id", scriptModId);

            ScriptMod resultScriptMod = new ScriptMod();
            DbDataReader reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                this.ModsDb.Connection.Close();
                return null;
            }
            while (await reader.ReadAsync())
            {
                resultScriptMod.Id = (int)reader["id"];
                resultScriptMod.Name = (string)reader["name"];
                resultScriptMod.Description = (string)reader["description"];
                resultScriptMod.IsEnabled = Convert.ToBoolean((int)reader["isEnabled"]);
            }

            this.ModsDb.Connection.Close();


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
                Directory.Move(Path.Combine(this.ModsRootFolder.FullName, oldDirectoryName),
                    Path.Combine(this.ModsRootFolder.FullName, newName));
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException)
                    return false;

                throw;
            }


            await this.ModsDb.Connection.OpenAsync();

            string sql = "UPDATE ScriptMod SET name=@name WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("name", newName);
            command.Parameters.AddWithValue("id", scriptModId);
            await command.ExecuteNonQueryAsync();

            this.ModsDb.Connection.Close();


            return true;
        }

        public async Task UpdateScriptModDescription(int scriptModId, string newDescription)
        {
            await this.ModsDb.Connection.OpenAsync();

            string sql = "UPDATE ScriptMod SET description=@description WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("description", newDescription);
            command.Parameters.AddWithValue("id", scriptModId);
            await command.ExecuteNonQueryAsync();

            this.ModsDb.Connection.Close();
        }

        public async Task UpdateScriptModIsEnabled(int scriptModId, bool newIsEnabled)
        {
            await this.ModsDb.Connection.OpenAsync();

            string sql = "UPDATE ScriptMod SET isEnabled=@isEnabled WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("isEnabled", Convert.ToInt32(newIsEnabled));
            command.Parameters.AddWithValue("id", scriptModId);
            await command.ExecuteNonQueryAsync();

            this.ModsDb.Connection.Close();
        }

        private async Task UpdateScriptModOrderIndex(int scriptModId, int newIndex)
        {
            await this.ModsDb.Connection.OpenAsync();

            string sql = "UPDATE ScriptMod SET orderIndex=@orderIndex WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("orderIndex", newIndex);
            command.Parameters.AddWithValue("id", scriptModId);
            await command.ExecuteNonQueryAsync();
            this.ModsDb.Connection.Close();
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
                string fullFolderPath = Path.Combine(this.ModsRootFolder.FullName, (await this.GetScriptModById(scriptModId)).Name);
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


            await this.ModsDb.Connection.OpenAsync();

            string sql = "DELETE FROM ScriptMod WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("id", scriptModId);
            await command.ExecuteNonQueryAsync();

            this.ModsDb.Connection.Close();


            return true;
        }
    }
}
