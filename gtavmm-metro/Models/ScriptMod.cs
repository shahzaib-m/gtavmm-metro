using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Data.Common;
using System.Data.SQLite;

namespace gtavmm_metro.Models
{
    public class ScriptMod
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public int OrderIndex { get; set; }
    }

    public class ScriptModAPI
    {
        private DirectoryInfo ModsRootFolder;
        private DBInstance ModsDb;

        public ScriptModAPI(string ModsRootFolder, DBInstance modsDbConection)
        {
            this.ModsRootFolder = new DirectoryInfo(ModsRootFolder);
            this.ModsDb = modsDbConection;
        }

        public async Task<ScriptMod> CreateScriptMod(string name, int orderIndex, string description = null, bool isEnabled = true, bool createFolder = true)
        {
            ScriptMod newScriptMod = new ScriptMod
            {
                Name = name,
                Description = description,
                IsEnabled = isEnabled,
                OrderIndex = orderIndex
            };

            if (createFolder)
            {
                if (Directory.Exists(Path.Combine(this.ModsRootFolder.FullName, name)))
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
            }


            await this.ModsDb.Connection.OpenAsync();

            string sql = "INSERT into ScriptMod (name, description, isEnabled, orderIndex) VALUES (@name, @description, @isEnabled, @orderIndex)";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("name", newScriptMod.Name);
            command.Parameters.AddWithValue("description", newScriptMod.Description);
            command.Parameters.AddWithValue("isEnabled", Convert.ToInt32(newScriptMod.IsEnabled));
            command.Parameters.AddWithValue("orderIndex", newScriptMod.OrderIndex);
            await command.ExecuteNonQueryAsync();

            sql = "SELECT last_insert_rowid()";
            command = new SQLiteCommand(sql, this.ModsDb.Connection);
            newScriptMod.Id = (int)(long)(await command.ExecuteScalarAsync());

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
                int id = (int)(long)reader["id"];
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

            DirectoryInfo[] allDirsInsideModsDir = this.ModsRootFolder.GetDirectories();
            if (allDirsInsideModsDir.Length != allScriptMods.Count)
            {
                foreach (DirectoryInfo dir in allDirsInsideModsDir)
                {
                    bool found = allScriptMods.Any(modFolder => modFolder.Name == dir.Name);
                    if (!found)
                    {
                        allScriptMods.Add(await this.CreateScriptMod(dir.Name, allScriptMods.Count - 1,
                            "* This modification was not recognized. It could have been added outside of this application, or it was an existing modification which has been renamed outside of this application. *",
                                false, false));
                    }
                }
            }

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
                resultScriptMod.Id = (int)(long)reader["id"];
                resultScriptMod.Name = (string)reader["name"];
                resultScriptMod.Description = (string)reader["description"];
                resultScriptMod.IsEnabled = Convert.ToBoolean((int)reader["isEnabled"]);
            }

            this.ModsDb.Connection.Close();


            return resultScriptMod;
        }

        public async Task<string> GetOldNameBeforeIllegalEdit(int scriptModId)
        {
            return (await this.GetScriptModById(scriptModId)).Name;
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

    public class ScriptModContent
    {
        public ScriptMod ScriptMod { get; set; }
        public List<string> FilesWithPaths { get; set; }
    }
}
