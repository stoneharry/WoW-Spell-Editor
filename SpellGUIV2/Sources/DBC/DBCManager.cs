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
            _DbcMap.TryAdd("AreaTable", new AreaTable());
            _DbcMap.TryAdd("SpellCategory", new SpellCategory());
            _DbcMap.TryAdd("SpellDispelType", new SpellDispelType());
            _DbcMap.TryAdd("SpellMechanic", new SpellMechanic());
            _DbcMap.TryAdd("SpellFocusObject", new SpellFocusObject());
            _DbcMap.TryAdd("SpellCastTimes", new SpellCastTimes());
            _DbcMap.TryAdd("SpellDuration", new SpellDuration());
            _DbcMap.TryAdd("SpellRange", new SpellRange());
            _DbcMap.TryAdd("SpellRadius", new SpellRadius());
            _DbcMap.TryAdd("ItemClass", new ItemClass());
            _DbcMap.TryAdd("ItemSubClass", new ItemSubClass());
            _DbcMap.TryAdd("TotemCategory", new TotemCategory());
            _DbcMap.TryAdd("SpellRuneCost", new SpellRuneCost());
            _DbcMap.TryAdd("SpellDescriptionVariables", new SpellDescriptionVariables());
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
