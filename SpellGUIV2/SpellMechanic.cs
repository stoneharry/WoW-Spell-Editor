using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices;

namespace SpellGUIV2
{
    class SpellMechanic
    {
        public SpellDBC_Header header;
        public MechanicDBC_Map body;

        private MainWindow main;
        private SpellDBC spell;

        public SpellMechanic(MainWindow window, SpellDBC theSpellDBC)
        {
            main = window;
            spell = theSpellDBC;

            if (!File.Exists("SpellMechanic.dbc"))
            {
                main.ERROR_STR = "SpellMechanic.dbc was not found!";
                return;
            }

            FileStream fs = new FileStream("SpellMechanic.dbc", FileMode.Open);
            // Read header
            int count = Marshal.SizeOf(typeof(SpellDBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fs);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (SpellDBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDBC_Header));
            handle.Free();

            body.records = new MechanicDBC_Record[header.record_count];
            // Read body
            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                count = Marshal.SizeOf(typeof(MechanicDBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fs);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (MechanicDBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(MechanicDBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.string_block_size));

            reader.Close();
            fs.Close();
            
            body.lookup = new List<MechanicLookup>();
            int boxIndex = 1;

            main.MechanicType.Items.Add("None");
            MechanicLookup t;
            t.ID = 0;
            t.offset = 0;
            t.stringHash = "None".GetHashCode();
            t.comboBoxIndex = 0;
            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                int offset = (int)body.records[i].Name[0];
                if (offset == 0)
                    continue;
                int returnValue = offset;
                string toAdd = "";
                while (body.StringBlock[offset] != '\0')
                    toAdd += body.StringBlock[offset++];

                MechanicLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.stringHash = toAdd.GetHashCode();
                temp.offset = returnValue;
                temp.comboBoxIndex = boxIndex;

                main.MechanicType.Items.Add(toAdd);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void updateMechanicSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.Mechanic;
            if (ID == 0)
                main.MechanicType.SelectedIndex = 0;
            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.MechanicType.SelectedIndex = body.lookup[i].comboBoxIndex;
                    return;
                }
            }
        }

        public struct MechanicDBC_Map
        {
            public MechanicDBC_Record[] records;
            public List<MechanicLookup> lookup;
            public string StringBlock;
        }

        public struct MechanicLookup
        {
            public int ID;
            public int offset;
            public int stringHash;
            public int comboBoxIndex;
        }

        public struct MechanicDBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public UInt32[] Name;
        }
    }
}
