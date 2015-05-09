using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class AreaGroup
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public AreaGroup_DBC_Map body;
        // End DBCs

        public AreaGroup(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].AreaID = new UInt32[6];
                body.records[i].NextGroup = new UInt32();
            }

            if (!File.Exists("DBC/AreaGroup.dbc"))
            {
                main.HandleErrorMessage("AreaGroup.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/AreaGroup.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new AreaGroup_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(AreaGroup_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (AreaGroup_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(AreaGroup_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<AreaGroupLookup>();

            int boxIndex = 1;

            main.AreaGroup.Items.Add(0);

            AreaGroupLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int id = (int)body.records[i].ID;

                AreaGroupLookup temp;

                temp.ID = id;
                temp.comboBoxIndex = boxIndex;

                main.AreaGroup.Items.Add(id);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateAreaGroupSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.AreaGroupID;

            if (ID == 0)
            {
                main.AreaGroup.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.AreaGroup.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }
    }

    public struct AreaGroup_DBC_Map
    {
        public AreaGroup_DBC_Record[] records;
        public List<AreaGroupLookup> lookup;
    };

    public struct AreaGroupLookup
    {
        public int ID;
        public int comboBoxIndex;
    };

    public struct AreaGroup_DBC_Record
    {
        public UInt32 ID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public UInt32[] AreaID;
        public UInt32 NextGroup;
    };
}
