using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpellEditor.Sources.DBC
{
    class AnimationData : AbstractDBC, IBoxContentProvider
    {
        public readonly List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public AnimationData()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\AnimationData.dbc");

            BuildLookups();
        }

        private void BuildLookups()
        {
            var index = 0;
            foreach (var record in Body.RecordMaps)
            {
                var id = uint.Parse(record["ID"].ToString());
                var nameOffset = uint.Parse(record["Name"].ToString());
                var name = nameOffset > 0 ? Reader.LookupStringOffset(nameOffset) : "";
                var precedingAnimId = uint.Parse(record["FallbackAnimationID"].ToString());
                if (precedingAnimId > 0)
                {
                    var otherName = GetNameForRecordId(precedingAnimId);
                    name += "\n Preceding: " + otherName;
                }
                var container = new DBCBoxContainer(id, id + " - " + name, index++);
                Lookups.Add(container);
            }
        }

        private string GetNameForRecordId(uint precedingAnimId)
        {
            var record = Body.RecordMaps.Single((entry) =>
            {
                var ID = uint.Parse(entry["ID"].ToString());
                if (ID == precedingAnimId)
                {
                    return true;
                }
                return false;
            });
            var nameOffset = uint.Parse(record["Name"].ToString());
            return nameOffset > 0 ? Reader.LookupStringOffset(nameOffset) : string.Empty;
        }

        public List<DBCBoxContainer> GetAllBoxes()
        {
            return Lookups;
        }
    }
}
