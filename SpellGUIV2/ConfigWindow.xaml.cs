using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using NLog;
using SpellEditor.Sources.Config;
using SpellEditor.Sources.Emulator;
using SpellEditor.Sources.VersionControl;
using static System.Environment;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Label = System.Windows.Controls.Label;
using TextBox = System.Windows.Controls.TextBox;

namespace SpellEditor
{
    partial class ConfigWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private DatabaseTypeContainer TypeContainer = new DatabaseTypeContainer();
        private Grid MySQLConfigGrid;
        private Grid SQLiteConfigGrid;
        private DatabaseIdentifier defaultConfigType;
        private TextBox _MpqNameText;

        public ConfigWindow(DatabaseIdentifier defaultConfigToShow)
        {
            InitializeComponent();
            defaultConfigType = defaultConfigToShow;
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Info("ERROR: " + e.Exception.Message);
            File.WriteAllText("error.txt", e.Exception.Message, UTF8Encoding.GetEncoding(0));
            e.Handled = true;
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
            ConfigGrid.Children.Clear();
            BuildConfigWindow();
        }

        private void BuildConfigWindow()
        {
            ConfigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ConfigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ConfigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            // WoW Version row
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // Database type row
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // misc value fields config row
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // caching config rows
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // Icon config row
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // MPQ name config row
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // Bindings and directory settings, 2 rows
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // Optional emulator world database row
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // Database type specific grid
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var databaseLabel = new Label { Content = "Database type:" };
            var databaseButton = new SplitButton();
            var versionLabel = new Label { Content = "WoW version: " };
            var versionButton = new SplitButton();

            var sqlite = new DatabaseType { Index = 0, Name = "SQLite", Identity = DatabaseIdentifier.SQLite };
            var mysql = new DatabaseType { Index = 1, Name = "MySQL", Identity = DatabaseIdentifier.MySQL };

            TypeContainer.AddDatabaseType(sqlite);
            TypeContainer.AddDatabaseType(mysql);

            databaseButton.Items.Add(sqlite.Name);
            databaseButton.Items.Add(mysql.Name);
            var index = 0;
            var i = 0;
            foreach (var version in WoWVersionManager.GetInstance().AllVersions())
            {
                versionButton.Items.Add(version.Version);
                if (version.Version.Equals(Config.WoWVersion, StringComparison.CurrentCultureIgnoreCase))
                {
                    index = i;
                }
                ++i;
            }
            versionButton.SelectedIndex = index;
            
            databaseButton.SelectedIndex = TypeContainer.GetDatabaseTypeForId(defaultConfigType).Index;
            databaseButton.MinWidth = 150;
            versionButton.MinWidth = 150;

            databaseButton.SelectionChanged += DatabaseButton_SelectionChanged;
            versionButton.SelectionChanged += VersionButton_SelectionChanged;

            databaseLabel.Margin = new Thickness(10);
            databaseButton.Margin = new Thickness(10);
            versionLabel.Margin = new Thickness(10);
            versionButton.Margin = new Thickness(10);

            int currentRow = 0;

            Grid.SetRow(databaseLabel, currentRow);
            Grid.SetColumn(databaseLabel, 0);
            Grid.SetRow(databaseButton, currentRow++);
            Grid.SetColumn(databaseButton, 1);
            Grid.SetColumnSpan(databaseButton, 2);
            Grid.SetRow(versionLabel, 1);
            Grid.SetColumn(versionLabel, 0);
            Grid.SetRow(versionButton, currentRow++);
            Grid.SetColumn(versionButton, 1);
            Grid.SetColumnSpan(versionButton, 2);

            ConfigGrid.Children.Add(databaseLabel);
            ConfigGrid.Children.Add(databaseButton);
            ConfigGrid.Children.Add(versionLabel);
            ConfigGrid.Children.Add(versionButton);

            currentRow = BuildMiscValueFieldsConfig(ConfigGrid, currentRow);

            currentRow = BuildCachingConfig(ConfigGrid, currentRow);

            currentRow = BuildMpqConfig(ConfigGrid, currentRow);

            currentRow = BuildBindingsAndDbcUI(ConfigGrid, currentRow);

            currentRow = BuildEmulatorConfigUI(ConfigGrid, ++currentRow);

            ++currentRow;
            BuildSQLiteConfigUI(currentRow);
            BuildMySQLConfigUI(currentRow);

            var selectedConfigType = TypeContainer.LookupDatabaseTypeName(databaseButton.SelectedItem.ToString());
            ToggleGridVisibility(selectedConfigType.Identity);
        }

