using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices;

namespace SpellGUIV2
{
    class SpellRadius
    {
        public SpellDBC_Header header;
        public SpellRadiusMap body;

        private MainWindow main;
        private SpellDBC spell;

        public SpellRadius(MainWindow window, SpellDBC theSpellDBC)
        {
            main = window;
            spell = theSpellDBC;

            if (!File.Exists("SpellRadius.dbc"))
            {
                main.ERROR_STR = "SpellRadius.dbc was not found!";
                return;
            }

            FileStream fs = new FileStream("SpellRadius.dbc", FileMode.Open);
            // Read header
            int count = Marshal.SizeOf(typeof(SpellDBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fs);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (SpellDBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDBC_Header));
            handle.Free();

            body.records = new SpellRadiusRecord[header.record_count];
            // Read body
            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellRadiusRecord));
                readBuffer = new byte[count];
                reader = new BinaryReader(fs);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellRadiusRecord)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellRadiusRecord));
                handle.Free();
            }

            reader.Close();
            fs.Close();

            body.lookup = new List<RadiusLookup>();

            main.RadiusIndex1.Items.Add("0");
            main.RadiusIndex2.Items.Add("0");
            main.RadiusIndex3.Items.Add("0");

            int boxIndex = 1;
            for (UInt32 i = 0; i < header.record_count; ++i)
            {
                RadiusLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.RadiusIndex1.Items.Add(body.records[i].radius);
                main.RadiusIndex2.Items.Add(body.records[i].radius);
                main.RadiusIndex3.Items.Add(body.records[i].radius);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void updateRadiusIndexes()
        {
            int[] IDs = {(int)spell.body.records[main.selectedID].record.EffectRadiusIndex1,
                            (int)spell.body.records[main.selectedID].record.EffectRadiusIndex2,
                            (int)spell.body.records[main.selectedID].record.EffectRadiusIndex3};
            main.newRadiusIndex[0] = spell.body.records[main.selectedID].record.EffectRadiusIndex1;
            main.newRadiusIndex[1] = spell.body.records[main.selectedID].record.EffectRadiusIndex2;
            main.newRadiusIndex[2] = spell.body.records[main.selectedID].record.EffectRadiusIndex3;
            for (int j = 0; j < IDs.Length; ++j)
            {
                int ID = IDs[j];
                if (ID == 0)
                {
                    if (j == 0) main.RadiusIndex1.SelectedIndex = 0;
                    else if (j == 1) main.RadiusIndex2.SelectedIndex = 0;
                    else if (j == 2) main.RadiusIndex3.SelectedIndex = 0;
                    continue;
                }
                for (int i = 0; i < body.lookup.Count; ++i)
                {
                    if (ID == body.lookup[i].ID)
                    {
                        if (j == 0) main.RadiusIndex1.SelectedIndex = body.lookup[i].comboBoxIndex;
                        else if (j == 1) main.RadiusIndex2.SelectedIndex = body.lookup[i].comboBoxIndex;
                        else if (j == 2) main.RadiusIndex3.SelectedIndex = body.lookup[i].comboBoxIndex;
                        continue;
                    }
                }
            }
        }

        public void updateIndexesSave()
        {
            int[] IDs = { main.RadiusIndex1.SelectedIndex, main.RadiusIndex2.SelectedIndex, main.RadiusIndex3.SelectedIndex };
            for (int j = 0; j < 3; ++j)
            {
                if (IDs[j] == 0)
                {
                    main.newRadiusIndex[j] = 0;
                    continue;
                }
                for (int i = 0; i < body.lookup.Count; ++i)
                {
                    if (IDs[j] == body.lookup[i].comboBoxIndex)
                    {
                        main.newRadiusIndex[j] = (UInt32)body.lookup[i].ID;
                        continue;
                    }
                }
            }
        }

        public struct SpellRadiusMap
        {
            public SpellRadiusRecord[] records;
            public List<RadiusLookup> lookup;
        }

        public struct RadiusLookup
        {
            public int ID;
            public int comboBoxIndex;
        }

        public struct SpellRadiusRecord
        {
            public UInt32 ID;
            public float radius;
            public float radiusPerLevel;
            public float maxRadius;
        }
    }
}
