using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Config;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace HeadlessExport
{
    class Program
    {
        static ConcurrentDictionary<int, string> _TaskNameLookup;
        static ConcurrentDictionary<int, int> _TaskProgressLookup;
        static ConcurrentDictionary<int, HeadlessDbc> _HeadlessDbcLookup;

        private static BlockingCollection<string> m_Queue = new BlockingCollection<string>();

        // Toggle between REPLACING vs INSERT IGNORE. This is pretty legacy and before I made it fully idempotent (finds by matching name in id range)
        private static readonly bool ALWAYS_REPLACE = true;
        private static readonly string EDIT_QUERY_OPERATION = ALWAYS_REPLACE ? "REPLACE" : "INSERT IGNORE";

        // Configure the drop chance. Needs to be converted to a float if we want more precision.
        private static readonly int _dropChance = 100;

        // Minimum dungeon level to drop.
        private static readonly int _requiredDungeonLevel = 50;

        // A list of all NPC IDs to parse.
        private static readonly uint[] _npcIdsToParse = new uint[] {
            // Blackrock Stronghold
            52309, // Stormfang
            52304, // Magtheridon
            52306, // Grand Warlock
            52311, // Kelidan Breaker
            52292, // Blacksmith Crimbo
            52287, // Ashweaver
            52289, // Morgath Ironbelly
            52295, // Karglash Stormfang
            // Stromgarde
            52135, // Mage Boss
            52146, // Lady Ashcroft
            52155, // Stromkar
            52181, // Fleshgolem
            52149, // Valorcall Boss
            52168, // Archbishop Kelsing
            52187, // Doris Orefinger
            // Shadow Hold
            50465, // Xandris
            50468, // Methisto - gobject based loot
            50476, // Remaniel
            // Arathor
            52005, // Jack Hawkthorn
            52010, // Magister
            52212, // Derered
            52216, // Ashfang
            52008, // Abigail
            // Fall of Dalaran
            50171, // Ashcromb
            50017, // Vipenthor
            50019, // Archmage Koreln
            50039, // Archmage Baratea
            50044, // Chorno-Lord
            60017, // Crocolisk
            // Northsire Siege
            50113, // Dreadlord
            50119, // Theodosius
            50123, // Kyra
            52193, // Gravemind Pete
            // The Crypts
            50054, // Flayer
            50058, // Blightworm - gobject based loot
            50061, // Sarah Arello - gobject based loot
            50068, // Diodor
            // Valour Keep
            50098, // First Boss
            50154, // Gardener
            50162, // Khadgar
            // World's End
            50236, // Broggok
            50246, // Xannallan
            52046, // Torthridath
            50255, // Apolalypse Shade
            // The Vault
            60119 // Archmage Tervosh
        };

        private static readonly uint[] _gobjectGenerateId = new uint[]
        {
            50468, // Methisto
            50058, // Blightworm
            50061  // Sarah Arello
        };

        static Program()
        {
            var thread = new Thread(
              () =>
              {
                  while (true) Console.WriteLine(m_Queue.Take());
              })
            {
                IsBackground = true
            };
            thread.Start();
        }

        public static void WriteLine(string value)
        {
            m_Queue.Add(value);
        }

        static int PrioritiseSpellCompareBindings(Binding b1, Binding b2)
        {
            if (b1.ToString().Contains("Spell$"))
                return -1;
            if (b2.ToString().Contains("Spell$"))
                return 1;
            
            return string.Compare(b1.ToString(), b2.ToString());
        }

        static void Main(string[] args)
        {
            var adapters = new List<IDatabaseAdapter>();
            try
            {
                WriteLine("Loading config...");
                Config.Init();
                Config.connectionType = Config.ConnectionType.MySQL;

                SpawnAdapters(ref adapters);
                var adapterIndex = 0;

                var adapter = adapters[adapterIndex];
                adapters.RemoveAt(adapterIndex);
                var inQuery = string.Join(", ", _npcIdsToParse);
                using (var query = adapter.Query(
                    $"SELECT * FROM new_world.creature_template WHERE entry IN ({inQuery})"))
                {
                    foreach (DataRow row in query.Rows)
                    {
                        var npcId = uint.Parse(row["entry"].ToString());
                        var lootId = uint.Parse(row["lootid"].ToString());
                        WriteLine($"Processing npcId {npcId} with lootId {lootId}");
                        WriteLine("---------------------------------");
                        if (++adapterIndex >= adapters.Count)
                            adapterIndex = 0;
                        CreateNewLoot(npcId, row, adapters[adapterIndex], lootId);
                        WriteLine("---------------------------------");
                    }
                }

            }
            catch (Exception e)
            {
                WriteLine($"Build failed: {e.GetType()}: {e.Message}\n{e}");
                if (e.InnerException != null)
                    WriteLine($"{e.InnerException.GetType()}: {e.InnerException.Message}\n{e.InnerException}\n");
            }
            finally
            {
                while (m_Queue.Count > 0)
                    Thread.Sleep(10);
                adapters.ForEach((adapter) => adapter.Dispose());
                m_Queue.Dispose();
                Console.WriteLine("Terminating program.");
            }   
        }

        private static void CreateNewLoot(uint npcId, DataRow baseNpc, IDatabaseAdapter adapter, uint lootId)
        {
            WriteLine($"Generating data for npcId {npcId}");
            // Generate creature
            uint newPetCreatureId = 0u;
            var creatureName = baseNpc["name"].ToString();
            // Find by name in ID range first
            using (var query = adapter.Query($"SELECT entry FROM new_world.creature_template WHERE entry BETWEEN 80015 AND 80500 AND `name` = '{adapter.EscapeString(creatureName)}'"))
            {
                if (query.Rows.Count > 0)
                {
                    DataRow row = query.Rows[0];
                    uint.TryParse(row[0].ToString(), out newPetCreatureId);
                }
            }
            // Generate new ID
            if (newPetCreatureId == 0u)
            {
                using (var query = adapter.Query("SELECT MAX(entry) FROM new_world.creature_template WHERE entry BETWEEN 80015 AND 80500"))
                {
                    DataRow row = query.Rows[0];
                    newPetCreatureId = uint.Parse(row[0].ToString()) + 1u;
                }
            }
            {
                var newCreatureQuery = $"{EDIT_QUERY_OPERATION} INTO new_world.creature_template VALUES ";
                var values = new List<string>();
                foreach (var column in baseNpc.Table.Columns)
                {
                    var name = column.ToString();
                    if (name.Equals("entry"))
                        values.Add(newPetCreatureId.ToString());
                    else if (name.Equals("faction"))
                        values.Add("35");
                    else if (name.Equals("speed_walk"))
                        values.Add("1");
                    else if (name.Equals("speed_run"))
                        values.Add("1.14286");
                    else if (name.Equals("scale"))
                    {
                        // Shrink Magtheridon since his model is massive
                        if (npcId == 52304)
                            values.Add("0.08");
                        // Chrono-Lord Boss from Dalaran
                        else if (npcId == 50044)
                            values.Add("0.25");
                        else
                            values.Add("0.4");
                    }
                    else if (name.Equals("rank"))
                        values.Add("0");
                    else if (name.Equals("unit_class"))
                        values.Add("1");
                    else if (name.Equals("min_level"))
                        values.Add("1");
                    else if (name.Equals("max_level"))
                        values.Add("1");
                    else if (name.Equals("VerifiedBuild"))
                        values.Add("0");
                    else if (name.Equals("subname"))
                        values.Add("''");
                    else if (name.Equals("type_flags"))
                        values.Add("0");
                    else if (name.Equals("HealthModifier"))
                        values.Add("1");
                    else
                        values.Add("'" + adapter.EscapeString(baseNpc[column.ToString()].ToString()) + "'");
                }
                newCreatureQuery += $"({string.Join(", ", values)})";
                WriteLine("Generated new creature:\n " + newCreatureQuery);
                adapter.Execute(newCreatureQuery);
                // Add ghostly aura
                var addonQuery = $"{EDIT_QUERY_OPERATION} INTO new_world.creature_template_addon VALUES ({newPetCreatureId}, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, '460285')";
                WriteLine("Updating creature to set ghostly aura:\n " + addonQuery);
                adapter.Execute(addonQuery);
            }
            // Generate spell
            uint newSpellId = 0u;
            {
                var cloneSpellId = 460249u;
                // Find by name
                using (var query = adapter.Query("SELECT id FROM spell WHERE SpellName0 = '" + adapter.EscapeString(creatureName) + " Pet'"))
                {
                    if (query.Rows.Count > 0)
                    {
                        DataRow row = query.Rows[0];
                        uint.TryParse(row[0].ToString(), out newSpellId);
                    }
                }
                // Find new ID
                if (newSpellId == 0u)
                {
                    using (var query = adapter.Query("SELECT MAX(id) FROM spell WHERE id BETWEEN 460249 AND 460500"))
                    {
                        DataRow row = query.Rows[0];
                        newSpellId = uint.Parse(row[0].ToString()) + 1u;
                    }
                }
                var values = new List<string>();
                using (var query = adapter.Query("SELECT * FROM spell WHERE id = " + cloneSpellId))
                {
                    DataRow row = query.Rows[0];
                    foreach (var column in row.Table.Columns)
                    {
                        var name = column.ToString();
                        if (name.Equals("ID"))
                            values.Add(newSpellId.ToString());
                        else if (name.Equals("EffectMiscValue1"))
                            values.Add(newPetCreatureId.ToString());
                        else if (name.Equals("SpellIconID"))
                            values.Add("2303"); // TODO: Get icon from achievement? But most are scripted so not linked in data
                        else if (name.Equals("SpellName0"))
                            values.Add("'" + adapter.EscapeString(creatureName) + " Pet'");
                        else if (name.Equals("SpellDescription0"))
                            values.Add($"'Right Click to summon or dismiss {adapter.EscapeString(creatureName)}.'");
                        else
                            values.Add("'" + adapter.EscapeString(row[column.ToString()].ToString()) + "'");
                    }
                }
                var spellQuery = $"{EDIT_QUERY_OPERATION} INTO spell VALUES ({string.Join(", ", values)})";
                WriteLine("Creating new spell:\n " + spellQuery);
                adapter.Execute(spellQuery);
            }

            // Generate item
            uint newItemId = 0u;
            {
                var cloneItemId = 40654u;
                // Find by name
                using (var query = adapter.Query(
                    $"SELECT MIN(entry) FROM new_world.item_template WHERE entry BETWEEN 59800 AND 59935 AND `name` = 'Memory of {adapter.EscapeString(creatureName)}'"))
                {
                    if (query.Rows.Count > 0)
                    {
                        DataRow row = query.Rows[0];
                        uint.TryParse(row[0].ToString(), out newItemId);
                    }
                }
                // Find new ID
                if (newItemId == 0u)
                {
                    using (var query = adapter.Query("SELECT MIN(entry) FROM new_world.item_template WHERE entry BETWEEN 59800 AND 59935"))
                    {
                        DataRow row = query.Rows[0];
                        newItemId = uint.Parse(row[0].ToString()) - 1u;
                    }
                }
                var values = new List<string>();
                using (var query = adapter.Query("SELECT * FROM new_world.item_template WHERE entry = " + cloneItemId))
                {
                    DataRow row = query.Rows[0];
                    foreach (var column in row.Table.Columns)
                    {
                        var name = column.ToString();
                        if (name.Equals("entry"))
                            values.Add(newItemId.ToString());
                        else if (name.Equals("spellid_2"))
                            values.Add(newSpellId.ToString());
                        else if (name.Equals("name"))
                            values.Add($"'Memory of {adapter.EscapeString(creatureName)}'");
                        else if (name.Equals("VerifiedBuild"))
                            values.Add("0");
                        else
                            values.Add("'" + adapter.EscapeString(row[column.ToString()].ToString()) + "'");
                    }
                }
                var itemQuery = $"{EDIT_QUERY_OPERATION} INTO new_world.item_template VALUES ({string.Join(", ", values)})";
                WriteLine("Creating new item:\n " + itemQuery);
                adapter.Execute(itemQuery);

                WriteLine("Updating item.dbc record");
                adapter.Execute($"REPLACE INTO dbc.item " +
                    $"SELECT entry, class, subclass, soundoverridesubclass, Material, displayid, InventoryType, sheath " +
                    $"FROM new_world.item_template " +
                    $"WHERE entry = {newItemId}");
            }

            // creature_loot_template Entry can be the npcId
            {
                var alreadyInLoot = false;
                using (var query = adapter.Query($"SELECT * FROM new_world.creature_loot_template WHERE Entry = {npcId} AND Item = {newItemId}"))
                {
                    if (query.Rows.Count > 0)
                    {
                        foreach (DataRow row in query.Rows)
                        {
                            var itemId = uint.Parse(row["Item"].ToString());
                            if (itemId == newItemId)
                            {
                                alreadyInLoot = true;
                                break;
                            }
                        }
                    }
                }
                // If already in loot table, just update the existing record (e.g: drop rate change)
                if (alreadyInLoot)
                {
                    WriteLine("Updating existing loot entry data");
                    adapter.Execute($"UPDATE new_world.creature_loot_template SET Chance = {_dropChance} WHERE Entry = {npcId} AND Item = {newItemId}");
                    WriteLine("Updating creature to point to loot entry");
                    adapter.Execute($"UPDATE new_world.creature_template SET lootid = {npcId} WHERE entry = {npcId}");
                }
                // Otherwise we need to insert into the loot table and hook up to the npc
                else
                {
                    if (lootId == npcId)
                    {
                        WriteLine("Inserting new creature loot entry data and hooking up to base npc");
                        adapter.Execute($"INSERT INTO new_world.creature_loot_template VALUES ({npcId}, {newItemId}, 0, {_dropChance}, 0, 1, 0, 1, 1, 'Memory of {adapter.EscapeString(creatureName)} (PET)', {_requiredDungeonLevel})");
                        // ! This function is assuming that the creature did not already have a loot table !
                        adapter.Execute($"UPDATE new_world.creature_template SET lootid = {npcId} WHERE entry = {npcId}");
                    }
                    else
                    {
                        // We need to copy the existing loot entries to a new loot table
                        WriteLine("Copying over existing loot entries to creature loot ID");
                        adapter.Execute($"REPLACE INTO new_world.creature_loot_template " +
                            $"SELECT {npcId}, Item, Reference, Chance, QuestRequired, LootMode, GroupId, MinCount, MaxCount, `Comment`, RequiredDungeonLevel " +
                            $"FROM new_world.creature_loot_template WHERE Entry = {lootId}");
                        WriteLine("Inserting new item ID into loot table");
                        adapter.Execute($"REPLACE INTO new_world.creature_loot_template VALUES ({npcId}, {newItemId}, 0, {_dropChance}, 0, 1, 0, 1, 1, 'Memory of {adapter.EscapeString(creatureName)} (PET)', {_requiredDungeonLevel})");
                        WriteLine("Updating creature to use new loot entry");
                        adapter.Execute($"UPDATE new_world.creature_template SET lootid = {npcId} WHERE entry = {npcId}");
                    }
                }
            }

            // Generate achievement for pet
            {
                var achievementId = 0u;
                // Find by name
                using (var query = adapter.Query(
                    $"SELECT MAX(ID) FROM achievement WHERE `name1` = 'Memory of {adapter.EscapeString(creatureName)}'"))
                {
                    if (query.Rows.Count > 0)
                    {
                        DataRow row = query.Rows[0];
                        uint.TryParse(row[0].ToString(), out achievementId);
                    }
                }
                // Find new ID
                if (achievementId == 0u)
                {
                    using (var query = adapter.Query("SELECT MAX(ID) FROM achievement"))
                    {
                        DataRow row = query.Rows[0];
                        achievementId = uint.Parse(row[0].ToString()) + 1u;
                    }
                }
                var achievementQuery = $"{EDIT_QUERY_OPERATION} INTO achievement VALUES ({achievementId}, -1, -1, 0, " +
                    $"'Memory of {adapter.EscapeString(creatureName)}', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '16712190', " +
                    $"'Obtain the memory of {adapter.EscapeString(creatureName)}', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '16712190', " +
                    $"201, 5, 0, 0, 2303, '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '16712174', 0, 0)";
                WriteLine($"Updating achievement data\n {achievementQuery}");
                adapter.Execute(achievementQuery);

                // And same for achievemnt_criteria
                var achievementCriteriaId = 0u;
                // Find by name
                using (var query = adapter.Query(
                    $"SELECT MAX(ID) FROM achievement_criteria WHERE `name1` = 'Memory of {adapter.EscapeString(creatureName)}'"))
                {
                    if (query.Rows.Count > 0)
                    {
                        DataRow row = query.Rows[0];
                        uint.TryParse(row[0].ToString(), out achievementCriteriaId);
                    }
                }
                // Find new ID
                if (achievementCriteriaId == 0u)
                {
                    using (var query = adapter.Query("SELECT MAX(ID) FROM achievement_criteria"))
                    {
                        DataRow row = query.Rows[0];
                        achievementCriteriaId = uint.Parse(row[0].ToString()) + 1u;
                    }
                }

                var criteriaQuery = $"{EDIT_QUERY_OPERATION} INTO achievement_criteria VALUES ({achievementCriteriaId}, {achievementId}, 34, {newSpellId}, 0, 0, 0, 0, 0, " +
                    $"'Memory of {adapter.EscapeString(creatureName)}', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '16712190', 0, 0, 0, 0, 0)";
                WriteLine($"Updating achievement_criteria data\n {criteriaQuery}");
                adapter.Execute(criteriaQuery);
            }

            // Gameobject_template_loot handling
            if (_gobjectGenerateId.Contains(npcId))
            {
                var gobjectId = 0u;
                // Find by name
                using (var query = adapter.Query(
                    $"SELECT entry FROM new_world.gameobject_template WHERE `name` = '{adapter.EscapeString(creatureName)} Treasure'"))
                {
                    if (query.Rows.Count > 0)
                    {
                        DataRow row = query.Rows[0];
                        uint.TryParse(row[0].ToString(), out gobjectId);
                    }
                }
                // Find new ID
                if (gobjectId == 0u)
                {
                    using (var query = adapter.Query("SELECT MAX(entry) FROM new_world.gameobject_template WHERE entry between 50002 and 50200"))
                    {
                        DataRow row = query.Rows[0];
                        gobjectId = uint.Parse(row[0].ToString()) + 1u;
                    }
                }
                // Create gobject
                var gobjectQuery = $"{EDIT_QUERY_OPERATION} INTO new_world.gameobject_template VALUES ({gobjectId}, 3, 259, " +
                    $"'{adapter.EscapeString(creatureName)} Treasure', '', '', '', 1, 57, {gobjectId}, 604800000, 0, 0, 0, 0, 0, 0, 0, 0, " +
                    $"0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, '', '', '', 0)";
                // Create loot for object
                var gLootQuery = $"{EDIT_QUERY_OPERATION} INTO new_world.gameobject_loot_template VALUES " +
                    $"({gobjectId}, 1, 79998, 100, 0, 1, 0, 1, 1, 'Boss Loot Table - Shards', 0), " +
                    $"({gobjectId}, 2, 90010, 100, 0, 1, 2, 1, 1, 'Boss Loot Table - Rare Items', 0), " +
                    $"({gobjectId}, {newItemId}, 0, 1, 0, 0, 0, 1, 1, 'Memory of {adapter.EscapeString(creatureName)}', {_requiredDungeonLevel})";
                WriteLine($"Creating gobject:\n {gobjectQuery}");
                adapter.Execute(gobjectQuery);
                WriteLine($"Creating gobject loot:\n {gLootQuery}");
                adapter.Execute(gLootQuery);
                WriteLine($"[{npcId}] Removing lootId from creature_template since we created a gobject [{gobjectId}]. This will need to be attached by a script.");
                adapter.Execute($"UPDATE new_world.creature_template SET lootid = 0 WHERE entry = {npcId}");
            }

            // Special Handling
            {
                // Stromkar
                if (npcId == 52155)
                {
                    WriteLine("Setting Stromkar pet creature equipment");
                    adapter.Execute($"REPLACE INTO new_world.creature_equip_template VALUES ({newPetCreatureId}, 1, 45205, 0, 0, 0)");
                }
            }
        }

        private static void SpawnAdapters(ref List<IDatabaseAdapter> adapters)
        {
            var tasks = new List<Task<IDatabaseAdapter>>();
            int numBindings = BindingManager.GetInstance().GetAllBindings().Length;
            int numConnections = Math.Max(numBindings >= 2 ? 2 : 1, numBindings / 10);
            WriteLine($"Spawning {numConnections} adapters...");
            var timer = new Stopwatch();
            timer.Start();
            for (var i = 0; i < numConnections; ++i)
            {
                tasks.Add(Task.Run(() =>
                {
                    var adapter = AdapterFactory.Instance.GetAdapter(false);
                    WriteLine($"Spawned Adapter{Task.CurrentId}");
                    return adapter;
                }));
            }
            Task.WaitAll(tasks.ToArray());
            foreach (var task in tasks)
            {
                adapters.Add(task.Result);
            }
            timer.Stop();
            WriteLine($"Spawned {numConnections} adapters in {Math.Round(timer.Elapsed.TotalSeconds, 2)} seconds.");
        }
    }
}
