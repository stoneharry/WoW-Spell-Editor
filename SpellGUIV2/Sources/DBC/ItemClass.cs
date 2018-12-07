using SpellEditor.Sources.Config;
using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace SpellEditor.Sources.DBC
{
    class ItemClass : AbstractDBC
    {
        private MainWindow main;
        private DBAdapter adapter;

        public List<ItemClassLookup> Lookups;

        public ItemClass(MainWindow window, DBAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile<ItemClass_DBC_Record>("DBC/ItemClass.dbc");

                Lookups = new List<ItemClassLookup>();

                int boxIndex = 1;
                window.EquippedItemClass.Items.Add("None");
                ItemClassLookup noneLookup;
                noneLookup.ID = -1;
                noneLookup.comboBoxIndex = 0;
                Lookups.Add(noneLookup);

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];

                    uint offset = ((uint[])record["Name"])[window.GetLanguage()];
                    if (offset == 0)
                        continue;
                    ItemClassLookup temp;
                    temp.ID = int.Parse(record["ID"].ToString());
                    temp.comboBoxIndex = boxIndex;
                    window.EquippedItemClass.Items.Add(Reader.LookupStringOffset(offset));
                    Lookups.Add(temp);
                    boxIndex++;
                }
                Reader.CleanStringsMap();
                // In this DBC we don't actually need to keep the DBC data now that
                // we have extracted the lookup tables. Nulling it out may help with
                // memory consumption.
                Reader = null;
                Body = null;
            }
            catch (Exception ex)
            {
                window.HandleErrorMessage(ex.Message);
                return;
            }
        }

        public void UpdateItemClassSelection()
        {
            int ID = int.Parse(adapter.query(string.Format("SELECT `EquippedItemClass` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0][0].ToString());

            if (ID == -1)
            {
                main.EquippedItemClass.threadSafeIndex = 0;
                //foreach (ThreadSafeCheckBox box in main.equippedItemInventoryTypeMaskBoxes)
                //  box.threadSafeChecked = false;
				main.Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(()
                    => main.EquippedItemInventoryTypeGrid.IsEnabled = false));
                return;
            }

            if (ID == 2 || ID == 4) 
			{
				main.Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(()
                    => main.EquippedItemInventoryTypeGrid.IsEnabled = true));
            }
            else
            {
                foreach (ThreadSafeCheckBox box in main.equippedItemInventoryTypeMaskBoxes)
                    box.threadSafeChecked = false;
				main.Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(()
                    => main.EquippedItemInventoryTypeGrid.IsEnabled = false));
            }

            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.EquippedItemClass.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }

        public struct ItemClassLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct ItemClass_DBC_Record
        {
// These fields are used through reflection, disable warning
#pragma warning disable 0649
#pragma warning disable 0169
            public uint ID;
            public uint SecondaryID;
            public uint IsWeapon;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public uint[] Name;
            public uint Flags;
#pragma warning restore 0649
#pragma warning restore 0169
        };
    };
}
