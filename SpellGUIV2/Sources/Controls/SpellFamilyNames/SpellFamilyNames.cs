using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SpellEditor.Sources.Controls.SpellFamilyNames
{
    public static class SpellFamilyNames
    {
        private class FamilyEntry
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string DefinitionFile { get; set; }
        }

        public static Dictionary<int, string> familyNames = new Dictionary<int, string>();
        private static Dictionary<int, string> definitionFiles = new Dictionary<int, string>();

        public static readonly Dictionary<int, Dictionary<int, string>> familyFlagsNames = new Dictionary<int, Dictionary<int, string>>();

        // hack for UI because filteredcombobox currently only works properly with itemsource, not items added from code
        public static List<string> familyNamesitemSource = new List<string>(); 

        public static string GetFamilyName(int familyId)
        {
            if (familyNames.ContainsKey(familyId))
            {
                var name = familyNames[familyId];

                if (!string.IsNullOrEmpty(name))
                    return name;
            }
            return null;
        }

        public static string GetFamilyFlagName(int familyId, int flag_id)
        {
            if (familyFlagsNames.ContainsKey(familyId))
            {
                var familyFlagsDict = familyFlagsNames[familyId];
                if (familyFlagsDict.ContainsKey(flag_id))
                {
                    var name = familyFlagsDict[flag_id];

                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }

            return null;
        }

        public static void Init()
        {
            ReadFamilyDefinitions();
            ReadFamilyFlagsDefinitions();

            if (!familyNames.ContainsKey(0))
                familyNames.Add(0, "Default");
            else if (string.IsNullOrEmpty(familyNames[0]))
                familyNames.Add(0, "Default");

            GenerateItemSource();
        }

        private static void ReadFamilyDefinitions()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(basePath, "SpellFamilies", "Definitions.json");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                List<FamilyEntry> entries = JsonConvert.DeserializeObject<List<FamilyEntry>>(json);

                foreach (var entry in entries)
                {
                    familyNames.Add(entry.Id, entry.Name);
                    definitionFiles.Add(entry.Id, entry.DefinitionFile);
                }
            }

        }

        private static void ReadFamilyFlagsDefinitions()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            foreach (var pair in definitionFiles)
            {
                if (string.IsNullOrEmpty(pair.Value))
                    continue;

                string name = pair.Value + ".json";

                string path = Path.Combine(basePath, "SpellFamilies", name);

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);

                    var dict = JsonConvert.DeserializeObject<Dictionary<int, string>>(json);

                    int id = pair.Key;
                    familyFlagsNames.Add(id, dict);
                }
            }
        }

        public static void GenerateItemSource()
        {
            familyNamesitemSource.Clear();

            foreach (var family in familyNames)
            {
                if (string.IsNullOrEmpty(family.Value))
                    continue;

                familyNamesitemSource.Add(family.Key.ToString() + " - " + family.Value);
            }
        }

    }
}
