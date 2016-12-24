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
    class SpellMechanic
    {
        // Begin Window
        private MainWindow main;
        private DBAdapter adapter;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public Mechanic_DBC_Map body;
        // End DBCs

        public SpellMechanic(MainWindow window, DBAdapter adapter)
        {
            this.main = window;
            this.adapter = adapter;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Name = new UInt32[16];
                body.records[i].NameFlags = new UInt32();
            }

            if (!File.Exists("DBC/SpellMechanic.dbc"))
            {
                main.HandleErrorMessage("SpellMechanic.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellMechanic.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new Mechanic_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(Mechanic_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (Mechanic_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Mechanic_DBC_Record));
                handle.Free();
            }

            body.StringBlock =reader.ReadBytes(header.StringBlockSize);

            reader.Close();
            fileStream.Close();

            body.lookup = new List<MechanicLookup>();

            int boxIndex = 1;

            main.MechanicType.Items.Add("None");

            MechanicLookup t;

            t.ID = 0;
            t.offset = 0;
            t.stringHash = "None".GetHashCode();
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
				int offset = (int)body.records[i].Name[window.GetLanguage()];

                if (offset == 0) { continue; }

                int returnValue = offset;

				System.Collections.ArrayList al = new System.Collections.ArrayList(); 

                while (body.StringBlock[offset] != '\0') { al.Add(body.StringBlock[offset++]); }

				byte[] toAdd = new byte[al.Count];
				int n = 0;
				foreach (byte o in al) { toAdd[n++] = o; }

                MechanicLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.offset = returnValue;
                temp.stringHash = toAdd.GetHashCode();
                temp.comboBoxIndex = boxIndex;

				main.MechanicType.Items.Add(Encoding.UTF8.GetString(toAdd));

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateMechanicSelection()
        {
            int ID = Int32.Parse(adapter.query(string.Format("SELECT `Mechanic` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0][0].ToString());

            if (ID == 0)
            {
                main.MechanicType.threadSafeIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.MechanicType.threadSafeIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct Mechanic_DBC_Map
        {
            public Mechanic_DBC_Record[] records;
            public List<MechanicLookup> lookup;
            public byte[] StringBlock;
        };

        public struct MechanicLookup
        {
            public int ID;
            public int offset;
            public int stringHash;
            public int comboBoxIndex;
        };

        public struct Mechanic_DBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 NameFlags;
        };
    };
}
