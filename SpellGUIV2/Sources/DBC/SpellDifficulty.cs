using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellDifficulty
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellDifficulty_DBC_Map body;
        // End DBCs

        public SpellDifficulty(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Difficulties = new UInt32[4];
            }

            if (!File.Exists("DBC/SpellDifficulty.dbc"))
            {
                main.HandleErrorMessage("SpellDifficulty.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellDifficulty.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellDifficulty_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellDifficulty_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellDifficulty_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDifficulty_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellDifficultyLookup>();

            int boxIndex = 1;

            main.Difficulty.Items.Add(0);

            SpellDifficultyLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int id = (int)body.records[i].ID;

                SpellDifficultyLookup temp;

                temp.ID = id;
                temp.comboBoxIndex = boxIndex;

                main.Difficulty.Items.Add(id);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateDifficultySelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.SpellDifficultyID;

            if (ID == 0)
            {
                main.Difficulty.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.Difficulty.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }
    }

    public struct SpellDifficulty_DBC_Map
    {
        public SpellDifficulty_DBC_Record[] records;
        public List<SpellDifficultyLookup> lookup;
    };

    public struct SpellDifficultyLookup
    {
        public int ID;
        public int comboBoxIndex;
    };

    public struct SpellDifficulty_DBC_Record
    {
        public UInt32 ID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public UInt32[] Difficulties;
    };
}
