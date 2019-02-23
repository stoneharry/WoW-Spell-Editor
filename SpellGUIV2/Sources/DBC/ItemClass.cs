using SpellEditor.Sources.Database;
using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace SpellEditor.Sources.DBC
{
    class ItemClass : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        public List<ItemClassLookup> Lookups;

        public ItemClass(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile("DBC/ItemClass.dbc");

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
                    int locale = window.GetLanguage() + 1;
                    uint offset = (uint)record["Name" + locale];
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
            int ID = int.Parse(adapter.Query(string.Format("SELECT `EquippedItemClass` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID)).Rows[0][0].ToString());

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
    };
}
