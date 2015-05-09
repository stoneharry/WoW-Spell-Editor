using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class TotemCategory
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public TotemCategory_DBC_Map body;
        // End DBCs

        public TotemCategory(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Name = new UInt32[16];
                body.records[i].NameFlags = new UInt32();
                body.records[i].CategoryType = new UInt32();
                body.records[i].CategoryMask = new UInt32();
            }

            if (!File.Exists("DBC/TotemCategory.dbc"))
            {
                main.HandleErrorMessage("TotemCategory.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/TotemCategory.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new TotemCategory_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(TotemCategory_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (TotemCategory_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(TotemCategory_DBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

            reader.Close();
            fileStream.Close();

            body.lookup = new List<TotemCategoryLookup>();

            int boxIndex = 1;

            main.TotemCategory1.Items.Add("None");
            main.TotemCategory2.Items.Add("None");

            TotemCategoryLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[0];

                if (offset == 0) { continue; }

                int returnValue = offset;

                string toAdd = "";

                while (body.StringBlock[offset] != 0) { toAdd += body.StringBlock[offset++]; }

                TotemCategoryLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.TotemCategory1.Items.Add(toAdd);
                main.TotemCategory2.Items.Add(toAdd);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateTotemCategoriesSelection()
        {
            int[] IDs = { (int)spell.body.records[main.selectedID].record.TotemCategory1, (int)spell.body.records[main.selectedID].record.TotemCategory2 };

            for (int j = 0; j < IDs.Length; ++j)
            {
                int ID = IDs[j];

                if (ID == 0)
                {
                    switch (j)
                    {
                        case 0:
                            {
                                main.TotemCategory1.SelectedIndex = 0;

                                break;
                            }

                        case 1:
                            {
                                main.TotemCategory2.SelectedIndex = 0;

                                break;
                            }

                        default: { break; }
                    }

                    continue;
                }

                for (int i = 0; i < body.lookup.Count; ++i)
                {
                    if (ID == body.lookup[i].ID)
                    {
                        switch (j)
                        {
                            case 0:
                                {
                                    main.TotemCategory1.SelectedIndex = body.lookup[i].comboBoxIndex;

                                    break;
                                }

                            case 1:
                                {
                                    main.TotemCategory2.SelectedIndex = body.lookup[i].comboBoxIndex;

                                    break;
                                }

                            default: { break; }
                        }

                        continue;
                    }
                }
            }
        }

        public struct TotemCategory_DBC_Map
        {
            public TotemCategory_DBC_Record[] records;
            public List<TotemCategoryLookup> lookup;
            public string StringBlock;
        };

        public struct TotemCategoryLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct TotemCategory_DBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 NameFlags;
            public UInt32 CategoryType;
            public UInt32 CategoryMask;
        };
    };
}
