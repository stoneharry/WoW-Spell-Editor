using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices;

namespace SpellGUIV2
{
    class SpellRange
    {
        public SpellDBC_Header header;
        public SpellRangeDBC_Map body;

        private MainWindow main;
        private SpellDBC spell;

        public SpellRange(MainWindow window, SpellDBC theSpellDBC)
        {
            main = window;
            spell = theSpellDBC;

            if (!File.Exists("SpellRange.dbc"))
            {
                main.ERROR_STR = "SpellRange.dbc was not found!";
                return;
            }

            FileStream fs = new FileStream("SpellRange.dbc", FileMode.Open);
            // Read header
            int count = Marshal.SizeOf(typeof(SpellDBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fs);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (SpellDBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDBC_Header));
            handle.Free();

            body.records = new SpellRangeDBC_Record[header.record_count];
            // Read body
            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellRangeDBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fs);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellRangeDBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellRangeDBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.string_block_size));

            reader.Close();
            fs.Close();

            body.lookup = new List<SpellRange_Lookup>();
            int boxIndex = 0;

            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                int offset = (int)body.records[i].Name[0];
                if (offset == 0)
                    continue;
                int returnValue = offset;
                string toAdd = body.records[i].ID + " - ";
                while (body.StringBlock[offset] != '\0')
                    toAdd += body.StringBlock[offset++];

                SpellRange_Lookup temp;
                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.SpellRange.Items.Add(toAdd);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void updateSpellRangeSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.rangeIndex;
            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.SpellRange.SelectedIndex = body.lookup[i].comboBoxIndex;
                    return;
                }
            }
        }

        public struct SpellRangeDBC_Map
        {
            public SpellRangeDBC_Record[] records;
            public List<SpellRange_Lookup> lookup;
            public string StringBlock;
        }

        public struct SpellRange_Lookup
        {
            public int ID;
            public int comboBoxIndex;
        }

        public struct SpellRangeDBC_Record
        {
            public UInt32 ID;
            public float minRangeHostile;
            public float minRangeFriend;
            public float maxRangeHostile;
            public float maxRangeFriend;
            public Int32 type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public UInt32[] Name;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public UInt32[] ShortName;
        }
    }
}
