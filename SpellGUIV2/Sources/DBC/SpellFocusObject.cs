using SpellEditor.Sources.Config;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpellEditor.Sources.DBC
{
    class SpellFocusObject : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        public List<SpellFocusObjectLookup> Lookups = new List<SpellFocusObjectLookup>();

        public SpellFocusObject(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile<SpellFocusObject_DBC_Record>("DBC/SpellFocusObject.dbc");

                int boxIndex = 1;
                main.RequiresSpellFocus.Items.Add("None");
                SpellFocusObjectLookup t;
                t.ID = 0;
                t.offset = 0;
                t.stringHash = "None".GetHashCode();
                t.comboBoxIndex = 0;
                Lookups.Add(t);
                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];

                    uint offset = ((uint[])record["Name"])[window.GetLanguage()];
                    if (offset == 0)
                        continue;
                    string name = Reader.LookupStringOffset(offset);

                    SpellFocusObjectLookup temp;
                    temp.ID = (uint) record["ID"];
                    temp.offset = offset;
                    temp.stringHash = name.GetHashCode();
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    main.RequiresSpellFocus.Items.Add(name);

                    boxIndex++;
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

        public void UpdateSpellFocusObjectSelection()
        {
            uint ID = uint.Parse(adapter.Query(string.Format("SELECT `RequiresSpellFocus` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0][0].ToString());
            if (ID == 0)
            {
                main.RequiresSpellFocus.threadSafeIndex = 0;
                return;
            }

            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.RequiresSpellFocus.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }

        public struct SpellFocusObjectLookup
        {
            public uint ID;
            public uint offset;
            public int stringHash;
            public int comboBoxIndex;
        };

        public struct SpellFocusObject_DBC_Record
        {
// These fields are used through reflection, disable warning
#pragma warning disable 0649
#pragma warning disable 0169
            public uint ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public uint[] Name;
            public uint Flags;
#pragma warning restore 0649
#pragma warning restore 0169
        };
    };
}
