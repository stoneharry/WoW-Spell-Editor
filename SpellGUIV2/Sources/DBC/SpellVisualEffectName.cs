using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellVisualEffectName : AbstractDBC
    {
        public SpellVisualEffectName()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellVisualEffectName.dbc");
        }

        public string LookupStringOffset(uint offset) => Reader.LookupStringOffset(offset);
    }
}
