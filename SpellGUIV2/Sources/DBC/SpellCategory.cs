using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{

    class SpellCategory : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        public List<SpellCategoryLookup> Lookups = new List<SpellCategoryLookup>();

        public SpellCategory(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile("DBC/SpellCategory.dbc");

                int boxIndex = 1;
                main.Category.Items.Add(0);
                SpellCategoryLookup t;
                t.ID = 0;
                t.comboBoxIndex = 0;
                Lookups.Add(t);

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];
                    uint id = (uint) record["ID"];

                    SpellCategoryLookup temp;
                    temp.ID = id;
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);
                    main.Category.Items.Add(id);

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

        public void UpdateCategorySelection()
        {
            uint ID = uint.Parse(adapter.Query(string.Format("SELECT `Category` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID)).Rows[0][0].ToString());

            if (ID == 0)
            {
                main.Category.threadSafeIndex = 0;

                return;
            }

            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.Category.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }
    }

    public struct SpellCategoryLookup
    {
        public uint ID;
        public int comboBoxIndex;
    }
}
