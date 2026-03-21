using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

using MahApps.Metro.Controls;

using NLog;

using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Config;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Tools.MPQ;
using Binding = SpellEditor.Sources.Binding.Binding;

namespace SpellEditor
{
    partial class ImportExportWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private ConcurrentDictionary<int, ProgressBar> _TaskLookup;
        private string _MpqArchiveName;
        private Action _PopulateSelectSpell;
        private Action<Action<string>> _ReloadData;
        private readonly string _SpellBindingName = "Spell";

        public ImportExportWindow(IDatabaseAdapter adapter, Action populateSelectSpell, Action<Action<string>> reloadData)
        {
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
            CustomMpqCopyDirectoryTxt.Text = Config.CustomMpqCopyDirectory ?? string.Empty;
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
                int numRows;
                using (var adapter = AdapterFactory.Instance.GetAdapter(false))
                {
                    numRows = binding.GetNumRowsInTable(adapter);
                }
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
            archiveName = archiveName.ToLower().EndsWith(".mpq") ? archiveName : archiveName + ".MPQ";
            _MpqArchiveName = archiveName;
            Config.DefaultMpqName = archiveName;
            Config.CustomMpqCopyDirectory = CustomMpqCopyDirectoryTxt.Text?.Trim() ?? string.Empty;
            ClickHandler(false);
        }

        private void ImportClick(object sender, RoutedEventArgs e) => ClickHandler(true);
        private void ExportClick(object sender, RoutedEventArgs e)
        {
            // Ensure plain export does not trigger MPQ creation.
            _MpqArchiveName = string.Empty;
            ClickHandler(false);
        }
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
            // Capture MPQ target name for this run to avoid cross-click state bleed.
            var mpqArchiveName = _MpqArchiveName;
            var isMpqExportRun = !isImport && !string.IsNullOrEmpty(mpqArchiveName);
            var selectedBindingsForRun = new List<string>(bindingList);
            var barLookup = new Dictionary<string, ProgressBar>();
            bindingList.ForEach(bindingName => barLookup.Add(bindingName, LookupProgressBar(bindingName, isImport)));

            var label = isImport ? ImportLoadedCount : ExportLoadedCount;

            Task.Run(() =>
            {
                try
                {
                    var bag = new ConcurrentBag<Task>();
                    var adapters = new List<IDatabaseAdapter>();
                    bool isSpellImport = bindingList.Contains(_SpellBindingName);
                    var adapterIndex = isSpellImport ? 1 : 0;

                    // Start spell export immediately if it is selected
                    if (bindingList.Contains(_SpellBindingName))
                    {
                        bindingList.Remove(_SpellBindingName);
                        // Load data
                        var abstractDbc = GetDBC(_SpellBindingName, isImport);
                        var adapter = AdapterFactory.Instance.GetAdapter(false);
                        StartImportExport(abstractDbc, adapter, _SpellBindingName, isImport, ref bag, ref barLookup, useType);
                        adapters.Add(adapter);
                    }

                    // Spawn adapters
                    SpawnAdapters(ref adapters, bindingList.Count);

                    // Start tasks
                    bindingList.AsParallel().ForAll(bindingName =>
                    {
                        try
                        {
                            // Load data
                            var abstractDbc = GetDBC(bindingName, isImport);

                            // Get adapter
                            var adapter = adapters[adapterIndex];
                            if (++adapterIndex >= adapters.Count)
                            {
                                adapterIndex = isSpellImport ? 1 : 0;
                            }

                            // Perform operation
                            StartImportExport(abstractDbc, adapter, bindingName, isImport, ref bag, ref barLookup, useType);
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

                    adapters.ForEach(adapter => adapter.Dispose());
                    adapters.Clear();

                    // Create MPQ if required
                    if (isMpqExportRun)
                    {
                        var exportList = selectedBindingsForRun
                            .Select(bindingName => Path.Combine("Export", bindingName + ".dbc"))
                            .Where(File.Exists)
                            .ToList();

                        var mpqExport = new MpqExport();
                        mpqExport.CreateMpqFromDbcFileList(mpqArchiveName, exportList);

                        var customCopyDirectory = Config.CustomMpqCopyDirectory?.Trim();
                        if (!string.IsNullOrEmpty(customCopyDirectory))
                        {
                            if (!Directory.Exists(customCopyDirectory))
                            {
                                ShowFlyoutMessage($"Custom MPQ copy folder does not exist: {customCopyDirectory}");
                            }
                            else
                            {
                                var sourceMpqPath = Path.Combine("Export", mpqArchiveName);
                                var destinationMpqPath = Path.Combine(customCopyDirectory, mpqArchiveName);
                                try
                                {
                                    File.Copy(sourceMpqPath, destinationMpqPath, true);
                                }
                                catch (Exception exception)
                                {
                                    Logger.Info($"ERROR: Failed to copy MPQ to custom folder {customCopyDirectory}: {exception.Message}\n{exception}\n{exception.InnerException}");
                                    ShowFlyoutMessage($"Failed to copy MPQ to custom folder: {customCopyDirectory}");
                                }
                            }
                        }

                        // MPQ mode should not leave DBC artifacts behind.
                        foreach (var exportedDbc in exportList)
                        {
                            try
                            {
                                if (File.Exists(exportedDbc))
                                {
                                    File.Delete(exportedDbc);
                                }
                            }
                            catch (Exception exception)
                            {
                                Logger.Info($"ERROR: Failed to clean exported DBC artifact {exportedDbc}: {exception.Message}\n{exception}\n{exception.InnerException}");
                            }
                        }
                    }
                    // Reset
                    Dispatcher.InvokeAsync(new Action(() =>
                    {
                        try
                        {
                            // Always re-enable buttons even if some progress bars are missing.
                            ImportClickBtn.IsEnabled = true;
                            ExportClickBtn1.IsEnabled = true;
                            ExportClickBtn2.IsEnabled = true;

                            _TaskLookup.Values.ToList().ForEach(bar =>
                            {
                                if (bar == null)
                                    return;
                                bar.Value = 0;
                                bar.Visibility = Visibility.Hidden;
                            });
                            _TaskLookup = new ConcurrentDictionary<int, ProgressBar>();
                        }
                        catch (Exception exception)
                        {
                            Logger.Info($"ERROR: Failed to reset import/export UI state: {exception.Message}\n{exception}\n{exception.InnerException}");
                        }
                    }));

                    // Refresh spell selection list on import
                    if (isImport)
                    {
                        Dispatcher.InvokeAsync(new Action(() =>
                        {
                            _ReloadData.Invoke(null);
                            _PopulateSelectSpell.Invoke();
                            Close();
                        }));
                    }
                }
                catch (Exception exception)
                {
                    Logger.Info($"ERROR: Import/Export worker failed: {exception.Message}\n{exception}\n{exception.InnerException}");
                    ShowFlyoutMessage("Import/Export failed. Check log output for details.");
                    Dispatcher.InvokeAsync(new Action(() =>
                    {
                        ImportClickBtn.IsEnabled = true;
                        ExportClickBtn1.IsEnabled = true;
                        ExportClickBtn2.IsEnabled = true;
                    }));
                }
            });
        }

        private AbstractDBC GetDBC(string bindingName, bool isImport)
        {
            var manager = DBCManager.GetInstance();
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
                    return null;
                }
            }
            if (isImport && !abstractDbc.HasData())
                abstractDbc.ReloadContents();
            return abstractDbc;
        }

