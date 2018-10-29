﻿using SpellEditor.Sources.Config;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpellEditor.Sources.DBC
{
    class SpellMechanic : AbstractDBC
    {
        private MainWindow main;
        private DBAdapter adapter;

        public List<MechanicLookup> Lookups = new List<MechanicLookup>();

        public SpellMechanic(MainWindow window, DBAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile<Mechanic_DBC_Record>("DBC/SpellMechanic.dbc");

                int boxIndex = 1;
                main.MechanicType.Items.Add("None");
                MechanicLookup t;
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
                    string name = reader.LookupStringOffset(offset);

                    MechanicLookup temp;
                    temp.ID = (uint) record["ID"];
                    temp.offset = offset;
                    temp.stringHash = name.GetHashCode();
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    main.MechanicType.Items.Add(name);

                    ++boxIndex;
                }
                reader.CleanStringsMap();
                // In this DBC we don't actually need to keep the DBC data now that
                // we have extracted the lookup tables. Nulling it out may help with
                // memory consumption.
                reader = null;
                Body = null;
            }
            catch (Exception ex)
            {
                window.HandleErrorMessage(ex.Message);
                return;
            }
        }

        public void UpdateMechanicSelection()
        {
            uint ID = uint.Parse(adapter.query(string.Format("SELECT `Mechanic` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0][0].ToString());
            if (ID == 0)
            {
                main.MechanicType.threadSafeIndex = 0;
                return;
            }
            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.MechanicType.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }

        public struct MechanicLookup
        {
            public uint ID;
            public uint offset;
            public int stringHash;
            public int comboBoxIndex;
        };

        public struct Mechanic_DBC_Record
        {
// These fields are used through reflection, disable warning
#pragma warning disable 0649
#pragma warning disable 0169
            public uint ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public uint[] Name;
            public uint NameFlags;
#pragma warning restore 0649
#pragma warning restore 0169
        };
    };
}
