using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices;

namespace SpellGUIV2
{
    class SpellDuration
    {
        public SpellDBC_Header header;
        public SpellDurationMap body;

        private MainWindow main;
        private SpellDBC spell;

        public SpellDuration(MainWindow window, SpellDBC theSpellDBC)
        {
            main = window;
            spell = theSpellDBC;

            if (!File.Exists("SpellDuration.dbc"))
            {
                main.ERROR_STR = "SpellDuration.dbc was not found!";
                return;
            }

            FileStream fs = new FileStream("SpellDuration.dbc", FileMode.Open);
            // Read header
            int count = Marshal.SizeOf(typeof(SpellDBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fs);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (SpellDBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDBC_Header));
            handle.Free();

            body.records = new SpellDurationRecord[header.record_count];
            // Read body
            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellDurationRecord));
                readBuffer = new byte[count];
                reader = new BinaryReader(fs);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellDurationRecord)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDurationRecord));
                handle.Free();
            }

            reader.Close();
            fs.Close();

            body.lookup = new List<DurationLookup>();

            main.SpellDuration.Items.Add(0);

            int boxIndex = 1;
            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                DurationLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.SpellDuration.Items.Add(body.records[i].BaseDuration);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void updateDurationIndexes()
        {
            int ID = (int)spell.body.records[main.selectedID].record.DurationIndex;
            if (ID == 0)
            {
                main.SpellDuration.SelectedIndex = 0;
                return;
            }
            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.SpellDuration.SelectedIndex = body.lookup[i].comboBoxIndex;
                    return;
                }
            }
        }

        public struct SpellDurationMap
        {
            public SpellDurationRecord[] records;
            public List<DurationLookup> lookup;
        }

        public struct DurationLookup
        {
            public int ID;
            public int comboBoxIndex;
        }

        public struct SpellDurationRecord
        {
            public UInt32 ID;
            public Int32 BaseDuration;
            public Int32 PerLevel;
            public Int32 MaxDuration;
        }
    }
}
