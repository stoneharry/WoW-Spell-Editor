using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    public interface IGraphicUserInterfaceDBC
    {
        /**
         * Call from the UI thread to load UI dependent data
         */
        void LoadGraphicUserInterface();
    }
}
