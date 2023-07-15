using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using MahApps.Metro.Controls;

using NLog;

using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Config;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Tools.MPQ;

namespace SpellEditor
{
    partial class ImportExportWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IDatabaseAdapter _Adapter;
        private ConcurrentDictionary<int, ProgressBar> _TaskLookup;
        private string _MpqArchiveName;
        private Action _PopulateSelectSpell;
        private Action _ReloadData;

        public ImportExportWindow(IDatabaseAdapter adapter, Action populateSelectSpell, Action reloadData)
        {
            _Adapter = adapter;
            _TaskLookup = new ConcurrentDictionary<int, ProgressBar>();
            _PopulateSelectSpell = populateSelectSpell;
            _ReloadData = reloadData;
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
            BuildImportExportTab();
            ExportMpqNameTxt.Text = string.IsNullOrEmpty(Config.DefaultMpqName) ? "patch-4.MPQ" : Config.DefaultMpqName;
        }

        private bool IsDefaultImport(string name)
        {
            return name.Equals("spell") ||
                name.Contains("spellvisual") ||
                name.Contains("spellmissile");
        }

        private void BuildImportExportTab()
        {
            if (ImportTypeCombo.Items.Count == 0)
            {
                foreach (var type in Enum.GetValues(typeof(ImportExportType)))
                {
                    ImportTypeCombo.Items.Add(type.ToString());
                    ExportTypeCombo.Items.Add(type.ToString());
                }
                ImportTypeCombo.SelectedIndex = 0;
                ExportTypeCombo.SelectedIndex = 0;
            }

            var importContents = ImportGridDbcs.Children;
            if (importContents.Count > 0)
                return;
            var exportContents = ExportGridDbcs.Children;
            if (exportContents.Count > 0)
                return;
            var bindings = new List<Binding>(BindingManager.GetInstance().GetAllBindings());
            // Build initial checkboxes
            bindings.ForEach((binding) =>
            {
                // Import
                importContents.Add(new CheckBox
                {
                    Name = binding.Name + "ImportCheckBox",
                    Content = $"{binding.Name}.dbc Loading...",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(1),
                    IsEnabled = false,
                    IsChecked = false
                });
                importContents.Add(new ProgressBar
                {
                    Name = binding.Name + "ImportProgressBar",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(1, 1, 5, 1),
                    Width = 200,
                    Visibility = Visibility.Hidden
                });
                // Export
                exportContents.Add(new CheckBox
                {
                    Name = binding.Name + "ExportCheckBox",
                    Content = $"{binding.Name} Loading...",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(1),
                    IsEnabled = false,
                    IsChecked = false
                });
                exportContents.Add(new ProgressBar
                {
                    Name = binding.Name + "ImportProgressBar",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(1, 1, 5, 1),
                    Width = 200,
                    Visibility = Visibility.Hidden
                });
            });
            var totalBindings = bindings.Count;
            var loadedCount = 0;
            // Populate all contents asynchronously (expensive)
            Task.Run(() => bindings.AsParallel().ForAll(binding =>
            {
                var numRows = binding.GetNumRowsInTable(_Adapter);
                Dispatcher.InvokeAsync(new Action(() =>
                {
                    // Import
                    CheckBox box = null;
                    foreach (UIElement element in importContents)
                    {
                        if (element is CheckBox child && child.Name.StartsWith(binding.Name))
                        {
                            box = child;
                            break;
                        }
                    }
                    if (box != null)
                    {
                        box.Content = $"{binding.Name}.dbc {(numRows > 0 ? $"- {numRows} rows" : "")}";
                        box.IsEnabled = numRows == 0;
                        box.IsChecked = numRows == 0 && IsDefaultImport(binding.Name.ToLower());
                    }
                    // Export
                    box = null;
                    foreach (UIElement element in exportContents)
                    {
                        if (element is CheckBox child && child.Name.StartsWith(binding.Name))
                        {
                            box = child;
                            break;
                        }
                    }
                    if (box != null)
                    {
                        box.Content = $"{binding.Name} {(numRows > 0 ? numRows.ToString() : "")} {(numRows > 0 ? "rows " : "")}to Export\\{binding.Name}.dbc";
                        box.IsEnabled = numRows > 0;
                        box.IsChecked = numRows > 0 && IsDefaultImport(binding.Name.ToLower());
                    }
                    ++loadedCount;
                    ImportLoadedCount.Content = $"Loaded: {loadedCount} / {totalBindings}";
                    ExportLoadedCount.Content = $"Loaded: {loadedCount} / {totalBindings}";
                }));
            }));
        }

