using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices;

namespace SpellGUIV2
{
    class SpellCastTimes
    {
        public SpellDBC_Header header;
        public SpellCastTimesDBC_Map body;

        private MainWindow main;
        private SpellDBC spell;

        public SpellCastTimes(MainWindow window, SpellDBC theSpellDBC)
        {
            main = window;
            spell = theSpellDBC;

            if (!File.Exists("SpellCastTimes.dbc"))
            {
                main.ERROR_STR = "SpellCastTimes.dbc was not found!";
                return;
            }

            FileStream fs = new FileStream("SpellCastTimes.dbc", FileMode.Open);
            // Read header
            int count = Marshal.SizeOf(typeof(SpellDBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fs);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (SpellDBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDBC_Header));
            handle.Free();

            body.records = new SpellCastTimesDBC_Record[header.record_count];
            // Read body
            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellCastTimesDBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fs);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellCastTimesDBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellCastTimesDBC_Record));
                handle.Free();
            }

            reader.Close();
            fs.Close();

            body.lookup = new List<CastTimeLookup>();

            int boxIndex = 0;
            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                CastTimeLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.CastTime.Items.Add(body.records[i].CastTime);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void updateCastTimeSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.CastingTimeIndex;
            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.CastTime.SelectedIndex = body.lookup[i].comboBoxIndex;
                    return;
                }
            }
        }

        public struct SpellCastTimesDBC_Map
        {
            public SpellCastTimesDBC_Record[] records;
            public List<CastTimeLookup> lookup;
        }

        public struct CastTimeLookup
        {
            public int ID;
            public int comboBoxIndex;
        }

        public struct SpellCastTimesDBC_Record
        {
            public UInt32 ID;
            public Int32 CastTime;
            public float CastTimePerLevel;
            public Int32 MinCastTime;
        }
    }
}
