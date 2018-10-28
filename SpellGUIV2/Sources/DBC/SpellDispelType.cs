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
    class SpellDispelType : AbstractDBC
    {
        // Begin Window
        private MainWindow main;
        private DBAdapter adapter;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellDispel_DBC_Map body;
        // End DBCs

        public SpellDispelType(MainWindow window, DBAdapter adapter)
        {
            this.main = window;
            this.adapter = adapter;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Name = new UInt32[16];
                body.records[i].NameFlags = new UInt32();
                body.records[i].Combinations = new UInt32();
                body.records[i].ImmunityPossible = new UInt32();
                body.records[i].InternalName = new UInt32();
            }

            if (!File.Exists("DBC/SpellDispelType.dbc"))
            {
                main.HandleErrorMessage("SpellDispelType.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellDispelType.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellDispel_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellDispel_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellDispel_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDispel_DBC_Record));
                handle.Free();
            }

            body.StringBlock = reader.ReadBytes(header.StringBlockSize);

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellDispel_DBC_Lookup>();

            int boxIndex = 0;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[window.GetLanguage()];

                if (offset == 0) { continue; }

                int returnValue = offset;

				System.Collections.ArrayList al = new System.Collections.ArrayList(); 

                while (body.StringBlock[offset] != '\0') {al.Add(body.StringBlock[offset++]); }

				byte[] toAdd = new byte[al.Count];
				int n = 0;
				foreach (byte o in al) { toAdd[n++] = o; }

                SpellDispel_DBC_Lookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.offset = returnValue;
				temp.stringHash = Encoding.UTF8.GetString(toAdd).GetHashCode();
                temp.comboBoxIndex = boxIndex;

				main.DispelType.Items.Add(Encoding.UTF8.GetString(toAdd));

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateDispelSelection()
        {
            int ID = int.Parse(adapter.query(string.Format("SELECT `Dispel` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0][0].ToString());

            if (ID == 0 || ID == -1)
            {
                main.Category.threadSafeIndex = body.lookup[1].comboBoxIndex;
                return;
            }

            for (int i = 0; i < header.RecordCount; ++i)
            {
                if (ID == body.records[i].ID)
                {
                    main.DispelType.threadSafeIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct SpellDispel_DBC_Map
        {
            public SpellDispel_DBC_Record[] records;
            public List<SpellDispel_DBC_Lookup> lookup;
            public byte[] StringBlock;
        };

        public struct SpellDispel_DBC_Lookup
        {
            public int ID;
            public int offset;
            public int stringHash;
            public int comboBoxIndex;
        };

        public struct SpellDispel_DBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 NameFlags;
            public UInt32 Combinations;
            public UInt32 ImmunityPossible;
            public UInt32 InternalName;
        };
    };
}