        private Task StartImportExport(AbstractDBC dbc, IDatabaseAdapter adapter, string bindingName, bool isImport, ref ConcurrentBag<Task> bag, ref Dictionary<string, ProgressBar> barLookup, ImportExportType useType)
        {
            var task = isImport ?
                dbc.ImportTo(adapter, SetProgress, "ID", bindingName, useType) :
                dbc.ExportTo(adapter, SetProgress, "ID", bindingName, useType);

            _TaskLookup.TryAdd(task.Id, barLookup[bindingName]);
            bag.Add(task);

            return task;
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

        private void SpawnAdapters(ref List<IDatabaseAdapter> adapters, int numBindings)
        {
            var tasks = new List<Task<IDatabaseAdapter>>();
            int numConnections = Math.Max(numBindings >= 4 ? 2 : 1, numBindings / 10);
            Logger.Info($"Spawning {numConnections} adapters...");
            var timer = new Stopwatch();
            timer.Start();
            for (var i = 0; i < numConnections; ++i)
            {
                tasks.Add(Task.Run(() =>
                {
                    var adapter = AdapterFactory.Instance.GetAdapter(false);
                    Logger.Info($"Spawned Adapter{Task.CurrentId}");
                    return adapter;
                }));
            }
            Task.WaitAll(tasks.ToArray());
            foreach (var task in tasks)
            {
                adapters.Add(task.Result);
            }
            timer.Stop();
            Logger.Info($"Spawned {numConnections} adapters in {Math.Round(timer.Elapsed.TotalSeconds, 2)} seconds.");
        }

        public void SetProgress(double progress, int taskIdOverride = 0)
        {
            int id = taskIdOverride > 0 ? taskIdOverride : Task.CurrentId.GetValueOrDefault(0);
            var bar = id > 0 ? _TaskLookup.ContainsKey(id) ? _TaskLookup[id] : null : null;
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

        private void CustomMpqCopyBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                var currentPath = CustomMpqCopyDirectoryTxt.Text?.Trim();
                if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                {
                    dialog.SelectedPath = currentPath;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    CustomMpqCopyDirectoryTxt.Text = dialog.SelectedPath;
                    Config.CustomMpqCopyDirectory = dialog.SelectedPath;
                }
            }
        }

        private void CustomMpqCopyDirectoryTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            Config.CustomMpqCopyDirectory = CustomMpqCopyDirectoryTxt.Text?.Trim() ?? string.Empty;
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SizeToContent = SizeToContent.Width;
        }
    }
}
