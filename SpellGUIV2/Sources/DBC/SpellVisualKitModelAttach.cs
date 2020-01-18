using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellVisualKitModelAttach : AbstractDBC
    {
        public SpellVisualKitModelAttach()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellVisualKitModelAttach.dbc");
        }
    }
}
