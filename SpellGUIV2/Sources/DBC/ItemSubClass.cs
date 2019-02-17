using System;
using SpellEditor.Sources.Database;

namespace SpellEditor.Sources.DBC
{
    class ItemSubClass : AbstractDBC
    {
        public ItemSubClassLookup[,] Lookups = new ItemSubClassLookup[29, 32];

        public ItemSubClass(MainWindow window, IDatabaseAdapter adapter)
        {
            try
            {
                ReadDBCFile("DBC/ItemSubClass.dbc");

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];
                    int locale = window.GetLanguage() + 1;
                    uint offset = (uint)record["displayName" + locale];
                    if (offset == 0)
                        continue;
                    ItemSubClassLookup temp;
                    temp.ID = (uint)record["subClass"];
                    temp.Name = Reader.LookupStringOffset(offset);
                    Lookups[(uint) record["Class"], temp.ID] = temp;
                }
                Reader.CleanStringsMap();
                // In this DBC we don't actually need to keep the DBC data now that
                // we have extracted the lookup tables. Nulling it out may help with
                // memory consumption.
                Reader = null;
                Body = null;
            }
            catch (Exception ex)
            {
                window.HandleErrorMessage(ex.Message);
                return;
            }
        }

        public struct ItemSubClassLookup
        {
            public uint ID;
            public string Name;
        };
    }
}
