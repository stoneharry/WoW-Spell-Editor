using SpellEditor.Sources.VersionControl;
using System;
using System.Collections.Concurrent;

namespace SpellEditor.Sources.DBC
{
    class DBCManager
    {
        private static readonly DBCManager _Instance = new DBCManager();

        private ConcurrentDictionary<string, AbstractDBC> _DbcMap = new ConcurrentDictionary<string, AbstractDBC>();

        private DBCManager()
        {
        }

        /**
         * Loads all the hardcoded DBC's that this program requires for operation.
         * 
         * There are some exemptions to this where dependencies were not easy to remove.
         * These are loaded by the ForceLoadDbc function.
         */
        public void LoadRequiredDbcs()
        {
            TryLoadDbc<AreaTable>("AreaTable");
            TryLoadDbc<SpellCategory>("SpellCategory");
            TryLoadDbc<SpellDispelType>("SpellDispelType");
            TryLoadDbc<SpellMechanic>("SpellMechanic");
            TryLoadDbc<SpellFocusObject>("SpellFocusObject");
            TryLoadDbc<SpellCastTimes>("SpellCastTimes");
            TryLoadDbc<SpellDuration>("SpellDuration");
            TryLoadDbc<SpellRange>("SpellRange");
            TryLoadDbc<SpellRadius>("SpellRadius");
            TryLoadDbc<ItemClass>("ItemClass");
            TryLoadDbc<ItemSubClass>("ItemSubClass");
            if (WoWVersionManager.IsTbcOrGreaterSelected)
            {
                TryLoadDbc<TotemCategory>("TotemCategory");
            }
            if (WoWVersionManager.IsWotlkOrGreaterSelected)
            {
                TryLoadDbc<SpellRuneCost>("SpellRuneCost");
                TryLoadDbc<SpellDescriptionVariables>("SpellDescriptionVariables");
            }
        }

        private bool TryLoadDbc<DBCType>(string name) where DBCType : AbstractDBC, new() => _DbcMap.TryAdd(name, new DBCType());

        public bool InjectLoadedDbc(string name, AbstractDBC dbc) => _DbcMap.TryAdd(name, dbc);

        public AbstractDBC FindDbcForBinding(string bindingName, bool tryLoad = false)
        {
            if (_DbcMap.TryGetValue(bindingName, out var dbc))
            {
                return dbc;
            }
            if (tryLoad)
            {
                var newDbc = new GenericDbc(Config.Config.DbcDirectory + "\\" + bindingName + ".dbc");
                _DbcMap.TryAdd(bindingName, newDbc);
                return newDbc;
            }
            return null;
        }

        public AbstractDBC ClearDbcBinding(string bindingName) => _DbcMap.TryRemove(bindingName, out var removed) ? removed : null;

        public static DBCManager GetInstance() => _Instance;
    }
}
