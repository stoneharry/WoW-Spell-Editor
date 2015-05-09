using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellCastTimes
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellCastTimes_DBC_Map body;
        // End DBCs

        public SpellCastTimes(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].CastingTime = new Int32();
                body.records[i].CastingTimePerLevel = new float();
                body.records[i].MinimumCastingTime = new Int32();
            }

            if (!File.Exists("DBC/SpellCastTimes.dbc"))
            {
                main.HandleErrorMessage("SpellCastTimes.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellCastTimes.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellCastTimes_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellCastTimes_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellCastTimes_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellCastTimes_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellCastTimeLookup>();

            int boxIndex = 1;

            main.CastTime.Items.Add(0);

            SpellCastTimeLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int castTime = (int)body.records[i].CastingTime;

                SpellCastTimeLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.CastTime.Items.Add(castTime);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateCastTimeSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.CastingTimeIndex;

            if (ID == 0)
            {
                main.CastTime.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.CastTime.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }
    }

    public struct SpellCastTimes_DBC_Map
    {
        public SpellCastTimes_DBC_Record[] records;
        public List<SpellCastTimeLookup> lookup;
    };

    public struct SpellCastTimeLookup
    {
        public int ID;
        public int comboBoxIndex;
    };

    public struct SpellCastTimes_DBC_Record
    {
        public UInt32 ID;
        public Int32 CastingTime;
        public float CastingTimePerLevel;
        public Int32 MinimumCastingTime;
    };
}
