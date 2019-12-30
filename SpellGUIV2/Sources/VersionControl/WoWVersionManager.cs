using SpellEditor.Sources.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.VersionControl
{
    class WoWVersionManager
    {
        private static readonly WoWVersionManager _Instance = new WoWVersionManager();

        // Temporaily hardcoded
        private WoWVersion _CurrentVersion = new WoWVersion() { Identity = 112, Name = "WoW 1.12.1", Version = "1.12.1", NumLocales = 8 };
        // new WoWVersion() { Identity = 335, Name = "WoW 3.3.5a, Version = "3.3.5a 12340", NumLocales = 16 };

        private WoWVersionManager()
        {
        }
        
        public static WoWVersionManager GetInstance() => _Instance;

        public WoWVersion SelectedVersion() => _CurrentVersion; 
    }
}
