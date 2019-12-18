using System;

namespace SpellEditor.Sources.DBC
{
    class ItemSubClass : AbstractDBC
    {
        public ItemSubClassLookup[,] Lookups = new ItemSubClassLookup[29, 32];

        public ItemSubClass()
        {
            ReadDBCFile("DBC/ItemSubClass.dbc");

            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                ItemSubClassLookup temp;
                temp.ID = (uint)record["subClass"];
                temp.Name = GetAllLocaleStringsForField("displayName", record);
                Lookups[(uint) record["Class"], temp.ID] = temp;
            }
            Reader.CleanStringsMap();
            // In this DBC we don't actually need to keep the DBC data now that
            // we have extracted the lookup tables. Nulling it out may help with
            // memory consumption.
            Reader = null;
            Body = null;
        }

        public struct ItemSubClassLookup
        {
            public uint ID;
            public string Name;
        };
    }
}
