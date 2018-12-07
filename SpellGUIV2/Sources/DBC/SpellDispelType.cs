using SpellEditor.Sources.Config;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpellEditor.Sources.DBC
{
    class SpellDispelType : AbstractDBC
    {
        private MainWindow main;
        private DBAdapter adapter;

        public List<SpellDispel_DBC_Lookup> Lookups = new List<SpellDispel_DBC_Lookup>();

        public SpellDispelType(MainWindow window, DBAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile<SpellDispel_DBC_Record>("DBC/SpellDispelType.dbc");

                int boxIndex = 0;
                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];
                    uint offset = ((uint[])record["Name"])[window.GetLanguage()];
                    if (offset == 0)
                    {
                        continue;
                    }
                    var description = Reader.LookupStringOffset(offset);

                    SpellDispel_DBC_Lookup temp;
                    temp.ID = (uint)record["ID"];
                    temp.offset = offset;
                    temp.stringHash = description.GetHashCode();
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    main.DispelType.Items.Add(description);

                    ++boxIndex;
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

        public void UpdateDispelSelection()
        {
            uint ID = uint.Parse(adapter.query(string.Format("SELECT `Dispel` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0][0].ToString());
            if (ID == 0)
            {
                main.DispelType.threadSafeIndex = 0;
                return;
            }
            for (int i = 0; i < Header.RecordCount; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.DispelType.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }

        public struct SpellDispel_DBC_Lookup
        {
            public uint ID;
            public uint offset;
            public int stringHash;
            public int comboBoxIndex;
        };

        [Serializable]
        public struct SpellDispel_DBC_Record
        {
// These fields are used through reflection, disable warning
#pragma warning disable 0649
#pragma warning disable 0169
            public uint ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public uint[] Name;
            public uint NameFlags;
            public uint Combinations;
            public uint ImmunityPossible;
            public uint InternalName;
#pragma warning restore 0649
#pragma warning restore 0169
        };
    };
}
