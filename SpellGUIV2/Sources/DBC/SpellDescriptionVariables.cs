using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellDescriptionVariables
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellDescriptionVariables_DBC_Map body;
        // End DBCs

        public SpellDescriptionVariables(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Formula = new UInt32();
            }

            if (!File.Exists("DBC/SpellDescriptionVariables.dbc"))
            {
                main.HandleErrorMessage("SpellDescriptionVariables.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellDescriptionVariables.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellDescriptionVariables_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellDescriptionVariables_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellDescriptionVariables_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDescriptionVariables_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellDescriptionVariablesLookup>();

            int boxIndex = 1;

            main.SpellDescriptionVariables.Items.Add(0);

            SpellDescriptionVariablesLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int id = (int)body.records[i].ID;

                SpellDescriptionVariablesLookup temp;

                temp.ID = id;
                temp.comboBoxIndex = boxIndex;

                main.SpellDescriptionVariables.Items.Add(id);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateSpellDescriptionVariablesSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.SpellDescriptionVariableID;

            if (ID == 0)
            {
                main.SpellDescriptionVariables.threadSafeIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.SpellDescriptionVariables.threadSafeIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct SpellDescriptionVariables_DBC_Map
        {
            public SpellDescriptionVariables_DBC_Record[] records;
            public List<SpellDescriptionVariablesLookup> lookup;
        };

        public struct SpellDescriptionVariablesLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct SpellDescriptionVariables_DBC_Record
        {
            public UInt32 ID;
            public UInt32 Formula;
        };
    };
}
