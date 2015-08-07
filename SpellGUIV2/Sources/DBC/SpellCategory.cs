using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{

    class SpellCategory
    {
        // Begin Window
        private MainWindow main;
        private MySQL.MySQL mySQL;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellCategory_DBC_Map body;
        // End DBCs

        public SpellCategory(MainWindow window, MySQL.MySQL mySQLConn)
        {
            main = window;
            mySQL = mySQLConn;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Flags = new UInt32();
            }

            if (!File.Exists("DBC/SpellCategory.dbc"))
            {
                main.HandleErrorMessage("SpellCategory.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellCategory.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellCategory_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellCategory_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellCategory_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellCategory_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellCategoryLookup>();

            int boxIndex = 1;

            main.Category.Items.Add(0);

            SpellCategoryLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int id = (int)body.records[i].ID;

                SpellCategoryLookup temp;

                temp.ID = id;
                temp.comboBoxIndex = boxIndex;

                main.Category.Items.Add(id);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateCategorySelection()
        {
            int ID = Int32.Parse(mySQL.query(String.Format("SELECT `Category` FROM `{0}` WHERE `ID` = '{1}'", mySQL.Table, main.selectedID)).Rows[0][0].ToString());

            if (ID == 0)
            {
                main.Category.threadSafeIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.Category.threadSafeIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }
    }

    public struct SpellCategory_DBC_Map
    {
        public SpellCategory_DBC_Record[] records;
        public List<SpellCategoryLookup> lookup;
    };

    public struct SpellCategoryLookup
    {
        public int ID;
        public int comboBoxIndex;
    };

    public struct SpellCategory_DBC_Record
    {
        public UInt32 ID;
        public UInt32 Flags;
    };
}
