using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class AreaTable : AbstractDBC
    {
        public Dictionary<uint, AreaTableLookup> Lookups;

        public AreaTable(MainWindow window)
        {
            try
            {
                ReadDBCFile("DBC/AreaTable.dbc");

                Lookups = new Dictionary<uint, AreaTableLookup>();

                for (uint i = 0; i < Header.RecordCount; ++i) 
                {
                    var record = Body.RecordMaps[i];
                    int locale = window.GetLanguage() + 1;
                    AreaTableLookup temp;
                    temp.ID = (uint) record["ID"];
                    temp.AreaName = Reader.LookupStringOffset((uint) record["Name" + locale]);
                    Lookups.Add(temp.ID, temp);
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

        public struct AreaTableLookup
        {
            public uint ID;
            public string AreaName;
        };
    };
}
