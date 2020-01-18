using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellVisualKit : AbstractDBC
    {
        public SpellVisualKit()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellVisualKit.dbc");
        }
    }
}
