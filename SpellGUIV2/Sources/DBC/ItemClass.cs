using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SpellEditor.Sources.DBC
{
    class ItemClass
    {
        // Begin Window
        private MainWindow main;
        private MySQL.MySQL mySQL;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public ItemClass_DBC_Map body;
        // End DBCs

        public ItemClass(MainWindow window, MySQL.MySQL mySQLConn)
        {
            main = window;
            mySQL = mySQLConn;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].SecondaryID = new UInt32();
                body.records[i].IsWeapon = new UInt32();
                body.records[i].Name = new UInt32[16];
                body.records[i].Flags = new UInt32();
            }

            if (!File.Exists("DBC/ItemClass.dbc"))
            {
                main.HandleErrorMessage("ItemClass.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/ItemClass.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new ItemClass_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(ItemClass_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (ItemClass_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(ItemClass_DBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

            reader.Close();
            fileStream.Close();

            body.lookup = new List<ItemClassLookup>();

            int boxIndex = 1;

            main.EquippedItemClass.Items.Add("None");

            ItemClassLookup t;

            t.ID = -1;
            t.comboBoxIndex = -1;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[0];

                if (offset == 0) { continue; }

                int returnValue = offset;

                string toAdd = "";

                while (body.StringBlock[offset] != 0) { toAdd += body.StringBlock[offset++]; }

                ItemClassLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.EquippedItemClass.Items.Add(toAdd);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateItemClassSelection()
        {
            int ID = Int32.Parse(mySQL.query(String.Format("SELECT `EquippedItemClass` FROM `{0}` WHERE `ID` = '{1}'", mySQL.Table, main.selectedID)).Rows[0][0].ToString());

            if (ID == -1)
            {
                main.EquippedItemClass.threadSafeIndex = 0;

                return;
            }

            if (ID == 4) {
                main.Dispatcher.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => main.EquippedItemInventoryTypeGrid.IsEnabled = true));
            }
            else
            {
                foreach (ThreadSafeCheckBox box in main.equippedItemInventoryTypeMaskBoxes) { box.threadSafeChecked = false; }

                main.Dispatcher.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => main.EquippedItemInventoryTypeGrid.IsEnabled = false));
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.EquippedItemClass.threadSafeIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct ItemClass_DBC_Map
        {
            public ItemClass_DBC_Record[] records;
            public List<ItemClassLookup> lookup;
            public string StringBlock;
        };

        public struct ItemClassLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct ItemClass_DBC_Record
        {
            public UInt32 ID;
            public UInt32 SecondaryID;
            public UInt32 IsWeapon;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 Flags;
        };
    };
}
