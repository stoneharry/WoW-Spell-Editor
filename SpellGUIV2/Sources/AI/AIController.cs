using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.VersionControl;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.AI
{
    public class AIController
    {
        private readonly IDatabaseAdapter adapter;

        public AIController(IDatabaseAdapter adapter)
        {
            this.adapter = adapter;
        }

        public void ApplyAiSpellDefinition(AiSpellDefinition def, uint selectedID)
        {
            if (def == null)
                return;

            if (selectedID == 0)
                throw new InvalidOperationException("No spell selected. Select a spell before applying AI changes.");

            // Load the current spell row from the DB
            var query = $"SELECT * FROM `spell` WHERE `ID` = '{selectedID}' LIMIT 1";
            var table = adapter.Query(query);
            if (table.Rows.Count == 0)
                throw new Exception("Spell row not found for ID " + selectedID);

            var row = table.Rows[0];
            row.BeginEdit();

            // 1) Name / Description - using locale index 0 by default
            if (!string.IsNullOrWhiteSpace(def.Name))
                row["SpellName0"] = def.Name;

            if (!string.IsNullOrWhiteSpace(def.Description))
                row["SpellDescription0"] = def.Description;

            // 2) Range – convert yards to RangeIndex using SpellRange.dbc
            if (def.RangeYards.HasValue)
            {
                var rangeId = FindBestRangeIndex(def.RangeYards.Value);
                if (rangeId.HasValue)
                    row["RangeIndex"] = rangeId.Value;
            }

            // 3) Direct damage – assume Effect1 is the primary nuke and adjust EffectBasePoints1
            if (def.DirectDamage.HasValue)
            {
                // In Spell.dbc, actual damage is BasePoints + 1
                var basePoints = Math.Max(0, def.DirectDamage.Value - 1);
                try
                {
                    row["EffectBasePoints1"] = basePoints;
                }
                catch
                {
                    // In case the column is named slightly differently in bindings, you can tweak this.
                }
            }

            row.EndEdit();
            adapter.CommitChanges(query, table);
        }

        public uint CreateNewSpellFromAi(AiSpellDefinition def)
        {
            // 1) Allocate a new spell entry (find max ID + 1)
            var newId = FindNextAvailableSpellId();
            /*
                        // 2) Create a new DataRow in the SQL-based spell table
                        var table = adapter.Query("SELECT * FROM `spell` LIMIT 0");
                        var row = table.NewRow();

                        // Fill all columns with default values
                        foreach (DataColumn col in table.Columns)
                            row[col.ColumnName] = GetDefaultValue(col.DataType);

                        row["ID"] = newId;

                        table.Rows.Add(row);
                        adapter.CommitChanges("SELECT * FROM `spell`", table);

                        // 3) Now apply the AI definition same way we modify existing
                        ApplyAiDefinitionToRow(row, def);

                        // Save again
                        adapter.CommitChanges("SELECT * FROM `spell`", table);

                        // Reload new spell in UI
                        LoadSpecificSpell(newId);
            */
            return newId;
        }

        private uint FindNextAvailableSpellId()
        {
            var table = adapter.Query("SELECT MAX(ID) AS MaxId FROM `spell`");
            uint maxId = Convert.ToUInt32(table.Rows[0]["MaxId"]);
            return maxId + 1;
        }

        public void ApplyAiDefinitionToRow(DataRow row, AiSpellDefinition def)
        {
            if (def == null) return;

            row.BeginEdit();

            if (!string.IsNullOrWhiteSpace(def.Name))
                row["SpellName0"] = def.Name;

            if (!string.IsNullOrWhiteSpace(def.Description))
                row["SpellDescription0"] = def.Description;

            if (def.RangeYards.HasValue)
            {
                var rangeIndex = FindBestRangeIndex(def.RangeYards.Value);
                if (rangeIndex.HasValue)
                    row["RangeIndex"] = rangeIndex.Value;
            }

            if (def.DirectDamage.HasValue)
                row["EffectBasePoints1"] = Math.Max(0, def.DirectDamage.Value - 1);

            row.EndEdit();
        }

        /// <summary>
        /// Finds the SpellRange record whose maximum range is closest to the desired range.
        /// Uses the SpellRange DBC already loaded by DBCManager.
        /// </summary>
        private uint? FindBestRangeIndex(float desiredRangeYards)
        {
            var versionId = WoWVersionManager.GetInstance().SelectedVersion().Identity;
            if (versionId != 335)
            {
                // You can extend this for other versions later.
            }

            var spellRangeDbc = DBCManager.GetInstance().FindDbcForBinding("SpellRange") as SpellRange;
            if (spellRangeDbc == null)
                return null;

            var boxes = spellRangeDbc.GetAllBoxes();
            if (boxes == null || boxes.Count == 0)
                return null;

            double bestDiff = double.MaxValue;
            long bestId = -1;

            foreach (var box in boxes)
            {
                var rangeContainer = box as SpellRange.SpellRangeBoxContainer;
                if (rangeContainer == null)
                    continue;

                if (!float.TryParse(rangeContainer.RangeString, NumberStyles.Float, CultureInfo.InvariantCulture, out var maxRange))
                    continue;

                var diff = Math.Abs(maxRange - desiredRangeYards);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestId = rangeContainer.ID;
                }
            }

            return bestId >= 0 ? (uint?)bestId : null;
        }
    }
}
