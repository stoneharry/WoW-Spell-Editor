using SpellEditor.Sources.Controls;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class AreaGroup : AbstractDBC, IBoxContentProvider
    {
        
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public AreaGroup(Dictionary<uint, AreaTable.AreaTableLookup> areaTableLookups)
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\AreaGroup.dbc");

            Lookups.Add(new DBCBoxContainer(0, "0", 0));

            int boxIndex = 1;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                uint id = (uint)record["ID"];

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

                Label areaGroupLab = new Label();
                string areaList_str = "";
                foreach (uint val in al)
                {
                    string areaName = GetAreaTableName(val, areaTableLookups);
                    areaList_str += $"AreaId: { val }\t\t{ areaName }\n";
                }
                areaGroupLab.ToolTip = areaList_str;

                string contentString = record["ID"].ToString() + ": ";
                foreach (uint val in al)
                    contentString += GetAreaTableName(val, areaTableLookups) + ", ";
                if (al.Count > 0)
                    contentString = contentString.Substring(0, contentString.Length - 2);
                areaGroupLab.Content = contentString.Length > 120 ? (contentString.Substring(0, 110) + "...") : contentString;

                Lookups.Add(new DBCBoxContainer(id, areaGroupLab, boxIndex));

                boxIndex++;
                Reader.CleanStringsMap();
            }
            Reader.CleanStringsMap();
        }

        public List<DBCBoxContainer> GetAllBoxes()
        {
            return Lookups;
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
        
        public string GetAreaTableName(uint id, Dictionary<uint, AreaTable.AreaTableLookup> areaTableLookups)
        {
            var result = Lookups.Find(entry => entry.ID == id);
            return areaTableLookups.ContainsKey(id) ? areaTableLookups[id].AreaName : "";
        }

        public int UpdateAreaGroupSelection(uint ID)
        {
            if (ID == 0)
            {
                return 0;
            }

            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    return Lookups[i].ComboBoxIndex;
                }
            }
            return 0;
        }
    }
}
