using SpellEditor.Sources.Controls;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class SpellMechanic : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public SpellMechanic()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellMechanic.dbc");

            Lookups.Add(new DBCBoxContainer(0, "None", 0));

            int boxIndex = 1;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                var name = GetAllLocaleStringsForField("Name", record);
                var id = (uint) record["ID"];

                Lookups.Add(new DBCBoxContainer(id, name, boxIndex));

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

        public int UpdateMechanicSelection(uint ID)
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
    };
}
