using SpellEditor.Sources.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class AreaGroup : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;
        
        public List<AreaGroupLookup> Lookups;

        public AreaGroup(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile("DBC/AreaGroup.dbc");

                Lookups = new List<AreaGroupLookup>();
                int boxIndex = 1;

                main.AreaGroup.Items.Add(0);

                AreaGroupLookup t;
                t.ID = 0;
                t.comboBoxIndex = 0;

                Lookups.Add(t);

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];
                    uint id = (uint)record["ID"];

                    AreaGroupLookup temp;

                    ArrayList al = new ArrayList();

                    var recordPointer = record;
                    do
                    {
                        for (int areaIdCol = 1; areaIdCol <= 6; ++areaIdCol)
                        {
                            uint val = (uint)recordPointer["AreaId" + areaIdCol];
                            if (val != 0)
                                al.Add(val);
                        }
                        recordPointer = FindAreaGroup((uint)recordPointer["NextGroup"]);
                    } while ((uint)recordPointer["NextGroup"] != 0);

                    temp.ID = (int)id;
                    temp.comboBoxIndex = boxIndex;
                    Label areaGroupLab = new Label();

                    string areaList_str = "";
                    foreach (uint val in al)
                    {
                        areaList_str += "AreaId:";
                        areaList_str += val;
                        areaList_str += "\t\t";
                        areaList_str += window.GetAreaTableName(val);
                        areaList_str += "\n";
                    }

                    areaGroupLab.ToolTip = areaList_str;

                    string contentString = record["ID"].ToString() + ": ";
                    foreach (uint val in al)
                        contentString += window.GetAreaTableName(val) + ", ";
                    if (al.Count > 0)
                        contentString = contentString.Substring(0, contentString.Length - 2);
                    areaGroupLab.Content = contentString.Length > 120 ? (contentString.Substring(0, 110) + "...") : contentString;

                    main.AreaGroup.Items.Add(areaGroupLab);

                    Lookups.Add(temp);

                    boxIndex++;
                    Reader.CleanStringsMap();
                }
                Reader.CleanStringsMap();
            }
            catch (Exception ex)
            {
                main.HandleErrorMessage(ex.Message);
                return;
            }
            
        }

        public Dictionary<string, object> FindAreaGroup(uint fId)
        {
            foreach (var record in Body.RecordMaps) 
            {
                if ((uint) record["ID"] == fId)
                    return record;
            }
            var returnVal = new Dictionary<string, object>();
            returnVal.Add("NextGroup", (uint) 0);
            return returnVal;
        }

        public void UpdateAreaGroupSelection()
        {
            uint ID = uint.Parse(adapter.Query(string.Format("SELECT `AreaGroupID` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID)).Rows[0][0].ToString());

            if (ID == 0)
            {
                main.AreaGroup.threadSafeIndex = 0;
                return;
            }

            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.AreaGroup.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }
    }

    public struct AreaGroupLookup
    {
        public int ID;
        public int comboBoxIndex;
    };
}
