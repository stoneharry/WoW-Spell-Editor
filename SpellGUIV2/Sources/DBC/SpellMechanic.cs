using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellMechanic
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public Mechanic_DBC_Map body;
        // End DBCs

        public SpellMechanic(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Name = new UInt32[16];
                body.records[i].NameFlags = new UInt32();
            }

            if (!File.Exists("DBC/SpellMechanic.dbc"))
            {
                main.HandleErrorMessage("SpellMechanic.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellMechanic.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new Mechanic_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(Mechanic_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (Mechanic_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Mechanic_DBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

            reader.Close();
            fileStream.Close();

            body.lookup = new List<MechanicLookup>();

            int boxIndex = 1;

            main.MechanicType.Items.Add("None");

            MechanicLookup t;

            t.ID = 0;
            t.offset = 0;
            t.stringHash = "None".GetHashCode();
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[0];

                if (offset == 0) { continue; }

                int returnValue = offset;

                string toAdd = "";

                while (body.StringBlock[offset] != '\0') { toAdd += body.StringBlock[offset++]; }

                MechanicLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.offset = returnValue;
                temp.stringHash = toAdd.GetHashCode();
                temp.comboBoxIndex = boxIndex;

                main.MechanicType.Items.Add(toAdd.Remove(1).ToUpper() + toAdd.Substring(1));

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateMechanicSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.Mechanic;

            if (ID == 0)
            {
                main.MechanicType.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.MechanicType.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct Mechanic_DBC_Map
        {
            public Mechanic_DBC_Record[] records;
            public List<MechanicLookup> lookup;
            public string StringBlock;
        };

        public struct MechanicLookup
        {
            public int ID;
            public int offset;
            public int stringHash;
            public int comboBoxIndex;
        };

        public struct Mechanic_DBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 NameFlags;
        };
    };
}
