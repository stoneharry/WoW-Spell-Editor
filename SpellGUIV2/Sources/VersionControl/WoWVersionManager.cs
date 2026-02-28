using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.VersionControl
{
    class WoWVersionManager
    {
        public static readonly string DefaultVersionString = "3.3.5a 12340";

        private static readonly WoWVersionManager _Instance = new WoWVersionManager();

        private readonly List<WoWVersion> _VersionList = new List<WoWVersion>();

        private readonly Dictionary<WoWVersion, KeyResource> _KeyResourceLookup = new Dictionary<WoWVersion, KeyResource>();

        private WoWVersionManager()
        {
            var vanilla = new WoWVersion() { Identity = 112, Name = "WoW 1.12.1", Version = "1.12.1", NumLocales = 8 };
            var tbc = new WoWVersion() { Identity = 243, Name = "WoW 2.4.3", Version = "2.4.3", NumLocales = 15 };
            // 3.3.5a actually has 15 locales and 1 flag but we load it like this for legacy backwards compatibility
            var wotlk = new WoWVersion() { Identity = 335, Name = "WoW 3.3.5a 12340", Version = DefaultVersionString, NumLocales = 9 };

            var vanillaLookups = GenerateKeyResource(vanilla);
            var tbcLookups = GenerateKeyResource(tbc);
            var wotlkLookups = GenerateKeyResource(wotlk);

            _VersionList.Add(vanilla);
            _VersionList.Add(tbc);
            _VersionList.Add(wotlk);

            _KeyResourceLookup.Add(vanilla, vanillaLookups);
            _KeyResourceLookup.Add(tbc, tbcLookups);
            _KeyResourceLookup.Add(wotlk, wotlkLookups);
        }

        public static WoWVersionManager GetInstance() => _Instance;

        public WoWVersion SelectedVersion() => LookupVersion(Config.Config.WoWVersion) ?? LookupVersion(DefaultVersionString);

        public WoWVersion LookupVersion(string version) => _VersionList.Find(i => i.Version.Equals(version, StringComparison.CurrentCultureIgnoreCase));

        public KeyResource LookupKeyResource() => LookupKeyResource(SelectedVersion());
        public KeyResource LookupKeyResource(WoWVersion version) => _KeyResourceLookup[version];

        public List<WoWVersion> AllVersions() => _VersionList;

        public static bool IsWotlkOrGreaterSelected => GetInstance().SelectedVersion().Identity >= 335;

        public static bool IsTbcOrGreaterSelected => GetInstance().SelectedVersion().Identity >= 243;

        #region GenerateKeyResource strings
        private KeyResource GenerateKeyResource(WoWVersion version)
        {
            switch (version.Identity)
            {
                case 112:
                    return new KeyResource(new string[]
                        {
                            "PrecastKit",
                            "CastKit",
                            "ImpactKit",
                            "StateKit",
                            "StateDoneKit",
                            "ChannelKit",
                        },
                        new string[]
                        {
                            "HeadEffect",
                            "ChestEffect",
                            "BaseEffect",
                            "LeftHandEffect",
                            "RightHandEffect",
                            "BreathEffect",
                            "LeftWeaponEffect",
                            "RightWeaponEffect"
                        });
                case 243:
                    return new KeyResource(new string[]
                         {
                            "PrecastKit",
                            "CastKit",
                            "ImpactKit",
                            "StateKit",
                            "StateDoneKit",
                            "ChannelKit",
                        },
                        new string[]
                        {
                            "HeadEffect",
                            "ChestEffect",
                            "BaseEffect",
                            "LeftHandEffect",
                            "RightHandEffect",
                            "BreathEffect",
                            "LeftWeaponEffect",
                            "RightWeaponEffect"
                        });
                case 335:
                    return new KeyResource(new string[]
                        {
                            "PrecastKit",
                            "CastKit",
                            "ImpactKit",
                            "StateKit",
                            "StateDoneKit",
                            "ChannelKit",
                            "InstantAreaKit",
                            "ImpactAreaKit",
                            "CasterImpactKit",
                            "TargetImpactKit",
                            "MissileTargetingKit",
                            "PersistentAreaKit"
                        },
                        new string[]
                        {
                            "HeadEffect",
                            "ChestEffect",
                            "BaseEffect",
                            "LeftHandEffect",
                            "RightHandEffect",
                            "BreathEffect",
                            "LeftWeaponEffect",
                            "RightWeaponEffect"
                        });
                default:
                    throw new Exception($"Unknown version identity [{version.Identity}] generating WoWVersionManager.KeyResource");
            }
        }
        #endregion

        public class KeyResource
        {
            public readonly string[] KitColumnKeys;
            public readonly string[] EffectColumnKeys;

            public KeyResource(string[] kitColumnKeys, string[] effectColumnKeys)
            {
                KitColumnKeys = kitColumnKeys;
                EffectColumnKeys = effectColumnKeys;
            }
        }
    }
}
