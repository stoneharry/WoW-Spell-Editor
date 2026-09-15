using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Emulator
{
    /// <summary>
    /// Puts the world tables of the selected emulator into the main tab strip and owns the
    /// connection. One instance per emulator, MainWindow throws it away when the setting changes.
    /// </summary>
    public class WorldIntegration
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly MainWindow _mainWindow;
        private readonly TabControl _host;
        private readonly List<TabItem> _tabs = new List<TabItem>();
        private readonly List<WorldTableEditor> _editors = new List<WorldTableEditor>();

        private readonly object _connectLock = new object();

        private WorldDatabase _database;
        private List<string> _missingTables = new List<string>();
        private uint _spellId;
        private bool _spellSelected;
        private bool _connecting;
        private bool _probedTables;
        private string _lastConnectError;

        public Emulator Emulator { get; }

        public WorldIntegration(MainWindow mainWindow, TabControl host, Emulator emulator)
        {
            _mainWindow = mainWindow;
            _host = host;
            Emulator = emulator;
        }

        public bool Owns(object tab) => tab is TabItem item && _tabs.Contains(item);

        /// <summary>Drops the tabs off the strip so a fresh integration can take over.</summary>
        public void RemoveTabs()
        {
            // Step off our tabs first, removing the selected one would activate the next of ours
            if (_host.SelectedItem is TabItem selected && _tabs.Contains(selected))
                _host.SelectedIndex = 0;

            foreach (var tab in _tabs)
                _host.Items.Remove(tab);
            _tabs.Clear();
            _editors.Clear();
            _database = null;
            _probedTables = false;
        }

        public void UpdateTabs(bool spellSelected)
        {
            _spellSelected = spellSelected;
            UpdateTabs();
        }

        private void UpdateTabs()
        {
            if (!WorldDatabase.IsConfigured || !_spellSelected)
            {
                foreach (var tab in _tabs)
                    tab.Visibility = Visibility.Collapsed;
                if (_host.SelectedItem is TabItem selected && _tabs.Contains(selected))
                    _host.SelectedIndex = 0;
                return;
            }

            if (_tabs.Count == 0)
                BuildTabs();

            foreach (var tab in _tabs)
                tab.Visibility = IsAvailable(tab) ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool IsAvailable(TabItem tab) =>
            !_missingTables.Contains(((WorldTable)tab.Tag).Name, StringComparer.OrdinalIgnoreCase);

        private void BuildTabs()
        {
            foreach (var table in Emulator.Tables)
            {
                var editor = new WorldTableEditor(table, _mainWindow);
                editor.SetSpell(_spellId);
                editor.SetDatabase(_database);
                editor.Saved += () => InvalidateOthers(editor);
                _editors.Add(editor);

                var tab = new TabItem
                {
                    Header = table.Header,
                    Content = editor,
                    Tag = table
                };
                _tabs.Add(tab);
                _host.Items.Add(tab);
            }
        }

        public async void Activate(object tab)
        {
            if (!(tab is TabItem item) || !(item.Content is WorldTableEditor editor))
                return;

            if (!WorldDatabase.IsConfigured)
            {
                editor.ShowMessage(Localise("emu_disabled",
                    "The world database integration is turned off. Turn it on and enter your " +
                    "server details in the settings."), false);
                return;
            }

            if (_database == null || !_probedTables)
            {
                await ConnectAsync(editor);
                if (_database == null)
                    return;
            }

            editor.Refresh(_spellId);
        }

        /// <summary>Connects on first use. Null means the world database is off or unreachable.</summary>
        public WorldDatabase EnsureDatabase()
        {
            if (_database != null)
                return _database;
            if (!WorldDatabase.IsConfigured)
                return null;

            lock (_connectLock)
            {
                if (_database != null)
                    return _database;
                try
                {
                    var database = WorldDatabase.FromConfig();
                    database.TestConnection();
                    _database = database;
                    foreach (var current in _editors)
                        current.SetDatabase(database);
                }
                catch (Exception exception)
                {
                    Logger.Error(exception, $"Failed to connect to the {Emulator.Name} world database");
                    _lastConnectError = exception.Message;
                }
                return _database;
            }
        }

        /// <summary>Editing spell_ranks changes which rows the other tabs inherit.</summary>
        private void InvalidateOthers(WorldTableEditor source)
        {
            foreach (var editor in _editors)
            {
                if (editor != source)
                    editor.Invalidate();
            }
        }

        public void SetSpell(uint spellId)
        {
            _spellId = spellId;
            foreach (var editor in _editors)
                editor.SetSpell(spellId);

            // The rest catch up when they are opened
            if (_database != null && _host.SelectedItem is TabItem selected && _tabs.Contains(selected) &&
                selected.Content is WorldTableEditor editor2)
                editor2.Refresh(spellId);
        }

        public void RefreshLocalisation()
        {
            foreach (var tab in _tabs)
                tab.Header = ((WorldTable)tab.Tag).Header;
            foreach (var editor in _editors)
                editor.RefreshLocalisation();
        }

        private async Task ConnectAsync(WorldTableEditor editor)
        {
            if (_connecting)
                return;

            _connecting = true;
            editor.ShowMessage(Localise("emu_connecting", "Connecting..."), false);
            try
            {
                _missingTables = await Task.Run(() =>
                {
                    var database = EnsureDatabase();
                    if (database == null)
                        throw new Exception(_lastConnectError ?? "the world database could not be reached");
                    return database.FindMissingTables(Emulator.Tables.Select(table => table.Name));
                });
                _probedTables = true;

                UpdateTabs();
                if (_missingTables.Count > 0)
                    Logger.Info($"{Emulator.Name} tables not present in the world database: {string.Join(", ", _missingTables)}");
            }
            catch (Exception exception)
            {
                Logger.Error(exception, $"Failed to connect to the {Emulator.Name} world database");
                _database = null;
                foreach (var current in _editors)
                    current.SetDatabase(null);
                editor.ShowMessage(string.Format(Localise("emu_connect_failed", "Could not connect: {0}"),
                    exception.Message), true);
            }
            finally
            {
                _connecting = false;
            }
        }

        private static string Localise(string key, string fallback)
        {
            var resource = Application.Current?.TryFindResource(key) as string;
            return string.IsNullOrWhiteSpace(resource) ? fallback : resource.Trim();
        }
    }
}
