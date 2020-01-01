using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class SpellRuneCost : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public SpellRuneCost()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellRuneCost.dbc");

            Lookups.Add(new DBCBoxContainer(0, "0", 0));

            int boxIndex = 1;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];

                uint id = (uint) record["ID"];
                uint runeCost1 = (uint)record["RuneCost1"];
                uint runeCost2 = (uint)record["RuneCost2"];
                uint runeCost3 = (uint)record["RuneCost3"];
                uint runepowerGain = (uint) record["RunePowerGain"];
                string name = $"Cost {runeCost1}, {runeCost2}, {runeCost3} Gain { runepowerGain } ID { id }";

                Lookups.Add(new DBCBoxContainer(id, name, boxIndex));

                //RuneCost.Items.Add();

                ++boxIndex;
            }
            Reader.CleanStringsMap();
            // In this DBC we don't actually need to keep the DBC data now that
            // we have extracted the lookup tables. Nulling it out may help with
            // memory consumption.
            Reader = null;
            Body = null;
        }

        public List<DBCBoxContainer> GetAllBoxes()
        {
            return Lookups;
        }

        public int UpdateSpellRuneCostSelection(uint ID)
        {
            var match = Lookups.Find((entry) => entry.ID == ID);
            return match != null ? match.ComboBoxIndex : 0;
        }
    };
}
