using System;
using System.IO;
using System.Collections.ObjectModel;

using Newtonsoft.Json;
using System.Collections.Generic;

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
        public int OrderIndex { get; set; }
    }
}
