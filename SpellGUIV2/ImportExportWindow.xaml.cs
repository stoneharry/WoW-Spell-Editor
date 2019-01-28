using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Threading;
using SpellEditor.Sources.Binding;
using System.Windows.Controls.Primitives;
using System.Threading;
using SpellEditor.Sources.Config;

namespace SpellEditor
{
	partial class ImportExportWindow
    {
        private IDatabaseAdapter _Adapter;
        public List<string> BindingImportList = new List<string>();

        public ImportExportWindow(IDatabaseAdapter adapter)
        {
            _Adapter = adapter;
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
            BuildImportTab();
            BuildExportTab();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = sender as TabControl;
        }

        private void BuildImportTab()
        {
            var contents = ImportGrid.Children;
            if (contents.Count > 0)
                return;
            contents.Add(new Label()
            {
                Content = "The Spell DBC is the file that needs to be imported for this program to work."
            });
            var importBtn = new Button()
            {
                Content = "Import Checked DBC Files",
                Padding = new Thickness(4, 10, 4, 10)
            };
            importBtn.Click += ImportClick;
            contents.Add(importBtn);
            foreach (var binding in BindingManager.GetInstance().GetAllBindings())
            {
                var noData = !TableHasData(binding.Name);
                contents.Add(new CheckBox()
                {
                    Name = binding.Name + "CheckBox",
                    Content = $"Import DBC\\{binding.Name}.dbc",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsEnabled = noData,
                    IsChecked = noData && binding.Name.Equals("Spell")
                });
            }
        }

        private bool TableHasData(string tableName)
        {
            try
            {
                var table = _Adapter.Query("SELECT COUNT(*) FROM " + tableName);
                if (table.Rows.Count == 1)
                {
                    if (int.Parse(table.Rows[0][0].ToString()) > 0)
                        return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("WARNING: ImportExportWindow triggered: " + e.Message);
                return false;
            }
        }

        private void ImportClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var bindingNameList = new List<string>();
            foreach (var element in ImportGrid.Children)
            {
                if (element is CheckBox)
                {
                    var box = element as CheckBox;
                    if (box.IsChecked.HasValue && box.IsChecked.Value)
                        bindingNameList.Add(box.Name.Substring(0, box.Name.IndexOf("CheckBox")));
                }
            }
            Console.WriteLine("Bindings selected to import: " + String.Join(", ", bindingNameList));
            BindingImportList = bindingNameList;
        }

        private void BuildExportTab()
        {
            var contents = ExportGrid.Children;
            if (contents.Count > 0)
                return;
        }
    };
};
