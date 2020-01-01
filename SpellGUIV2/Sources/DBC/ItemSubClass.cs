using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class ItemSubClass : AbstractDBC
    {
        public Dictionary<string, ItemSubClassLookup> Lookups = new Dictionary<string, ItemSubClassLookup>();

        public ItemSubClassLookup LookupClassAndSubclass(long clazz, uint subclass)
        {
            return Lookups.TryGetValue(GetLookupKey(clazz, subclass), out var result) ? result : new ItemSubClassLookup();
        }

        public ItemSubClass()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\ItemSubClass.dbc");

            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                ItemSubClassLookup temp;
                temp.ID = (uint)record["subClass"];
                temp.Name = GetAllLocaleStringsForField("displayName", record);
                Lookups.Add($"{(uint)record["Class"]}-{temp.ID}", temp);
            }
            Reader.CleanStringsMap();
            // In this DBC we don't actually need to keep the DBC data now that
            // we have extracted the lookup tables. Nulling it out may help with
            // memory consumption.
            Reader = null;
            Body = null;
        }

        private string GetLookupKey(long clazz, uint subclass)
        {
            return $"{ clazz }-{ subclass }";
        }

        public struct ItemSubClassLookup
        {
            public uint ID;
            public string Name;
        };
    }
}
