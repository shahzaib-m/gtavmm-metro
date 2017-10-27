using System;
using System.IO;
using System.Collections.ObjectModel;

using Newtonsoft.Json;

namespace gtavmm_metro.Models
{
    public class ScriptMod
    {
        /// <summary>
        /// The randomly generated numerical unique identifier for the script mod.
        /// </summary>
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }

        #region Static Methods
        /// <summary>
        /// Creates a new script mod and serializes that data into a JSON file. This is a static method not specific to an instance of a script mod.
        /// </summary>
        /// <param name="scriptModsRootPath">The full path of the root directory where the script mods are contained.</param>
        /// <param name="Name">The name of the script mod. This cannot be empty.</param>
        /// <param name="Description">An optional description of the script mod.</param>
        /// <param name="IsEnabled">An optional initial state of the script mod (enabled by default).</param>
        /// <returns></returns>
        public static ScriptMod CreateScriptMod(string scriptModsRootPath, string Name, string Description = null, bool IsEnabled = true)
        {
            Random randgen = new Random(Guid.NewGuid().GetHashCode()); // https://stackoverflow.com/questions/1785744/how-do-i-seed-a-random-class-to-avoid-getting-duplicate-random-values
            ScriptMod newScriptMod = new ScriptMod
            {
                Id = randgen.Next(100000000, 999999999),
                Name = Name,
                Description = Description,
                IsEnabled = IsEnabled
            };

            string fullPath = Path.Combine(scriptModsRootPath, newScriptMod.Id.ToString());

            Directory.CreateDirectory(fullPath);
            File.WriteAllText(String.Format("{0}.json", fullPath), JsonConvert.SerializeObject(newScriptMod, Formatting.Indented));
            File.Create(Path.Combine(fullPath, String.Format("~ {0}.gtavmm", newScriptMod.Name))).Close();

            return newScriptMod;
        }

        /// <summary>
        /// Returns a list of all deserialized script mods contained in the given root directory using their JSON files. This is a static method not specific to an instance of a script mod.
        /// </summary>
        /// <param name="scriptModsRootPath">The full path of the root directory where the script mods are contained.</param>
        /// <returns></returns>
        public static ObservableCollection<ScriptMod> GetAllScriptMods(string scriptModsRootPath)
        {
            ObservableCollection<ScriptMod> result = new ObservableCollection<ScriptMod>();

            DirectoryInfo scriptModsRootFolder = new DirectoryInfo(@"ScriptMods"); // temp
            string fileContent;
            FileInfo[] scriptModFiles = scriptModsRootFolder.GetFiles("*.json");
            foreach (FileInfo file in scriptModFiles)
            {
                fileContent = File.ReadAllText(file.FullName);
                try
                {
                    result.Add(JsonConvert.DeserializeObject<ScriptMod>(fileContent));
                }
                catch (JsonException ex)
                {
                    // handle JSON deserialization errors
                }
            }

            return result;
        }
        #endregion

        #region Instance Specific Methods
        /// <summary>
        /// Updates this script mod and its JSON file with the new properties.
        /// </summary>
        /// <param name="scriptModsRootPath">The full path of the root directory where the script mods are contained.</param>
        /// <param name="newName">The new name for this script mod.</param>
        /// <param name="newDescription">The new description for this script mod.</param>
        /// <param name="newIsEnabled">The new state for this script mod.</param>
        public void UpdateScriptMod(string scriptModsRootPath, string newName, string newDescription, bool newIsEnabled)
        {
            ScriptMod updatedScriptMod = new ScriptMod
            {
                Id = this.Id,
                Name = newName,
                Description = newDescription,
                IsEnabled = newIsEnabled
            };

            string fullJsonPath = Path.Combine(scriptModsRootPath, String.Format("{0}.json", this.Id));
            File.WriteAllText(fullJsonPath, JsonConvert.SerializeObject(updatedScriptMod, Formatting.Indented));


            string scriptModNameFilePath = Path.Combine(scriptModsRootPath, 
                                                        this.Id.ToString(), 
                                                        String.Format("~ {0}.gtavmm", updatedScriptMod.Name));
            DirectoryInfo scriptModDirectory = new DirectoryInfo(Path.Combine(scriptModsRootPath, this.Id.ToString()));
            if (!File.Exists(scriptModNameFilePath))
            {
                foreach(FileInfo file in scriptModDirectory.EnumerateFiles("*.gtavmm"))
                {
                    file.Delete();
                }

                File.Create(scriptModNameFilePath).Close();
            }
        }

        /// <summary>
        /// Marks the folder and JSON file of this script mod as removed OR deletes the folder recursively and the JSON file.
        /// </summary>
        /// <param name="scriptModsRootPath">The full path of the root directory where the script mods are contained.</param>
        /// <param name="deleteFiles">If false, the related files of this script mod are marked as removed. If true, all related files of this script mod are deleted.</param>
        public void RemoveAndOrDeleteScriptMod(string scriptModsRootPath, bool deleteFiles)
        {
            string fullJsonPath = Path.Combine(scriptModsRootPath, String.Format("{0}.json", this.Id));
            string fullFolderPath = Path.Combine(scriptModsRootPath, String.Format("{0}", this.Id));

            if (!deleteFiles)
            {
                File.Move(fullJsonPath, String.Format("{0}.removed", fullJsonPath));
                Directory.Move(fullFolderPath, String.Format("{0}.removed", fullFolderPath));
            }
            else
            {
                File.Delete(fullJsonPath);
                Directory.Delete(fullFolderPath, true);
            }
        }
        #endregion
    }
}
 