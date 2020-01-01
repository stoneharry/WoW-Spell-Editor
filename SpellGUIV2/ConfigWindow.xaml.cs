using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using System.Windows.Media;
using SpellEditor.Sources.Config;
using static System.Environment;

namespace SpellEditor
{
    partial class ConfigWindow
    {
        private DatabaseTypeContainer TypeContainer = new DatabaseTypeContainer();
        private Grid MySQLConfigGrid = null;
        private Grid SQLiteConfigGrid = null;
        private DatabaseIdentifier defaultConfigType;

        public ConfigWindow(DatabaseIdentifier defaultConfigToShow)
        {
            InitializeComponent();
            defaultConfigType = defaultConfigToShow;
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Console.WriteLine("ERROR: " + e.Exception.Message);
            File.WriteAllText("error.txt", e.Exception.Message, UTF8Encoding.GetEncoding(0));
            e.Handled = true;
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            ConfigGrid.Children.Clear();
            BuildConfigWindow();
        }

        private void BuildConfigWindow()
        {
            ConfigGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            ConfigGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            ConfigGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            // Database type row
            ConfigGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            // Bindings and directory settings, 2 rows
            ConfigGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            ConfigGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            // Database type specific grid
            ConfigGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            var splitLabel = new Label() { Content = "Database type:" };
            var splitButton = new SplitButton();

            var sqlite = new DatabaseType() { Index = 0, Name = "SQLite", Identity = DatabaseIdentifier.SQLite };
            var mysql = new DatabaseType() { Index = 1, Name = "MySQL", Identity = DatabaseIdentifier.MySQL };

            TypeContainer.AddDatabaseType(sqlite);
            TypeContainer.AddDatabaseType(mysql);

            splitButton.Items.Add(sqlite.Name);
            splitButton.Items.Add(mysql.Name);

            splitButton.SelectedIndex = TypeContainer.GetDatabaseTypeForId(defaultConfigType).Index;
            splitButton.MinWidth = 150;

            splitButton.SelectionChanged += SplitButton_SelectionChanged;

            splitLabel.Margin = new Thickness(10);
            splitButton.Margin = new Thickness(25, 10, 25, 15);

            Grid.SetRow(splitLabel, 0);
            Grid.SetColumn(splitLabel, 0);
            Grid.SetRow(splitButton, 0);
            Grid.SetColumn(splitButton, 1);
            Grid.SetColumnSpan(splitButton, 2);

            ConfigGrid.Children.Add(splitLabel);
            ConfigGrid.Children.Add(splitButton);

            int currentRow = BuildBindingsAndDbcUI(ConfigGrid, 1);

            BuildSQLiteConfigUI(currentRow);
            BuildMySQLConfigUI(++currentRow);

            var selectedConfigType = TypeContainer.LookupDatabaseTypeName(splitButton.SelectedItem.ToString());
            ToggleGridVisibility(selectedConfigType.Identity);
        }

        private void SplitButton_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

            ConfigGrid.Children.Add(grid); 
            SQLiteConfigGrid = grid;
        }

        private void BuildMySQLConfigUI(int row)
        {
            var grid = new Grid();

            Grid.SetRow(grid, row);
            Grid.SetColumn(grid, 0);
            Grid.SetColumnSpan(grid, 3);

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            row = 0;

            var hostLabel = new Label() { Content = "Hostname: " };
            var hostText = new TextBox() { Text = Config.Host, MinWidth = 200 };
            var userLabel = new Label() { Content = "Username: " };
            var userText = new TextBox() { Text = Config.User, MinWidth = 200 };
            var passLabel = new Label() { Content = "Password: " };
            var passText = new TextBox() { Text = Config.Pass, MinWidth = 200 };
            var portLabel = new Label() { Content = "Port: " };
            var portText = new TextBox() { Text = Config.Port, MinWidth = 200 };
            var databaseLabel = new Label() { Content = "Database: " };
            var databaseText = new TextBox() { Text = Config.Database, MinWidth = 200 };
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
            ShowFlyoutMessage("Saved config.xml");
        }

        public void ShowFlyoutMessage(string message)
        {
            Flyout.IsOpen = true;
            FlyoutText.Text = message;
        }

        private int BuildBindingsAndDbcUI(Grid grid, int currentRow)
        {
            var bindingsLabel = new Label() { Content = "Bindings Directory:" };
            var dbcLabel = new Label() { Content = "DBC Directory:" };
            var bindingsDirLabel = new Label() { Content = Config.BindingsDirectory };
            var dbcDirLabel = new Label() { Content = Config.DbcDirectory };
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
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = SpecialFolder.MyComputer;
                dialog.SelectedPath = button.GetLabelText();
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
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
    };
};
