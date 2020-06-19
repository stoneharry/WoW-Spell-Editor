using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NLog;
using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Database;

namespace SpellEditor
{
    partial class ImportExportWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IDatabaseAdapter _Adapter;
        public volatile string MpqArchiveName;
        public volatile List<string> BindingImportList = new List<string>();
        public volatile List<string> BindingExportList = new List<string>();

        public bool IsDataSelected() => BindingImportList.Count > 0 || BindingExportList.Count > 0;

        public ImportExportWindow(IDatabaseAdapter adapter)
        {
            _Adapter = adapter;
            InitializeComponent();
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Info("ERROR: " + e.Exception.Message);
            File.WriteAllText("error.txt", e.Exception.Message, Encoding.GetEncoding(0));
            e.Handled = true;
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
            BuildImportTab();
            BuildExportTab();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var item = sender as TabControl;
        }

        private void BuildImportTab()
        {
            var contents = ImportGridDbcs.Children;
            if (contents.Count > 0)
                return;
            foreach (var binding in BindingManager.GetInstance().GetAllBindings())
            {
                var numRows = binding.GetNumRowsInTable(_Adapter);
                contents.Add(new CheckBox
                {
                    Name = binding.Name + "ImportCheckBox",
                    Content = $"{binding.Name}.dbc {(numRows > 0 ? $"- {numRows} rows" : "")}",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(1),
                    IsEnabled = numRows == 0,
                    IsChecked = numRows == 0 && 
                        (binding.Name.Equals("Spell") || 
                        binding.Name.Contains("SpellVisual"))
                });
            }
        }

        private void BuildExportTab()
        {
            var contents = ExportGridDbcs.Children;
            if (contents.Count > 0)
                return;
            foreach (var binding in BindingManager.GetInstance().GetAllBindings())
            {
                var numRows = binding.GetNumRowsInTable(_Adapter);
                contents.Add(new CheckBox
                {
                    Name = binding.Name + "ExportCheckBox",
                    Content = $"{binding.Name} {(numRows > 0 ? numRows.ToString() : "")} {(numRows > 0 ? "rows " : "")}to Export\\{binding.Name}.dbc",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(1),
                    IsEnabled = numRows > 0,
                    IsChecked = numRows > 0 && 
                        (binding.Name.Equals("Spell") ||
                        binding.Name.Contains("SpellVisual"))
                });
            }
        }

        private void MpqClick(object sender, RoutedEventArgs e)
        {
            var archiveName = ExportMpqNameTxt.Text.Length > 0 ? ExportMpqNameTxt.Text : "empty.mpq";
            archiveName = archiveName.EndsWith(".mpq") ? archiveName : archiveName + ".mpq";
            MpqArchiveName = archiveName;
            ClickHandler(false);
        }

        private void ImportClick(object sender, RoutedEventArgs e) => ClickHandler(true);
        private void ExportClick(object sender, RoutedEventArgs e) => ClickHandler(false);
        private void ClickHandler(bool isImport)
        {
            var bindingNameList = new List<string>();
            var children = isImport ? ImportGridDbcs.Children : ExportGridDbcs.Children;
            var prefix = isImport ? "Import" : "Export";
            foreach (var element in children)
            {
                if (element is CheckBox box)
                {
                    if (box.IsChecked.HasValue && box.IsChecked.Value)
                        bindingNameList.Add(box.Name.Substring(0, box.Name.IndexOf(prefix + "CheckBox")));
                }
            }
            if (isImport)
                BindingImportList = bindingNameList;
            else
                BindingExportList = bindingNameList;
            Logger.Info($"Bindings selected to {prefix.ToLower()}: {String.Join(", ", bindingNameList)}");
        }
    }
}
