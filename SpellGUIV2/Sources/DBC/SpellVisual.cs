using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellVisual : AbstractDBC
    {
        public SpellVisual()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellVisual.dbc");
        }
    }
}
