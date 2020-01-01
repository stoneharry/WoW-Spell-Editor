using SpellEditor.Sources.Controls;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{

    class SpellCategory : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public SpellCategory()
        {

            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellCategory.dbc");

            int boxIndex = 1;
            Lookups.Add(new DBCBoxContainer(0, "0", 0));

            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                uint id = (uint) record["ID"];

                Lookups.Add(new DBCBoxContainer(id, id.ToString(), boxIndex));

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

        public int UpdateCategorySelection(uint ID)
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
