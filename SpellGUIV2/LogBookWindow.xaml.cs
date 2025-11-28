using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using MahApps.Metro.Controls;

using NLog;

using SpellEditor.Sources.Controls.SpellSelectList;

namespace SpellEditor
{
    partial class LogBookWindow : MetroWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly int _MaxLogBookRecords = 9;

        private readonly Action<SpellLogRecord> _SelectSpellAction;

        private readonly ObservableCollection<SpellLogRecord> _logRecords = new ObservableCollection<SpellLogRecord>();

        private Window _main;

        public LogBookWindow(Action<SpellLogRecord> selectSpellAction)
        {
            InitializeComponent();
            _SelectSpellAction = selectSpellAction;
            LogBookList.ItemsSource = _logRecords;

            WindowStartupLocation = WindowStartupLocation.Manual;
            Height = 400;
            Width = 300;

            _main = Application.Current.MainWindow;
            PositionRelativeToMain();

            _main.LocationChanged += MainWindowMoved;
            Closed += LogBookWindow_Closed;
            IsVisibleChanged += LogBookWindow_IsVisibleChanged;

            Topmost = true;

            Owner = _main;
        }

        private void LogBookWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                // Window is now visible → position it and start following main window
                PositionRelativeToMain();
                _main.LocationChanged += MainWindowMoved;
            }
            else
            {
                // Window is hidden → stop following (prevents memory leak)
                _main.LocationChanged -= MainWindowMoved;
            }
        }

        private void LogBookWindow_Closed(object sender, EventArgs e)
        {
            _main.LocationChanged -= MainWindowMoved;
        }

        private void MainWindowMoved(object sender, EventArgs e)
        {
            PositionRelativeToMain();
        }

        private void PositionRelativeToMain()
        {
            Left = _main.Left + _main.Width - Width;
            Top = _main.Top + _main.Height - Height;
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
        }

        public void RecordLogEntry(SpellSelectionEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("LogBook: Expected StackPanel to be a SpellSelectionEntry, got null");

            if (!int.TryParse(MaxEntriesBox.Text, out int max))
                // If less than 0 then infinite
                max = max < 0 ? 
                    9999999 : 
                    // If more than 0 use the value
                    max > 0 ? 
                        max : 
                        // If 0, use default constant
                        _MaxLogBookRecords;

            // If already in the log book, bring it to the most recent by removing and readding
            var record = new SpellLogRecord
            {
                SpellLogName = entry.GetSpellText(),
                SpellLogIcon = entry.GetIconId().ToString(),
            };

            // check duplicates
            var selectedId = entry.GetSpellId();
            var existing = _logRecords.FirstOrDefault(r =>
            {
                var otherName = r.SpellLogName;
                return uint.Parse(otherName.Substring(1, otherName.IndexOf(' ', 1))) == selectedId;
            });
            if (existing != null)
            {
                _logRecords.Remove(existing);
            }

            _logRecords.Insert(0, record);

            while (_logRecords.Count > max)
                _logRecords.RemoveAt(_logRecords.Count - 1);
        }

        private void LogBookList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = e.AddedItems;
            if (items.Count == 0 || sender != LogBookList)
                return;
            if (items.Count > 1)
            {
                LogBookList.UnselectAll();
                return;
            }

            if (items[0] is SpellLogRecord selected)
                _SelectSpellAction.Invoke(selected);
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            _logRecords.Clear();
        }

        public void ShowFlyoutMessage(string message)
        {
            Dispatcher.InvokeAsync(new Action(() =>
            {
                Flyout.IsOpen = true;
                FlyoutText.Text = message;
            }));
        }
    }
}
