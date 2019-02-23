using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class SpellRadius : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        public List<RadiusLookup> Lookups = new List<RadiusLookup>();

        public SpellRadius(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile("DBC/SpellRadius.dbc");

                int boxIndex = 1;
                main.RadiusIndex1.Items.Add("0 - 0");
                main.RadiusIndex2.Items.Add("0 - 0");
                main.RadiusIndex3.Items.Add("0 - 0");
                RadiusLookup t;
                t.ID = 0;
                t.comboBoxIndex = 0;
                Lookups.Add(t);

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];

                    float radius = (float) record["Radius"];
                    float maximumRadius = (float) record["MaximumRadius"];

                    RadiusLookup temp;
                    temp.ID = (uint) record["ID"];
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    // Some attempt to pad the label better
                    string label = $"{ String.Format("{0,-23}", $"{ radius } - { maximumRadius}") }\t(Radius - MaxRadius)";
                    main.RadiusIndex1.Items.Add(label);
                    main.RadiusIndex2.Items.Add(label);
                    main.RadiusIndex3.Items.Add(label);

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

        public void UpdateRadiusIndexes()
        {
            var result = adapter.Query(string.Format("SELECT `EffectRadiusIndex1`, `EffectRadiusIndex2`, `EffectRadiusIndex3` FROM `{0}` WHERE `ID` = '{1}'", 
                "spell", main.selectedID)).Rows[0];
            uint[] IDs = { uint.Parse(result[0].ToString()), uint.Parse(result[1].ToString()), uint.Parse(result[2].ToString()) };
            for (int j = 0; j < IDs.Length; ++j)
            {
                uint ID = IDs[j];

                if (ID == 0)
                {
                    switch (j)
                    {
                        case 0:
                        {
                            main.RadiusIndex1.threadSafeIndex = 0;
                            break;
                        }
                        case 1:
                        {
                            main.RadiusIndex2.threadSafeIndex = 0;
                            break;
                        }
                        case 2:
                        {
                            main.RadiusIndex3.threadSafeIndex = 0;
                            break;
                        }
                    }
                    continue;
                }
                for (int i = 0; i < Lookups.Count; ++i)
                {
                    if (ID == Lookups[i].ID)
                    {
                        switch (j)
                        {
                            case 0:
                            {
                                main.RadiusIndex1.threadSafeIndex = Lookups[i].comboBoxIndex;
                                break;
                            }
                            case 1:
                            {
                                main.RadiusIndex2.threadSafeIndex = Lookups[i].comboBoxIndex;
                                break;
                            }
                            case 2:
                            {
                                main.RadiusIndex3.threadSafeIndex = Lookups[i].comboBoxIndex;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public struct RadiusLookup
        {
            public uint ID;
            public int comboBoxIndex;
        };
    };
}
