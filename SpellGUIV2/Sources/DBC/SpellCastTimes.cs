using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class SpellCastTimes : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public SpellCastTimes()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellCastTimes.dbc");

            int boxIndex = 0;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                var id = (uint)record["ID"];
                var castTime = new Label();
                castTime.Content = record["CastingTime"].ToString() + "\t";
                castTime.ToolTip = "CastTime\t\t" + record["CastingTime"] + "\n" +
                    "PerLevel\t\t" + record["CastingTimePerLevel"] + "\n" +
                    "MinimumCastingTime\t" + record["MinimumCastingTime"] + "\n";

                Lookups.Add(new DBCBoxContainer(id, castTime, boxIndex));
                boxIndex++;
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

        public List<DBCBoxContainer> GetBoxItems() => Lookups;

        public int UpdateCastTimeSelection(uint ID)
        {
            if (ID == 0)
            {
                return 0;
            }
            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    return Lookups[i].ComboBoxIndex;
                }
            }
            return 0;
        }
    }
}
