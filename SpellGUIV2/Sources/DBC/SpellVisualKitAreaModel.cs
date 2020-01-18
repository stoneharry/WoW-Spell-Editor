using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    class SpellVisualKitAreaModel : AbstractDBC
    {
        public SpellVisualKitAreaModel()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellVisualKitAreaModel.dbc");
        }
    }
}
