using NLog;
using SpellEditor.Sources.VersionControl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace SpellEditor.Sources.DBC
{
    class DBCManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly DBCManager _Instance = new DBCManager();

        private ConcurrentDictionary<string, AbstractDBC> _DbcMap = new ConcurrentDictionary<string, AbstractDBC>();
        private readonly HashSet<string> _GuiLoaded = new HashSet<string>();
        private readonly object _LoadLock = new object();

        // Nothing on the main window is built from these, they are only needed once a spell
        // effect that uses them is opened. Loaded on first use unless CacheAllOnLoad is set.
        private static readonly string[] DeferredDbcs =
        {
            "AnimationData",
            "ItemSubClass"
        };

        // Same idea, but these only have bindings and misc value fields on wotlk or greater
        private static readonly string[] DeferredWotlkDbcs =
        {
            "SkillLine",
            "Languages",
            "LockType",
            "SkillLineCategory",
            "ScreenEffect"
        };

        private DBCManager()
        {
        }

        /**
         * Loads the DBC's needed to put the main window on screen.
         *
         * There are some exemptions to this where dependencies were not easy to remove.
         * These are loaded by the ForceLoadDbc function.
         *
         * Anything only referenced later on, when clicking into a spell, is left to
         * FindDbcForBinding to load on demand.
         */
        public List<Task<bool>> LoadRequiredDbcs()
        {
            var tasks = new List<Task<bool>>
            {
                ForceLoadDbc<AreaTable>("AreaTable"),
                ForceLoadDbc<SpellCategory>("SpellCategory"),
                ForceLoadDbc<SpellDispelType>("SpellDispelType"),
                ForceLoadDbc<SpellMechanic>("SpellMechanic"),
                ForceLoadDbc<SpellFocusObject>("SpellFocusObject"),
                ForceLoadDbc<SpellCastTimes>("SpellCastTimes"),
                ForceLoadDbc<SpellDuration>("SpellDuration"),
                ForceLoadDbc<SpellRange>("SpellRange"),
                ForceLoadDbc<SpellRadius>("SpellRadius"),
                ForceLoadDbc<ItemClass>("ItemClass"),
                ForceLoadDbc<CreatureType>("CreatureType"),
                ForceLoadDbc<SpellShapeshiftForm>("SpellShapeshiftForm")

            };

            if (WoWVersionManager.IsTbcOrGreaterSelected)
            {
                tasks.Add(ForceLoadDbc<TotemCategory>("TotemCategory"));
            }
            if (WoWVersionManager.IsWotlkOrGreaterSelected)
            {
                tasks.Add(ForceLoadDbc<SpellRuneCost>("SpellRuneCost"));
                tasks.Add(ForceLoadDbc<SpellDescriptionVariables>("SpellDescriptionVariables"));

                // tasks.Add(ForceLoadDbc<...>("OverrideSpellData"));
            }

            if (Config.Config.CacheAllOnLoad)
            {
                foreach (var name in DeferredDbcs)
                {
                    var deferred = name;
                    tasks.Add(Task.Run(() => LoadDbcOnDemand(deferred) != null));
                }
                if (WoWVersionManager.IsWotlkOrGreaterSelected)
                {
                    foreach (var name in DeferredWotlkDbcs)
                    {
                        var deferred = name;
                        tasks.Add(Task.Run(() => LoadDbcOnDemand(deferred) != null));
                    }
                }
            }
            return tasks;
        }

        private Task<bool> ForceLoadDbc<DBCType>(string name) where DBCType : AbstractDBC, new()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (_DbcMap.ContainsKey(name))
                    {
                        ClearDbcBinding(name);
                    }
                    return _DbcMap.TryAdd(name, new DBCType());
                }
                catch (Exception exception)
                {
                    Logger.Error(exception, $"Failed to load: [{name}.dbc], the program will likely break because of this error.");
                    throw;
                }
            });
        }

        public bool InjectLoadedDbc(string name, AbstractDBC dbc) => _DbcMap.TryAdd(name, dbc);

        /**
         * Reads a DBC we have a dedicated class for the first time something asks for it.
         *
         * Only works for classes that can be built without arguments, anything with a
         * dependency (AreaGroup, SpellDifficulty, SpellIcon) still has to be injected.
         */
        public AbstractDBC LoadDbcOnDemand(string bindingName)
        {
            var type = Type.GetType($"SpellEditor.Sources.DBC.{bindingName}");
            if (type == null || !typeof(AbstractDBC).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
                return null;

            lock (_LoadLock)
            {
                if (_DbcMap.TryGetValue(bindingName, out var existing))
                    return existing;
                try
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var dbc = (AbstractDBC)Activator.CreateInstance(type);
                    LoadGuiOnce(bindingName, dbc);
                    _DbcMap[bindingName] = dbc;
                    Logger.Info($"Loaded [{bindingName}.dbc] on first use in {watch.ElapsedMilliseconds}ms");
                    return dbc;
                }
                catch (Exception exception)
                {
                    Logger.Error(exception, $"Failed to load: [{bindingName}.dbc] on demand.");
                    return null;
                }
            }
        }

        public AbstractDBC FindDbcForBinding(string bindingName, bool tryLoad = false)
        {
            if (_DbcMap.TryGetValue(bindingName, out var dbc))
            {
                return dbc;
            }
            var loaded = LoadDbcOnDemand(bindingName);
            if (loaded != null)
            {
                return loaded;
            }
            if (tryLoad)
            {
                var newDbc = new GenericDbc(Config.Config.DbcDirectory + "\\" + bindingName + ".dbc");
                _DbcMap.TryAdd(bindingName, newDbc);
                return newDbc;
            }
            return null;
        }

        // Lookup that never reads anything from disk, for callers that only want what is
        // already in memory
        public AbstractDBC FindLoadedDbc(string bindingName) =>
            _DbcMap.TryGetValue(bindingName, out var dbc) ? dbc : null;

        public MutableGenericDbc ReadLocalDbcForBinding(string bindingName) => new MutableGenericDbc($"{Config.Config.DbcDirectory}\\{bindingName}.dbc");

        public AbstractDBC ClearDbcBinding(string bindingName)
        {
            lock (_GuiLoaded)
            {
                _GuiLoaded.Remove(bindingName);
            }
            return _DbcMap.TryRemove(bindingName, out var removed) ? removed : null;
        }

        internal void LoadGraphicUserInterface()
        {
            foreach (var pair in _DbcMap)
            {
                LoadGuiOnce(pair.Key, pair.Value);
            }
        }

        // A DBC read on demand builds its UI data straight away, so it must not be built a
        // second time when the startup pass runs, the lookups would end up duplicated
        private void LoadGuiOnce(string name, AbstractDBC dbc)
        {
            lock (_GuiLoaded)
            {
                if (!_GuiLoaded.Add(name))
                    return;
            }
            try
            {
                dbc.LoadGraphicUserInterface();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to load UI for " + dbc);
            }
        }

        public static DBCManager GetInstance() => _Instance;
    }
}
