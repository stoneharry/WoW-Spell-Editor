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
            ForceLoadDbc<AreaTable>("AreaTable");
            ForceLoadDbc<SpellCategory>("SpellCategory");
            ForceLoadDbc<SpellDispelType>("SpellDispelType");
            ForceLoadDbc<SpellMechanic>("SpellMechanic");
            ForceLoadDbc<SpellFocusObject>("SpellFocusObject");
            ForceLoadDbc<SpellCastTimes>("SpellCastTimes");
            ForceLoadDbc<SpellDuration>("SpellDuration");
            ForceLoadDbc<SpellRange>("SpellRange");
            ForceLoadDbc<SpellRadius>("SpellRadius");
            ForceLoadDbc<ItemClass>("ItemClass");
            ForceLoadDbc<ItemSubClass>("ItemSubClass");
            ForceLoadDbc<AnimationData>("AnimationData");
            if (WoWVersionManager.IsTbcOrGreaterSelected)
            {
                ForceLoadDbc<TotemCategory>("TotemCategory");
            }
            if (WoWVersionManager.IsWotlkOrGreaterSelected)
            {
                ForceLoadDbc<SpellRuneCost>("SpellRuneCost");
                ForceLoadDbc<SpellDescriptionVariables>("SpellDescriptionVariables");
            }
        }
        
        private bool ForceLoadDbc<DBCType>(string name) where DBCType : AbstractDBC, new()
        {
            if (_DbcMap.ContainsKey(name))
            {
                _DbcMap.TryRemove(name, out var oldDbc);
            }
            return _DbcMap.TryAdd(name, new DBCType());
        }

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

        public MutableGenericDbc ReadLocalDbcForBinding(string bindingName) => new MutableGenericDbc($"{Config.Config.DbcDirectory}\\{bindingName}.dbc");

        public AbstractDBC ClearDbcBinding(string bindingName) => _DbcMap.TryRemove(bindingName, out var removed) ? removed : null;

        public static DBCManager GetInstance() => _Instance;
    }
}
