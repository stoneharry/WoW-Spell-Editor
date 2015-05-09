using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellRange
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellRange_DBC_Map body;
        // End DBCs

        public SpellRange(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].MinimumRangeHostile = new float();
                body.records[i].MinimumRangeFriend = new float();
                body.records[i].MaximumRangeHostile = new float();
                body.records[i].MaximumRangeFriend = new float();
                body.records[i].Type = new Int32();
                body.records[i].Name = new UInt32[16];
                body.records[i].NameFlags = new UInt32();
                body.records[i].ShortName = new UInt32[16];
                body.records[i].ShortNameFlags = new UInt32();
            }

            if (!File.Exists("DBC/SpellRange.dbc"))
            {
                main.HandleErrorMessage("SpellRange.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellRange.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellRange_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellRange_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellRange_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellRange_DBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellRangeLookup>();

            int boxIndex = 0;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[0];
                int MinimumRangeHostile = (int)body.records[i].MinimumRangeHostile;
                int MaximumRangeHostile = (int)body.records[i].MaximumRangeHostile;
                int MinimumRangeFriend = (int)body.records[i].MinimumRangeFriend;
                int MaximumRangeFriend = (int)body.records[i].MaximumRangeFriend;

                if (offset == 0) { continue; }

                int returnValue = offset;

                string toAdd = "";

                while (body.StringBlock[offset] != 0) { toAdd += body.StringBlock[offset++]; }

                SpellRangeLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.Range.Items.Add(toAdd + "\t\t - " + "Hostile: " + MinimumRangeHostile + " - " + MaximumRangeHostile + "\t Friend: " + MinimumRangeFriend + " - " + MaximumRangeFriend);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateSpellRangeSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.RangeIndex;

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.Range.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct SpellRange_DBC_Map
        {
            public SpellRange_DBC_Record[] records;
            public List<SpellRangeLookup> lookup;
            public string StringBlock;
        };

        public struct SpellRangeLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct SpellRange_DBC_Record
        {
            public UInt32 ID;
            public float MinimumRangeHostile;
            public float MinimumRangeFriend;
            public float MaximumRangeHostile;
            public float MaximumRangeFriend;
            public Int32 Type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 NameFlags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] ShortName;
            public UInt32 ShortNameFlags;
        };
    };
}
