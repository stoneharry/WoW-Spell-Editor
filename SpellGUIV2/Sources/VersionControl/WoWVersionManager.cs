using SpellEditor.Sources.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpellEditor.Sources.VersionControl
{
    class WoWVersionManager
    {
        public static readonly string DefaultVersionString = "3.3.5a 12340";

        private static readonly WoWVersionManager _Instance = new WoWVersionManager();

        private List<WoWVersion> _VersionList = new List<WoWVersion>();

        private WoWVersionManager()
        {
            _VersionList.Add(new WoWVersion() { Identity = 112, Name = "WoW 1.12.1", Version = "1.12.1", NumLocales = 8 });
            _VersionList.Add(new WoWVersion() { Identity = 335, Name = "WoW 3.3.5a 12340", Version = DefaultVersionString, NumLocales = 16 });
        }
        
        public static WoWVersionManager GetInstance() => _Instance;

        public WoWVersion SelectedVersion() => LookupVersion(Config.Config.WoWVersion) ?? LookupVersion(DefaultVersionString);

        public WoWVersion LookupVersion(string version) => _VersionList.Find(i => i.Version.Equals(version, StringComparison.CurrentCultureIgnoreCase));

        public List<WoWVersion> AllVersions() => _VersionList;
    }
}
