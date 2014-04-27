using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices;

namespace SpellGUIV2
{
    class SpellDispelType
    {
        public SpellDBC_Header header;
        public DispelDBC_Map body;

        private MainWindow main;
        private SpellDBC spell;

        // Offset to string hash
        private Dictionary<int, int> offsetHashMap = new Dictionary<int, int>();
        // string hash to index
        private Dictionary<int, int> stringHashMap = new Dictionary<int, int>();
        // Index to ID
        public Dictionary<int, UInt32> IndexToIDMap = new Dictionary<int, UInt32>();

        public SpellDispelType(MainWindow window, SpellDBC theSpellDBC)
        {
            main = window;
            spell = theSpellDBC;

            if (!File.Exists("SpellDispelType.dbc"))
            {
                main.ERROR_STR = "SpellDispelType.dbc was not found!";
                return;
            }

            FileStream fs = new FileStream("SpellDispelType.dbc", FileMode.Open);
            // Read header
            int count = Marshal.SizeOf(typeof(SpellDBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fs);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (SpellDBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDBC_Header));
            handle.Free();

            body.records = new DispelDBC_Record[header.record_count];
            // Read body
            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                count = Marshal.SizeOf(typeof(DispelDBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fs);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (DispelDBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DispelDBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.string_block_size));

            reader.Close();
            fs.Close();

            int boxIndex = 0;

            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                int offset = (int)body.records[i].Name[0];
                if (offset == 0)
                    continue;
                int returnValue = offset;
                string toAdd = "";
                while (body.StringBlock[offset] != '\0')
                    toAdd += body.StringBlock[offset++];

                // Index to ID
                IndexToIDMap.Add(boxIndex, body.records[i].ID);
                // Hash to index
                stringHashMap.Add(toAdd.GetHashCode(), boxIndex++);
                // Offset to hash
                offsetHashMap.Add(returnValue, toAdd.GetHashCode());
                // Add to box
                main.DispelType.Items.Add(toAdd);
            }         
        }

        public void UpdateDispelSelection()
        {
            // When a record is loaded
            //// Get a DispelID
            //// DispelID points to string offset
            //// Set box selected index -> string.hash -> ID
            //// Get string hash from map of offset -> string

            int ID = (int)spell.body.records[main.selectedID].record.Dispel;

            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                if (ID == body.records[i].ID)
                {
                    main.DispelType.SelectedIndex = stringHashMap[offsetHashMap[(int)body.records[i].Name[0]]];
                    return;
                }
            }
        }

        public struct DispelDBC_Map
        {
            public DispelDBC_Record[] records;
            public string StringBlock;
        }

        public struct DispelDBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public UInt32[] Name;
            UInt32 mask;
            UInt32 immunityPossible;
            UInt32 internalName;
        }
    }
}
