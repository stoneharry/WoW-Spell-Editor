using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellDuration
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellDuration_DBC_Body body;
        // End DBCs

        public SpellDuration(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].BaseDuration = new Int32();
                body.records[i].PerLevel = new Int32();
                body.records[i].MaximumDuration = new Int32();
            }

            if (!File.Exists("DBC/SpellDuration.dbc"))
            {
                main.HandleErrorMessage("SpellDuration.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellDuration.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();
            body.records = new SpellDurationRecord[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellDurationRecord));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellDurationRecord)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDurationRecord));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellDurationLookup>();

            int boxIndex = 1;

            main.Duration.Items.Add(0);

            SpellDurationLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int baseDuration = (int)body.records[i].BaseDuration;

                SpellDurationLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.Duration.Items.Add(baseDuration);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateDurationIndexes()
        {
            int ID = (int)spell.body.records[main.selectedID].record.DurationIndex;

            if (ID == 0)
            {
                main.Duration.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.Duration.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct SpellDuration_DBC_Body
        {
            public SpellDurationRecord[] records;
            public List<SpellDurationLookup> lookup;
        };

        public struct SpellDurationLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct SpellDurationRecord
        {
            public UInt32 ID;
            public Int32 BaseDuration;
            public Int32 PerLevel;
            public Int32 MaximumDuration;
        };
    };
}