        private void MpqClick(object sender, RoutedEventArgs e)
        {
            var archiveName = ExportMpqNameTxt.Text.Length > 0 ? ExportMpqNameTxt.Text : "empty.mpq";
            archiveName = archiveName.EndsWith(".mpq") ? archiveName : archiveName + ".mpq";
            _MpqArchiveName = archiveName;
            ClickHandler(false);
        }

        private void ImportClick(object sender, RoutedEventArgs e) => ClickHandler(true);
        private void ExportClick(object sender, RoutedEventArgs e) => ClickHandler(false);
        private void ClickHandler(bool isImport)
        {
            ImportExportType useType;
            if (Enum.TryParse(isImport ? ImportTypeCombo.Text : ExportTypeCombo.Text, out ImportExportType _type))
                useType = _type;
            else
                useType = ImportExportType.DBC;
            var bindingNameList = new List<string>();
            var children = isImport ? ImportGridDbcs.Children : ExportGridDbcs.Children;
            var prefix = isImport ? "Import" : "Export";
            bool showBar = false;
            foreach (var element in children)
            {
                if (element is CheckBox box)
                {
                    showBar = box.IsChecked.HasValue && box.IsChecked.Value;
                    if (showBar)
                        bindingNameList.Add(box.Name.Substring(0, box.Name.IndexOf(prefix + "CheckBox")));
                }
                else if (element is ProgressBar bar)
                {
                    bar.Visibility = showBar ? Visibility.Visible : Visibility.Hidden;
                }
            }
            Logger.Info($"Bindings selected to {prefix.ToLower()}: {string.Join(", ", bindingNameList)}");

            // Now we want to disable the UI elements and update the progress bars
            ImportClickBtn.IsEnabled = false;
            ExportClickBtn1.IsEnabled = false;
            ExportClickBtn2.IsEnabled = false;

            doImportExport(isImport, bindingNameList, useType);
        }


        private void SelectAllChanged(object sender, RoutedEventArgs e)
        {
            var isChecked = (sender as CheckBox).IsChecked;
            var isImport = sender == ImportSelectAll;
            var contents = isImport ? ImportGridDbcs.Children : ExportGridDbcs.Children;
            for (int i = 0; i < contents.Count; ++i)
            {
                if (contents[i] is CheckBox box && box.IsEnabled)
                    box.IsChecked = isChecked;
            }
        }

