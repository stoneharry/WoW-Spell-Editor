using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellRuneCost
    {
        // Begin Window
        private MainWindow main;
        private MySQL.MySQL mySQL;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellRuneCost_DBC_Map body;
        // End DBCs

        public SpellRuneCost(MainWindow window, MySQL.MySQL mySQLConn)
        {
            main = window;
            mySQL = mySQLConn;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].RuneCost = new UInt32[3];
                body.records[i].RunePowerGain = new UInt32();
            }

            if (!File.Exists("DBC/SpellRuneCost.dbc"))
            {
                main.HandleErrorMessage("SpellRuneCost.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellRuneCost.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellRuneCost_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellRuneCost_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellRuneCost_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellRuneCost_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellRuneCostLookup>();

            int boxIndex = 1;

            main.RuneCost.Items.Add(0);

            SpellRuneCostLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int id = (int)body.records[i].ID;

                SpellRuneCostLookup temp;

                temp.ID = id;
                temp.comboBoxIndex = boxIndex;

                main.RuneCost.Items.Add(id);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateSpellRuneCostSelection()
        {
            int ID = Int32.Parse(mySQL.query(String.Format("SELECT `RuneCostID` FROM `{0}` WHERE `ID` = '{1}'", mySQL.Table, main.selectedID)).Rows[0][0].ToString());

            if (ID == 0)
            {
                main.RuneCost.threadSafeIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.RuneCost.threadSafeIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct SpellRuneCost_DBC_Map
        {
            public SpellRuneCost_DBC_Record[] records;
            public List<SpellRuneCostLookup> lookup;
        };

        public struct SpellRuneCostLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct SpellRuneCost_DBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public UInt32[] RuneCost;
            public UInt32 RunePowerGain;
        };
    };
}
