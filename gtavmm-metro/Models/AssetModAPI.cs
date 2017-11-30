using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Data.Common;
using System.Data.SQLite;

using gtavmm_metro.Properties;

namespace gtavmm_metro.Models
{
    public class AssetModAPI
    {
        private DirectoryInfo ModsRootFolder;
        private DBInstance ModsDb;
        private string GTAVPath;

        public AssetModAPI(string ModsRootFolder, DBInstance modsDbConection, string gtaVPath)
        {
            this.ModsRootFolder = new DirectoryInfo(ModsRootFolder);
            this.ModsDb = modsDbConection;
            this.GTAVPath = gtaVPath;
        }

        public IList<string> GetAllRPFsInsideGTAVDirectory()    // should be part of GTAV API?
        {
            DirectoryInfo directory = new DirectoryInfo(this.GTAVPath);
            FileInfo[] allRPFs = directory.GetFiles("*.rpf", SearchOption.AllDirectories);

            List<string> RPFNames = new List<string>();
            foreach (FileInfo RPF in allRPFs)
            {
                string fullPath = RPF.FullName;
                RPFNames.Add(fullPath.Split(new string[] { Settings.Default.GTAVDirectory }, StringSplitOptions.RemoveEmptyEntries)[0]);
            }

            return RPFNames;
        }

        public async Task<AssetMod> CreateAssetMod(string name, string targetRPF, int orderIndex,
            string description = null, bool isEnabled = true, bool isUsableAssetMod = true)
        {
            AssetMod newAssetMod = new AssetMod
            {
                Name = name,
                Description = description,
                IsEnabled = isEnabled,
                TargetRPF = targetRPF,
                IsUsableAssetMod = isUsableAssetMod,
                OrderIndex = orderIndex
            };

            if (isUsableAssetMod)
            {
                if (File.Exists(Path.Combine(this.ModsRootFolder.FullName, name + ".rpf")))
                {
                    int i = 1;
                    string numberAppendedFile = Path.Combine(this.ModsRootFolder.FullName, String.Format("{0} ({1}).rpf", newAssetMod.Name, i));
                    while (File.Exists(numberAppendedFile))
                    {
                        i++;
                        numberAppendedFile = Path.Combine(this.ModsRootFolder.FullName, String.Format("{0} ({1}).rpf", newAssetMod.Name, i));
                    }

                    File.Copy(Path.Combine(this.GTAVPath, targetRPF.Substring(1)), Path.Combine(this.ModsRootFolder.FullName, numberAppendedFile));
                    newAssetMod.Name = String.Format("{0} ({1})", newAssetMod.Name, i);
                }
                else
                {
                    File.Copy(Path.Combine(this.GTAVPath, targetRPF.Substring(1)),
                        Path.Combine(this.ModsRootFolder.FullName, newAssetMod.Name + ".rpf"));
                }
            }


            await this.ModsDb.Connection.OpenAsync();

            string sql = "INSERT into AssetMod (name, description, isEnabled, targetRPF, isUsableAssetMod, orderIndex) VALUES (@name, @description, @isEnabled, @targetRPF, @isUsableAssetMod, @orderIndex)";

            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);

            command.Parameters.AddWithValue("name", newAssetMod.Name);
            command.Parameters.AddWithValue("description", newAssetMod.Description);
            command.Parameters.AddWithValue("isEnabled", Convert.ToInt32(newAssetMod.IsEnabled));
            command.Parameters.AddWithValue("targetRPF", newAssetMod.TargetRPF);
            command.Parameters.AddWithValue("isUsableAssetMod", Convert.ToInt32(newAssetMod.IsUsableAssetMod));
            command.Parameters.AddWithValue("orderIndex", newAssetMod.OrderIndex);
            await command.ExecuteNonQueryAsync();

            sql = "SELECT last_insert_rowid()";
            command = new SQLiteCommand(sql, this.ModsDb.Connection);
            newAssetMod.Id = (int)(long)(await command.ExecuteScalarAsync());

            this.ModsDb.Connection.Close();


            return newAssetMod;
        }

        public async Task<List<AssetMod>> GetAllAssetMods()
        {
            List<AssetMod> allAssetMods = new List<AssetMod>();
            List<int> modsByIdWithNoFiles = new List<int>();

            await this.ModsDb.Connection.OpenAsync();

            string sql = "SELECT * FROM AssetMod ORDER BY orderIndex ASC";
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
                string targetRPF = (string)reader["targetRPF"];
                bool isUsableAssetMod = Convert.ToBoolean((int)reader["isUsableAssetMod"]);
                int orderIndex = (int)reader["orderIndex"];


                if (!File.Exists(Path.Combine(this.ModsRootFolder.FullName, name + ".rpf")) && isUsableAssetMod)
                {
                    modsByIdWithNoFiles.Add(id);
                }
                else
                {
                    AssetMod assetMod = new AssetMod();
                    allAssetMods.Add(new AssetMod()
                    {
                        Id = id,
                        Name = name,
                        Description = description,
                        IsEnabled = isEnabled,
                        TargetRPF = targetRPF,
                        IsUsableAssetMod = isUsableAssetMod,
                        OrderIndex = orderIndex
                    });
                }
            }

            this.ModsDb.Connection.Close();



            if (modsByIdWithNoFiles.Count != 0)
                foreach (int assetModId in modsByIdWithNoFiles)
                    await this.RemoveAndDeleteAssetMod(assetModId, false);

