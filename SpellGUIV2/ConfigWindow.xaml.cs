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

namespace SpellEditor
{
    partial class ConfigWindow
    {
        private DatabaseTypeContainer TypeContainer = new DatabaseTypeContainer();
        private Grid MySQLConfigGrid = null;
        private Grid SQLiteConfigGrid = null;

        public ConfigWindow()
        {
            InitializeComponent();
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

            var sqlite = new DatabaseType() { Index = 0, Name = "SQLite", Identity = DatabaseType.DatabaseIdentifier.SQLite };
            var mysql = new DatabaseType() { Index = 1, Name = "MySQL", Identity = DatabaseType.DatabaseIdentifier.MySQL };

            TypeContainer.AddDatabaseType(sqlite);
            TypeContainer.AddDatabaseType(mysql);

            splitButton.Items.Add(sqlite.Name);
            splitButton.Items.Add(mysql.Name);

            splitButton.SelectedIndex = 0;
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

            BuildBindingsAndDbcUI(ConfigGrid, 1);

            BuildSQLiteConfigUI();
            BuildMySQLConfigUI();

            var selectedConfigType = TypeContainer.LookupDatabaseTypeName(splitButton.SelectedItem.ToString());
            ToggleGridVisibility(selectedConfigType.Identity);
        }

        private void SplitButton_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = e.AddedItems[0].ToString();
            var selectedType = TypeContainer.LookupDatabaseTypeName(selectedItem);

            ToggleGridVisibility(selectedType.Identity);
        }

        private void ToggleGridVisibility(DatabaseType.DatabaseIdentifier selectedType)
        {
            SQLiteConfigGrid.Visibility = selectedType == DatabaseType.DatabaseIdentifier.SQLite ? Visibility.Visible : Visibility.Collapsed;
            MySQLConfigGrid.Visibility = selectedType == DatabaseType.DatabaseIdentifier.MySQL ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BuildSQLiteConfigUI()
        {
            var grid = new Grid();

            Grid.SetRow(grid, 2);
            Grid.SetColumn(grid, 0);
            Grid.SetColumnSpan(grid, 2);

            ConfigGrid.Children.Add(grid); 
            SQLiteConfigGrid = grid;
        }

        private void BuildMySQLConfigUI()
        {
            var grid = new Grid();

            Grid.SetRow(grid, 2);
            Grid.SetColumn(grid, 0);
            Grid.SetColumnSpan(grid, 2);

            ConfigGrid.Children.Add(grid);
            MySQLConfigGrid = grid;
        }

        private int BuildBindingsAndDbcUI(Grid grid, int currentRow)
        {
            var bindingsLabel = new Label() { Content = "Bindings Directory:" };
            var dbcLabel = new Label() { Content = "DBC Directory:" };
            var bindingsDirLabel = new Label() { Content = Config.GetConfigValue("BindingsDirectory") };
            var dbcDirLabel = new Label() { Content = Config.GetConfigValue("DbcDirectory") };
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
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var path = dialog.SelectedPath;
                    button.UpdateLabelText(path);
                    Config.UpdateConfigValue(
                        button.DbType == ButtonWithLabelRef.DirButtonType.Bindings ? "BindingsDirectory" : "DbcDirectory",
                        path);
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
        }

        private class DatabaseType
        {
            public string Name;
            public DatabaseIdentifier Identity;
            public int Index;

            public enum DatabaseIdentifier
            {
                MySQL,
                SQLite
            }
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

            public enum DirButtonType
            {
                Bindings,
                Dbc
            }
        }
    };
};
