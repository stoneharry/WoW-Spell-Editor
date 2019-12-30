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
            LoadRequiredDbcs();
        }

        /**
         * Loads all the hardcoded DBC's that this program requires for operation.
         * 
         * There are some exemptions to this where dependencies were not easy to remove.
         * These are loaded by the ForceLoadDbc function.
         */
        private void LoadRequiredDbcs()
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
            var isWotlkOrGreater = WoWVersionManager.GetInstance().SelectedVersion().Identity >= 335;
            if (isWotlkOrGreater)
            {
                TryLoadDbc<TotemCategory>("TotemCategory");
                TryLoadDbc<SpellRuneCost>("SpellRuneCost");
                TryLoadDbc<SpellDescriptionVariables>("SpellDescriptionVariables");
            }
        }

        /**
         * Loads the DBC file supressing any exception raised in order to not throw a hard error
         * because of a bad binding or dbc file.
         */
        private bool TryLoadDbc<DBCType>(string name) where DBCType : AbstractDBC, new()
        {
            try
            {
                return _DbcMap.TryAdd(name, new DBCType());
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Failed to load DBC: [" + name + "] " + e.Message + "\n" + e.StackTrace);
            }
            return false;
        }

        public bool ForceLoadDbc(string name, AbstractDBC dbc) => _DbcMap.TryAdd(name, dbc);

        public AbstractDBC FindDbcForBinding(string bindingName, bool tryLoad = false)
        {
            if (_DbcMap.TryGetValue(bindingName, out var dbc))
            {
                return dbc;
            }
            if (tryLoad)
            {
                var newDbc = new GenericDbc("DBC/" + bindingName + ".dbc");
                _DbcMap.TryAdd(bindingName, newDbc);
                return newDbc;
            }
            return null;
        }

        public AbstractDBC ClearDbcBinding(string bindingName) => _DbcMap.TryRemove(bindingName, out var removed) ? removed : null;

        public static DBCManager GetInstance() => _Instance;
    }
}
