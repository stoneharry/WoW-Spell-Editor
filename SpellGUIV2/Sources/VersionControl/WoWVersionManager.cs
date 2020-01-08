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
            _VersionList.Add(new WoWVersion() { Identity = 243, Name = "WoW 2.4.3", Version = "2.4.3", NumLocales = 15 });
            // 3.3.5a actually has 15 locales and 1 flag but we load it like this for legacy backwards compatibility
            _VersionList.Add(new WoWVersion() { Identity = 335, Name = "WoW 3.3.5a 12340", Version = DefaultVersionString, NumLocales = 9 });
        }
        
        public static WoWVersionManager GetInstance() => _Instance;

        public WoWVersion SelectedVersion() => LookupVersion(Config.Config.WoWVersion) ?? LookupVersion(DefaultVersionString);

        public WoWVersion LookupVersion(string version) => _VersionList.Find(i => i.Version.Equals(version, StringComparison.CurrentCultureIgnoreCase));

        public List<WoWVersion> AllVersions() => _VersionList;

        public static bool IsWotlkOrGreaterSelected
        {
            get
            {
                return GetInstance().SelectedVersion().Identity >= 335;
            }
        }

        public static bool IsTbcOrGreaterSelected
        {
            get
            {
                return GetInstance().SelectedVersion().Identity >= 243;
            }
        }
    }
}
