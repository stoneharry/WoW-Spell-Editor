using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class TotemCategory : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        public List<TotemCategoryLookup> Lookups = new List<TotemCategoryLookup>();

        public TotemCategory(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile("DBC/TotemCategory.dbc");

                int boxIndex = 1;
                main.TotemCategory1.Items.Add("None");
                main.TotemCategory2.Items.Add("None");
                TotemCategoryLookup t;
                t.ID = 0;
                t.comboBoxIndex = 0;
                Lookups.Add(t);

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];
                    uint offset = (uint)record["Name" + (window.GetLanguage() + 1)];
                    if (offset == 0)
                        continue;
                    string name = Reader.LookupStringOffset(offset);
                    uint id = (uint) record["ID"];

                    TotemCategoryLookup temp;
                    temp.ID = id;
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    main.TotemCategory1.Items.Add(name);
                    main.TotemCategory2.Items.Add(name);

                    ++boxIndex;
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

        public void UpdateTotemCategoriesSelection()
        {
            var result = adapter.Query(string.Format("SELECT `TotemCategory1`, `TotemCategory2` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID)).Rows[0];
            uint[] IDs = { uint.Parse(result[0].ToString()), uint.Parse(result[1].ToString()) };
            for (int j = 0; j < IDs.Length; ++j)
            {
                uint ID = IDs[j];
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
                    }
                    continue;
                }
                for (int i = 0; i < Lookups.Count; ++i)
                {
                    if (ID == Lookups[i].ID)
                    {
                        if (j == 0)
                        {
                            main.TotemCategory1.threadSafeIndex = Lookups[i].comboBoxIndex;
                            break;
                        }
                        else
                        {
                            main.TotemCategory2.threadSafeIndex = Lookups[i].comboBoxIndex;
                            break;
                        }
                    }
                }
            }
        }

        public struct TotemCategoryLookup
        {
            public uint ID;
            public int comboBoxIndex;
        };
    };
}