        private void doImportExport(bool isImport, List<string> bindingList, ImportExportType useType)
        {
            var manager = DBCManager.GetInstance();

            Dictionary<string, ProgressBar> barLookup = new Dictionary<string, ProgressBar>();
            bindingList.ForEach(bindingName => barLookup.Add(bindingName, LookupProgressBar(bindingName, isImport)));

            var label = isImport ? ImportLoadedCount : ExportLoadedCount;

            Task.Run(() =>
            {
                ConcurrentBag<Task> bag = new ConcurrentBag<Task>();

                // Start tasks
                bindingList.AsParallel().ForAll(bindingName =>
                {
                    try
                    {
                        manager.ClearDbcBinding(bindingName);
                        var abstractDbc = manager.FindDbcForBinding(bindingName);
                        if (abstractDbc == null)
                        {
                            try
                            {
                                abstractDbc = new GenericDbc($"{Config.DbcDirectory}\\{bindingName}.dbc");
                            }
                            catch (Exception exception)
                            {
                                Logger.Info($"ERROR: Failed to load {Config.DbcDirectory}\\{bindingName}.dbc: {exception.Message}\n{exception}\n{exception.InnerException}");
                                ShowFlyoutMessage($"Failed to load {Config.DbcDirectory}\\{bindingName}.dbc");
                                return;
                            }
                        }
                        if (isImport && !abstractDbc.HasData())
                            abstractDbc.ReloadContents();

                        int id;
                        if (isImport)
                        {
                            var task = abstractDbc.ImportTo(_Adapter, SetProgress, "ID", bindingName, useType);
                            bag.Add(task);
                            id = task.Id;
                        }
                        else
                        {
                            var task = abstractDbc.ExportTo(_Adapter, SetProgress, "ID", bindingName, useType);
                            bag.Add(task);
                            id = task.Id;
                        }
                        _TaskLookup.TryAdd(id, barLookup[bindingName]);
                    }
                    catch (Exception exception)
                    {
                        Logger.Info($"ERROR: Failed to load {Config.DbcDirectory}\\{bindingName}.dbc: {exception.Message}\n{exception}\n{exception.InnerException}");
                        ShowFlyoutMessage($"Failed to load {Config.DbcDirectory}\\{bindingName}.dbc");
                    }
                });

                // Wait for all tasks to complete
                List<Task> allTasks = bag.ToList();
                Dispatcher.InvokeAsync(new Action(() => label.Content = $"Remaining: {allTasks.Count}"));
                while (allTasks.Count > 0)
                {
                    Thread.Sleep(100);
                    for (int i = allTasks.Count - 1; i >= 0; --i)
                    {
                        var task = allTasks[i];
                        if (task.IsCompleted)
                        {
                            var bar = _TaskLookup[task.Id];
                            Dispatcher.InvokeAsync(new Action(() => bar.Value = 100));
                            allTasks.RemoveAt(i);
                        }
                    }
                    Dispatcher.InvokeAsync(new Action(() => label.Content = $"Remaining: {allTasks.Count}"));
                }

                // Create MPQ if required
                if (!string.IsNullOrEmpty(_MpqArchiveName))
                {
                    var exportList = new List<string>();
                    Directory.EnumerateFiles("Export")
                        .Where((dbcFile) => dbcFile.EndsWith(".dbc"))
                        .ToList()
                        .ForEach(exportList.Add);
                    var mpqExport = new MpqExport();
                    mpqExport.CreateMpqFromDbcFileList(_MpqArchiveName, exportList);
                }

                // Reset
                Dispatcher.InvokeAsync(new Action(() =>
                {
                    _TaskLookup.Values.ToList().ForEach(bar =>
                    {
                        bar.Value = 0;
                        bar.Visibility = Visibility.Hidden;
                    });
                    ImportClickBtn.IsEnabled = true;
                    ExportClickBtn1.IsEnabled = true;
                    ExportClickBtn2.IsEnabled = true;
                    _TaskLookup = new ConcurrentDictionary<int, ProgressBar>();
                }));

                // Refresh spell selection list on import
                if (isImport)
                {
                    Thread.Sleep(250);
                    Dispatcher.InvokeAsync(new Action(() =>
                    {
                        _ReloadData.Invoke();
                        _PopulateSelectSpell.Invoke();
                        Close();
                    }));
                }
            });
        }

        public ProgressBar LookupProgressBar(string bindingName, bool isImport)
        {
            var children = isImport ? ImportGridDbcs.Children : ExportGridDbcs.Children;
            for (int i = 0; i < children.Count; ++i)
            {
                if (children[i] is CheckBox box && box.Content.ToString().StartsWith(bindingName))
                {
                    return children[i + 1] as ProgressBar;
                }
            }
            return null;
        }

        public void SetProgress(double progress)
        {
            // Sending the id in the progress variable is a hack to get around the fact that I can't pass more than one variable to the callback
            // this happens because the callback is called from a different task
            int id = progress > 1 ? (int)progress : Task.CurrentId.GetValueOrDefault(0);
            progress = progress > 1 ? progress - id : progress;
            var bar = id == 0 ? null : _TaskLookup[id];
            if (bar != null)
            {
                int reportValue = Convert.ToInt32(progress * 100D);
                Dispatcher.InvokeAsync(new Action(() => bar.Value = reportValue));
            }
        }
        public void ShowFlyoutMessage(string message)
        {
            Dispatcher.InvokeAsync(new Action(() =>
            {
                Flyout.IsOpen = true;
                FlyoutText.Text = message;
            }));
        }

        private void TabClick(object sender, MouseButtonEventArgs e)
        {
            SizeToContent = SizeToContent.Width;
        }
    }
}
