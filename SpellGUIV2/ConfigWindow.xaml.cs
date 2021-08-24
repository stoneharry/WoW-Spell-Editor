using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using NLog;
using SpellEditor.Sources.Config;
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
            // Bindings and directory settings, 2 rows
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // Icon config row
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

            currentRow = BuildIconConfig(ConfigGrid, currentRow);

            currentRow = BuildBindingsAndDbcUI(ConfigGrid, currentRow);

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

            dbFileNameText.Margin =  new Thickness(10, 5, 10, 5);
            dbFileNameText.Margin = new Thickness(10, 5, 10, 5);
            confirmBtn.Margin = new Thickness(3, 10, 3, 2);
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
            confirmBtn.Margin = new Thickness(3, 10, 3, 2);
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
            ShowFlyoutMessage("Saved config.xml - Changes will be loaded on next program startup");
        }

        private void SaveSQLiteConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as SQLiteConfirmButton;
            Config.SQLiteFilename = button.SQLiteFilename();
            ShowFlyoutMessage("Saved config.xml - Changes will be loaded on next program startup");
        }

        public void ShowFlyoutMessage(string message)
        {
            Flyout.IsOpen = true;
            FlyoutText.Text = message;
        }

        private int BuildIconConfig(Grid grid, int currentRow)
        {
            var label = new Label { Content = "Render only spell icons in view:" };
            label.Margin = new Thickness(10);

            var checkbox = new System.Windows.Controls.CheckBox { };
            checkbox.Margin = new Thickness(10);

            checkbox.IsChecked = Config.RenderImagesInView;
            checkbox.Checked += RenderIconsInView_Checked;
            checkbox.Unchecked += RenderIconsInView_Checked;
            checkbox.ToolTip = "When this is turned on, the Icon tab will only load the images currently visible on the screen. It makes it slower to scroll but it can handle more images without crashing.";

            Grid.SetRow(label, currentRow);
            Grid.SetRow(checkbox, currentRow++);
            Grid.SetColumn(label, 0);
            Grid.SetColumn(checkbox, 1);

            grid.Children.Add(label);
            grid.Children.Add(checkbox);

            return currentRow;
        }

        private void RenderIconsInView_Checked(object sender, RoutedEventArgs e)
        {
            Config.RenderImagesInView = (sender as System.Windows.Controls.CheckBox).IsChecked.Value;
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