        private void VersionButton_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = e.AddedItems[0].ToString();
            //var selectedType = WoWVersionManager.GetInstance().LookupVersion(selectedItem);

            Config.WoWVersion = selectedItem;
        }

        private void DatabaseButton_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = e.AddedItems[0].ToString();
            var selectedType = TypeContainer.LookupDatabaseTypeName(selectedItem);

            ToggleGridVisibility(selectedType.Identity);
        }

        private void ToggleGridVisibility(DatabaseIdentifier selectedType)
        {
            SQLiteConfigGrid.Visibility = selectedType == DatabaseIdentifier.SQLite ? Visibility.Visible : Visibility.Collapsed;
            MySQLConfigGrid.Visibility = selectedType == DatabaseIdentifier.MySQL ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BuildSQLiteConfigUI(int row)
        {
            var grid = new Grid();

            Grid.SetRow(grid, row);
            Grid.SetColumn(grid, 0);
            Grid.SetColumnSpan(grid, 3);

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            row = 0;

            var dbFileNameLabel = new Label { Content = "SQLite Database File Name: " };
            var dbFileNameText = new TextBox { Text = Config.SQLiteFilename, MinWidth = 200 };
            var confirmBtn = new SQLiteConfirmButton(dbFileNameText)
            {
                Content = "Save Changes",
                Foreground = Brushes.Black
            };
            confirmBtn.Click += SaveSQLiteConfirmBtn_Click;

            dbFileNameLabel.Margin = new Thickness(10, 5, 10, 5);
            dbFileNameText.Margin = new Thickness(10, 5, 10, 5);
            confirmBtn.Margin = new Thickness(10, 10, 10, 5);
            confirmBtn.MinHeight = 40;

            Grid.SetRow(dbFileNameLabel, row);
            Grid.SetColumn(dbFileNameLabel, 0);
            Grid.SetRow(dbFileNameText, row++);
            Grid.SetColumn(dbFileNameText, 1);
            Grid.SetRow(confirmBtn, row++);
            Grid.SetColumn(confirmBtn, 1);

            grid.Children.Add(dbFileNameLabel);
            grid.Children.Add(dbFileNameText);
            grid.Children.Add(confirmBtn);

            ConfigGrid.Children.Add(grid); 
            SQLiteConfigGrid = grid;
        }

        private void BuildMySQLConfigUI(int row)
        {
            var grid = new Grid();

            Grid.SetRow(grid, row);
            Grid.SetColumn(grid, 0);
            Grid.SetColumnSpan(grid, 3);

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            row = 0;

            var hostLabel = new Label { Content = "Hostname: " };
            var hostText = new TextBox { Text = Config.Host, MinWidth = 200 };
            var userLabel = new Label { Content = "Username: " };
            var userText = new TextBox { Text = Config.User, MinWidth = 200 };
            var passLabel = new Label { Content = "Password: " };
            var passText = new TextBox { Text = Config.Pass, MinWidth = 200 };
            var portLabel = new Label { Content = "Port: " };
            var portText = new TextBox { Text = Config.Port, MinWidth = 200 };
            var databaseLabel = new Label { Content = "Database: " };
            var databaseText = new TextBox { Text = Config.Database, MinWidth = 200 };
            var confirmBtn = new MySQLConfirmButton(hostText, userText, passText, portText, databaseText)
            {
                Content = "Save Changes",
                Foreground = Brushes.Black
            };
            confirmBtn.Click += SaveMySQLConfirmBtn_Click;

            var margin = new Thickness(10, 5, 10, 5);
            hostLabel.Margin = margin;
            hostText.Margin = margin;
            userLabel.Margin = margin;
            userText.Margin = margin;
            passLabel.Margin = margin;
            passText.Margin = margin;
            portLabel.Margin = margin;
            portText.Margin = margin;
            databaseLabel.Margin = margin;
            databaseText.Margin = margin;
            confirmBtn.Margin = new Thickness(10, 10, 10, 5);
            confirmBtn.MinHeight = 40;

            Grid.SetRow(hostLabel, row);
            Grid.SetColumn(hostLabel, 0);
            Grid.SetRow(hostText, row++);
            Grid.SetColumn(hostText, 1);
            Grid.SetRow(userLabel, row);
            Grid.SetColumn(userLabel, 0);
            Grid.SetRow(userText, row++);
            Grid.SetColumn(userText, 1);
            Grid.SetRow(passLabel, row);
            Grid.SetColumn(passLabel, 0);
            Grid.SetRow(passText, row++);
            Grid.SetColumn(passText, 1);
            Grid.SetRow(portLabel, row);
            Grid.SetColumn(portLabel, 0);
            Grid.SetRow(portText, row++);
            Grid.SetColumn(portText, 1);
            Grid.SetRow(databaseLabel, row);
            Grid.SetColumn(databaseLabel, 0);
            Grid.SetRow(databaseText, row++);
            Grid.SetColumn(databaseText, 1);
            Grid.SetRow(confirmBtn, row++);
            Grid.SetColumn(confirmBtn, 1);

            grid.Children.Add(hostLabel);
            grid.Children.Add(hostText);
            grid.Children.Add(userLabel);
            grid.Children.Add(userText);
            grid.Children.Add(passLabel);
            grid.Children.Add(passText);
            grid.Children.Add(portLabel);
            grid.Children.Add(portText);
            grid.Children.Add(databaseLabel);
            grid.Children.Add(databaseText);
            grid.Children.Add(confirmBtn);

            ConfigGrid.Children.Add(grid);
            MySQLConfigGrid = grid;
        }

        private void SaveMySQLConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as MySQLConfirmButton;
            Config.Host = button.Host();
            Config.User = button.Username();
            Config.Pass = button.Pass();
            Config.Port = button.Port();
            Config.Database = button.Database();
            Config.DefaultMpqName = _MpqNameText.Text;
            ShowFlyoutMessage("Saved config.xml - Changes will be loaded on next program startup");
        }

        private void SaveSQLiteConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as SQLiteConfirmButton;
            Config.SQLiteFilename = button.SQLiteFilename();
            Config.DefaultMpqName = _MpqNameText.Text;
            ShowFlyoutMessage("Saved config.xml - Changes will be loaded on next program startup");
        }

        public void ShowFlyoutMessage(string message)
        {
            Flyout.IsOpen = true;
            FlyoutText.Text = message;
        }

        private int BuildMiscValueFieldsConfig(Grid grid, int currentRow)
        {
            var label = new Label { Content = "Use Dynamic Misc Value Fields:" };
            label.Margin = new Thickness(10);

            var checkbox = new System.Windows.Controls.CheckBox { };
            checkbox.Margin = new Thickness(10);

            checkbox.IsChecked = Config.DynamicMiscValueFields;
            checkbox.Checked += DynamicMiscValueFields_Checked;
            checkbox.Unchecked += DynamicMiscValueFields_Checked;
            checkbox.ToolTip = "When this is turned on, MiscValue fields UI will dynamically update based on content type, for example a power selection dropdown for Power auras. " +
                "It also greys out the effect fields the selected spell effect does not use. Turn it off to leave every effect field editable.";

            Grid.SetRow(label, currentRow);
            Grid.SetRow(checkbox, currentRow++);
            Grid.SetColumn(label, 0);
            Grid.SetColumn(checkbox, 1);

            grid.Children.Add(label);
            grid.Children.Add(checkbox);

            return currentRow;
        }

        private int BuildMpqConfig(Grid grid, int currentRow)
        {
            var label = new Label
            {
                Content = "Default MPQ name for Export:",
                Margin = new Thickness(10)
            };

            var textBox = new TextBox
            {
                Margin = new Thickness(10),
                Text = Config.DefaultMpqName
            };
            _MpqNameText = textBox;

            Grid.SetRow(label, currentRow);
            Grid.SetRow(textBox, currentRow++);
            Grid.SetColumn(label, 0);
            Grid.SetColumn(textBox, 1);

            grid.Children.Add(label);
            grid.Children.Add(textBox);

            return currentRow;
        }

        private void DynamicMiscValueFields_Checked(object sender, RoutedEventArgs e)
        {
            Config.DynamicMiscValueFields = (sender as System.Windows.Controls.CheckBox).IsChecked.Value;
        }

        private int BuildCachingConfig(Grid grid, int currentRow)
        {
            var cacheAllLabel = new Label
            {
                Content = "Cache Everything On Load:",
                Margin = new Thickness(10)
            };
            var cacheAllCheckbox = new System.Windows.Controls.CheckBox
            {
                Margin = new Thickness(10),
                IsChecked = Config.CacheAllOnLoad,
                ToolTip = "Off by default. When off, optional DBCs and spell family class masks are only read when something asks for them, " +
                    "which makes startup much faster. Turn this on to read it all up front instead."
            };
            cacheAllCheckbox.Checked += CacheAllOnLoad_Checked;
            cacheAllCheckbox.Unchecked += CacheAllOnLoad_Checked;

            var refreshLabel = new Label
            {
                Content = "Refresh Cache On Spell Selection:",
                Margin = new Thickness(10)
            };
            var refreshCheckbox = new System.Windows.Controls.CheckBox
            {
                Margin = new Thickness(10),
                IsChecked = Config.RefreshCacheOnSpellSelection,
                ToolTip = "On by default. Selecting a spell refetches it and the spells it references from the database, so edits made elsewhere show up. " +
                    "Turn this off to reuse cached spell data until it is saved or reloaded."
            };
            refreshCheckbox.Checked += RefreshCacheOnSpellSelection_Checked;
            refreshCheckbox.Unchecked += RefreshCacheOnSpellSelection_Checked;

            Grid.SetRow(cacheAllLabel, currentRow);
            Grid.SetRow(cacheAllCheckbox, currentRow++);
            Grid.SetColumn(cacheAllLabel, 0);
            Grid.SetColumn(cacheAllCheckbox, 1);

            Grid.SetRow(refreshLabel, currentRow);
            Grid.SetRow(refreshCheckbox, currentRow++);
            Grid.SetColumn(refreshLabel, 0);
            Grid.SetColumn(refreshCheckbox, 1);

            grid.Children.Add(cacheAllLabel);
            grid.Children.Add(cacheAllCheckbox);
            grid.Children.Add(refreshLabel);
            grid.Children.Add(refreshCheckbox);

            return currentRow;
        }

        private void CacheAllOnLoad_Checked(object sender, RoutedEventArgs e)
        {
            Config.CacheAllOnLoad = (sender as System.Windows.Controls.CheckBox).IsChecked.Value;
        }

        private void RefreshCacheOnSpellSelection_Checked(object sender, RoutedEventArgs e)
        {
            Config.RefreshCacheOnSpellSelection = (sender as System.Windows.Controls.CheckBox).IsChecked.Value;
        }

        private int BuildBindingsAndDbcUI(Grid grid, int currentRow)
        {
            var bindingsLabel = new Label { Content = "Bindings Directory:" };
            var dbcLabel = new Label { Content = "DBC Directory:" };
            var bindingsDirLabel = new Label { Content = Config.BindingsDirectory };
            var dbcDirLabel = new Label { Content = Config.DbcDirectory };
            var changeBindingsButton = new ButtonWithLabelRef(bindingsDirLabel, ButtonWithLabelRef.DirButtonType.Bindings)
            {
                Content = "Change Directory",
                Foreground = Brushes.Black
            };
            var changeDbcButton = new ButtonWithLabelRef(dbcDirLabel, ButtonWithLabelRef.DirButtonType.Dbc)
            {
                Content = "Change Directory",
                Foreground = Brushes.Black
            };

            bindingsLabel.Margin = new Thickness(10);
            dbcLabel.Margin = new Thickness(10);
            bindingsDirLabel.Margin = new Thickness(10);
            dbcDirLabel.Margin = new Thickness(10);
            changeBindingsButton.Margin = new Thickness(10);
            changeDbcButton.Margin = new Thickness(10);
            changeBindingsButton.MinWidth = 100;
            changeDbcButton.MinWidth = 100;

            changeBindingsButton.Click += OpenDirButton_Click;
            changeDbcButton.Click += OpenDirButton_Click;

            Grid.SetRow(bindingsLabel, currentRow);
            Grid.SetColumn(bindingsLabel, 0);
            Grid.SetRow(bindingsDirLabel, currentRow);
            Grid.SetColumn(bindingsDirLabel, 1);
            Grid.SetRow(changeBindingsButton, currentRow);
            Grid.SetColumn(changeBindingsButton, 2);
            Grid.SetRow(dbcLabel, ++currentRow);
            Grid.SetColumn(dbcLabel, 0);
            Grid.SetRow(dbcDirLabel, currentRow);
            Grid.SetColumn(dbcDirLabel, 1);
            Grid.SetRow(changeDbcButton, currentRow);
            Grid.SetColumn(changeDbcButton, 2);

            grid.Children.Add(bindingsLabel);
            grid.Children.Add(bindingsDirLabel);
            grid.Children.Add(changeBindingsButton);
            grid.Children.Add(dbcLabel);
            grid.Children.Add(dbcDirLabel);
            grid.Children.Add(changeDbcButton);

            return currentRow;
        }

        /// <summary>A separate server to the one holding the DBC data, so it gets its own fields.</summary>
        private int BuildEmulatorConfigUI(Grid parent, int currentRow)
        {
            var grid = new Grid();
            Grid.SetRow(grid, currentRow);
            Grid.SetColumn(grid, 0);
            Grid.SetColumnSpan(grid, 3);

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (var i = 0; i < 9; ++i)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var margin = new Thickness(10, 5, 10, 5);
            var header = new Label
            {
                Content = "Emulator world database (optional)",
                Margin = new Thickness(10, 20, 10, 5),
                FontWeight = FontWeights.Bold
            };

            var enabledLabel = new Label { Content = "Enable world database tabs:", Margin = margin };
            var enabledCheckbox = new System.Windows.Controls.CheckBox
            {
                Margin = margin,
                IsChecked = Config.EmulatorEnabled,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Adds tabs for editing the server side spell tables of an emulator world database. " +
                    "This only works against a WotLK core, so leave it off for anything else."
            };

            var selected = Emulators.Lookup(Config.EmulatorType);
            var emulatorLabel = new Label { Content = "Emulator: ", Margin = margin };
            var emulatorButton = new SplitButton { Margin = margin, MinWidth = 200 };
            foreach (var emulator in Emulators.All)
                emulatorButton.Items.Add(emulator.Name);
            emulatorButton.SelectedIndex = Emulators.All.ToList().IndexOf(selected);
            emulatorButton.ToolTip = selected.Description;

            var hostLabel = new Label { Content = "Hostname: ", Margin = margin };
            var hostText = new TextBox { Text = Config.EmulatorHost, MinWidth = 200, Margin = margin };
            var portLabel = new Label { Content = "Port: ", Margin = margin };
            var portText = new TextBox { Text = Config.EmulatorPort, MinWidth = 200, Margin = margin };
            var userLabel = new Label { Content = "Username: ", Margin = margin };
            var userText = new TextBox { Text = Config.EmulatorUser, MinWidth = 200, Margin = margin };
            var passLabel = new Label { Content = "Password: ", Margin = margin };
            var passText = new TextBox { Text = Config.EmulatorPass, MinWidth = 200, Margin = margin };
            var databaseLabel = new Label { Content = "World database: ", Margin = margin };
            var databaseText = new TextBox { Text = Config.EmulatorDatabase, MinWidth = 200, Margin = margin };

            // Only swap the defaults across when the fields still hold the other core's defaults
            var previous = selected;
            emulatorButton.SelectionChanged += (sender, args) =>
            {
                var chosen = Emulators.All.FirstOrDefault(entry => entry.Name == emulatorButton.SelectedItem?.ToString());
                if (chosen == null)
                    return;
                if (userText.Text.Trim() == previous.DefaultUser)
                    userText.Text = chosen.DefaultUser;
                if (databaseText.Text.Trim() == previous.DefaultDatabase)
                    databaseText.Text = chosen.DefaultDatabase;
                emulatorButton.ToolTip = chosen.Description;
                previous = chosen;
            };

            var confirmBtn = new EmulatorConfirmButton(enabledCheckbox, emulatorButton, hostText, portText, userText, passText, databaseText)
            {
                Content = "Save Changes",
                Foreground = Brushes.Black,
                Margin = new Thickness(10, 10, 10, 5),
                MinHeight = 40
            };
            confirmBtn.Click += SaveEmulatorConfirmBtn_Click;

            var testBtn = new EmulatorConfirmButton(enabledCheckbox, emulatorButton, hostText, portText, userText, passText, databaseText)
            {
                Content = "Test Connection",
                Foreground = Brushes.Black,
                Margin = new Thickness(10, 10, 10, 5),
                MinHeight = 40
            };
            testBtn.Click += TestEmulatorConnection_Click;

            var row = 0;
            Grid.SetRow(header, row++);
            Grid.SetColumn(header, 0);
            Grid.SetColumnSpan(header, 3);

            void AddPair(Label label, UIElement field)
            {
                Grid.SetRow(label, row);
                Grid.SetColumn(label, 0);
                Grid.SetRow(field, row++);
                Grid.SetColumn(field, 1);
                grid.Children.Add(label);
                grid.Children.Add(field);
            }

            grid.Children.Add(header);
            AddPair(enabledLabel, enabledCheckbox);
            AddPair(emulatorLabel, emulatorButton);
            AddPair(hostLabel, hostText);
            AddPair(portLabel, portText);
            AddPair(userLabel, userText);
            AddPair(passLabel, passText);
            AddPair(databaseLabel, databaseText);

            Grid.SetRow(confirmBtn, row);
            Grid.SetColumn(confirmBtn, 1);
            Grid.SetRow(testBtn, row);
            Grid.SetColumn(testBtn, 2);
            grid.Children.Add(confirmBtn);
            grid.Children.Add(testBtn);

            parent.Children.Add(grid);
            return currentRow;
        }

        private void SaveEmulatorConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as EmulatorConfirmButton;
            Config.EmulatorType = button.Emulator().Id;
            Config.EmulatorHost = button.Host();
            Config.EmulatorPort = button.Port();
            Config.EmulatorUser = button.Username();
            Config.EmulatorPass = button.Pass();
            Config.EmulatorDatabase = button.Database();
            // Set last, it is the flag the main window watches to show or hide the tabs
            Config.EmulatorEnabled = button.Enabled();

            (Application.Current.MainWindow as MainWindow)?.ReloadEmulatorIntegration();
            ShowFlyoutMessage($"Saved config.xml - The {button.Emulator().Name} tabs connect the next time you open one");
        }

        private async void TestEmulatorConnection_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as EmulatorConfirmButton;
            var emulator = button.Emulator();
            button.IsEnabled = false;
            try
            {
                var database = new WorldDatabase(emulator, button.Host(), button.Port(),
                    button.Username(), button.Pass(), button.Database());
                var missing = await System.Threading.Tasks.Task.Run(() =>
                {
                    database.TestConnection();
                    return database.FindMissingTables(emulator.Tables.Select(table => table.Name));
                });

                ShowFlyoutMessage(missing.Count == 0
                    ? $"Connected to {button.Database()} and found all the {emulator.Name} spell tables."
                    : $"Connected to {button.Database()}, but these tables are missing: {string.Join(", ", missing)}");
            }
            catch (Exception exception)
            {
                Logger.Error(exception, $"{emulator.Name} test connection failed");
                ShowFlyoutMessage("Could not connect: " + exception.Message);
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private void OpenDirButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as ButtonWithLabelRef;
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.RootFolder = SpecialFolder.MyComputer;
                var text = button.GetLabelText();
                if (text.StartsWith("\\"))
                {
                    text = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
                }
                dialog.SelectedPath = text;
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var path = dialog.SelectedPath;
                    button.UpdateLabelText(path);
                    if (button.DbType == ButtonWithLabelRef.DirButtonType.Bindings)
                    {
                        Config.BindingsDirectory = path;
                    }
                    else if (button.DbType == ButtonWithLabelRef.DirButtonType.Dbc)
                    {
                        Config.DbcDirectory = path;
                    }
                }
            }
        }

        private class DatabaseTypeContainer
        {
            private List<DatabaseType> _AvailableTypes = new List<DatabaseType>();
            
            public DatabaseType LookupDatabaseTypeName(string name) => _AvailableTypes.Find(type => type.Name == name);

            public void AddDatabaseType(DatabaseType type)
            {
                _AvailableTypes.Add(type);
            }

            public List<DatabaseType> StoredTypes() => _AvailableTypes;

            public DatabaseType GetDatabaseTypeForId(DatabaseIdentifier defaultConfigType) => 
                _AvailableTypes.Find(type => type.Identity == defaultConfigType);
        }

        private class DatabaseType
        {
            public string Name;
            public DatabaseIdentifier Identity;
            public int Index;
        }

        public enum DatabaseIdentifier
        {
            MySQL,
            SQLite
        }

        private class ButtonWithLabelRef : Button
        {
            private readonly Label _LabelReference;
            public readonly DirButtonType DbType;

            public ButtonWithLabelRef(Label labelRef, DirButtonType type)
            {
                _LabelReference = labelRef;
                DbType = type;
            }

            public void UpdateLabelText(string text)
            {
                _LabelReference.Content = text;
            }

            public string GetLabelText() => _LabelReference.Content.ToString();

            public enum DirButtonType
            {
                Bindings,
                Dbc
            }
        }

        private class MySQLConfirmButton : Button
        {
            private readonly TextBox _Host, _User, _Pass, _Port, _Database;

            public MySQLConfirmButton(TextBox host, TextBox user, TextBox pass, TextBox port, TextBox database)
            {
                _Host = host;
                _User = user;
                _Pass = pass;
                _Port = port;
                _Database = database;
            }

            public string Host() => _Host.Text;

            public string Username() => _User.Text;

            public string Pass() => _Pass.Text;

            public string Port() => _Port.Text;

            public string Database() => _Database.Text;
        }

        private class EmulatorConfirmButton : Button
        {
            private readonly System.Windows.Controls.CheckBox _Enabled;
            private readonly SplitButton _Emulator;
            private readonly TextBox _Host, _Port, _User, _Pass, _Database;

            public EmulatorConfirmButton(System.Windows.Controls.CheckBox enabled, SplitButton emulator, TextBox host,
                TextBox port, TextBox user, TextBox pass, TextBox database)
            {
                _Enabled = enabled;
                _Emulator = emulator;
                _Host = host;
                _Port = port;
                _User = user;
                _Pass = pass;
                _Database = database;
            }

            public bool Enabled() => _Enabled.IsChecked == true;

            public Sources.Emulator.Emulator Emulator() =>
                Emulators.All.FirstOrDefault(entry => entry.Name == _Emulator.SelectedItem?.ToString()) ?? Emulators.Default;

            public string Host() => _Host.Text.Trim();

            public string Port() => _Port.Text.Trim();

            public string Username() => _User.Text.Trim();

            public string Pass() => _Pass.Text;

            public string Database() => _Database.Text.Trim();
        }

        private class SQLiteConfirmButton : Button
        {
            private readonly TextBox _SQLiteFilenameText;

            public SQLiteConfirmButton(TextBox sqliteFileNameText)
            {
                _SQLiteFilenameText = sqliteFileNameText;
            }

            public string SQLiteFilename() => _SQLiteFilenameText.Text;
        }
    }
}