            return allAssetMods;
        }

        public async Task<AssetMod> GetAssetModById(int assetModId)
        {
            await this.ModsDb.Connection.OpenAsync();

            string sql = "SELECT * FROM AssetMod WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("id", assetModId);

            AssetMod resultAssetMod = new AssetMod();
            DbDataReader reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                this.ModsDb.Connection.Close();
                return null;
            }
            while (await reader.ReadAsync())
            {
                resultAssetMod.Id = (int)(long)reader["id"];
                resultAssetMod.Name = (string)reader["name"];
                resultAssetMod.Description = (string)reader["description"];
                resultAssetMod.IsEnabled = Convert.ToBoolean((int)reader["isEnabled"]);
                resultAssetMod.TargetRPF = (string)reader["targetRPF"];
                resultAssetMod.IsUsableAssetMod = Convert.ToBoolean((int)reader["isUsableAssetMod"]);
            }

            this.ModsDb.Connection.Close();


            return resultAssetMod;
        }

        public async Task<string> GetOldNameBeforeIllegalEdit(int assetModId)
        {
            AssetMod result = await this.GetAssetModById(assetModId);
            return result.Name;
        }

        public async Task<string> GetOldTargetRPFBeforeIllegalEdit(int assetModId)
        {
            AssetMod result = await this.GetAssetModById(assetModId);
            return result.TargetRPF;
        }

        public async Task<bool> UpdateAssetModName(int assetModId, string newName)
        {
            AssetMod oldAssetMod = await this.GetAssetModById(assetModId);
            string oldFileName = oldAssetMod.Name;
            try
            {
                File.Move(Path.Combine(this.ModsRootFolder.FullName, oldFileName + ".rpf"),
                    Path.Combine(this.ModsRootFolder.FullName, newName + ".rpf"));
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException)
                    return false;

                throw;
            }


            await this.ModsDb.Connection.OpenAsync();

            string sql = "UPDATE AssetMod SET name=@name WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("name", newName);
            command.Parameters.AddWithValue("id", assetModId);
            await command.ExecuteNonQueryAsync();

            this.ModsDb.Connection.Close();


            return true;
        }

        public async Task UpdateAssetModDescription(int assetModId, string newDescription)
        {
            await this.ModsDb.Connection.OpenAsync();

            string sql = "UPDATE AssetMod SET description=@description WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("description", newDescription);
            command.Parameters.AddWithValue("id", assetModId);
            await command.ExecuteNonQueryAsync();

            this.ModsDb.Connection.Close();
        }

        public async Task UpdateAssetModIsEnabled(int assetModId, bool newIsEnabled)
        {
            await this.ModsDb.Connection.OpenAsync();

            string sql = "UPDATE AssetMod SET isEnabled=@isEnabled WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("isEnabled", Convert.ToInt32(newIsEnabled));
            command.Parameters.AddWithValue("id", assetModId);
            await command.ExecuteNonQueryAsync();

            this.ModsDb.Connection.Close();
        }

        public async Task UpdateAssetModTargetRPF(int assetModId, string newTargetRPF)
        {
            await this.ModsDb.Connection.OpenAsync();

            string sql = "UPDATE AssetMod SET targetRPF=@targetRPF WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("targetRPF", newTargetRPF);
            command.Parameters.AddWithValue("id", assetModId);
            await command.ExecuteNonQueryAsync();

            this.ModsDb.Connection.Close();
        }

        public async Task UpdateAssetModIsUsableAssetMod(int assetModId, bool newIsUsableAssetMod)
        {
            await this.ModsDb.Connection.OpenAsync();

            string sql = "UPDATE AssetMod SET isUsableAssetMod=@isUsableAssetMod WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("isUsableAssetMod", Convert.ToInt32(newIsUsableAssetMod));
            command.Parameters.AddWithValue("id", assetModId);
            await command.ExecuteNonQueryAsync();

            this.ModsDb.Connection.Close();
        }

        private async Task UpdateAssetModOrderIndex(int assetModId, int newIndex)
        {
            await this.ModsDb.Connection.OpenAsync();

            string sql = "UPDATE AssetMod SET orderIndex=@orderIndex WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("orderIndex", newIndex);
            command.Parameters.AddWithValue("id", assetModId);
            await command.ExecuteNonQueryAsync();
            this.ModsDb.Connection.Close();
        }

        public async void UpdateAssetModOrderIndexes(IList<AssetMod> assetMods)
        {
            for (int i = 0; i < assetMods.Count; i++)
            {
                await this.UpdateAssetModOrderIndex(assetMods[i].Id, i);
            }
        }

        public async Task<bool> RemoveAndDeleteAssetMod(int assetModId, bool isManualDelete = true)
        {
            if (isManualDelete)
            {
                string fullFilePath = Path.Combine(this.ModsRootFolder.FullName, (await this.GetAssetModById(assetModId)).Name + ".rpf");
                if (File.Exists(fullFilePath))
                {
                    try
                    {
                        File.Delete(fullFilePath);
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

            string sql = "DELETE FROM AssetMod WHERE id=@id";
            SQLiteCommand command = new SQLiteCommand(sql, this.ModsDb.Connection);
            command.Parameters.AddWithValue("id", assetModId);
            await command.ExecuteNonQueryAsync();

            this.ModsDb.Connection.Close();


            return true;
        }
    }
}
