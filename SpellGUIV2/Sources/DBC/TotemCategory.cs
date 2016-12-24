using SpellEditor.Sources.Config;
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
        private DBAdapter adapter;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public TotemCategory_DBC_Map body;
        // End DBCs

        public TotemCategory(MainWindow window, DBAdapter adapter)
        {
            this.main = window;
			this.adapter = adapter;

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

            body.StringBlock =reader.ReadBytes(header.StringBlockSize);

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
				int offset = (int)body.records[i].Name[window.GetLanguage()];

                if (offset == 0) { continue; }

                int returnValue = offset;

				System.Collections.ArrayList al = new System.Collections.ArrayList(); 

                while (body.StringBlock[offset] != 0) { al.Add(body.StringBlock[offset++]); }

				byte[] toAdd = new byte[al.Count];
				int n = 0;
				foreach (byte o in al){ toAdd[n++] = o;}

                TotemCategoryLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

				main.TotemCategory1.Items.Add(Encoding.UTF8.GetString(toAdd));
				main.TotemCategory2.Items.Add(Encoding.UTF8.GetString(toAdd));

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateTotemCategoriesSelection()
        {
			var result = adapter.query(string.Format("SELECT `TotemCategory1`, `TotemCategory2` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0];
            int[] IDs = { Int32.Parse(result[0].ToString()), Int32.Parse(result[1].ToString()) };

            for (int j = 0; j < IDs.Length; ++j)
            {
                int ID = IDs[j];

                if (ID == 0)
                {
                    switch (j)
                    {
                        case 0:
                            {
                                main.TotemCategory1.threadSafeIndex = 0;

                                break;
                            }

                        case 1:
                            {
                                main.TotemCategory2.threadSafeIndex = 0;

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
                                    main.TotemCategory1.threadSafeIndex = body.lookup[i].comboBoxIndex;

                                    break;
                                }

                            case 1:
                                {
                                    main.TotemCategory2.threadSafeIndex = body.lookup[i].comboBoxIndex;

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
            public byte[] StringBlock;
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
