using NLog;
using SpellEditor.Sources.Controls.ListPickerDialog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpellEditor.Sources.Emulator
{
    /// <summary>
    /// Form editor for one world table, scoped to the selected spell. Values are held as strings
    /// and parsed on save, changes are applied as one transaction.
    /// </summary>
    public partial class WorldTableEditor : UserControl
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>Above this many flags the list gets its own scroll bar.</summary>
        private const int InlineFlagLimit = 8;
        private const double FlagListMaxHeight = 170;
        private const double FieldWidth = 150;
        private const double FlagCheckBoxWidth = 235;
        /// <summary>Auto columns are measured unbounded, so wrapping needs an explicit width.</summary>
        private const double WideFieldWidth = 4 * (FlagCheckBoxWidth + 10) + 4;

        private static readonly Brush CardBorder = Frozen(Color.FromArgb(0x60, 0x80, 0x80, 0x80));

        private readonly MainWindow _mainWindow;
        private readonly Dictionary<string, List<EnumValue>> _enumSources =
            new Dictionary<string, List<EnumValue>>();
        /// <summary>Extra context worked out while loading, see DescribeRows.</summary>
        private readonly Dictionary<DataRow, string> _rowNotes = new Dictionary<DataRow, string>();
        /// <summary>Tick filter per flag column, so it survives the rebuild on every load.</summary>
        private readonly Dictionary<string, bool> _flagFilters = new Dictionary<string, bool>();

        private WorldDatabase _database;
        private DataTable _data;
        private RankChain _chain;
        private uint _spellId;
        private bool _busy;

        public WorldTable Table { get; }

        /// <summary>Raised after a save, the other tabs may no longer be showing the right rows.</summary>
        public event Action Saved;

        /// <summary>True once the fields hold the rows for the spell last asked for.</summary>
        public bool IsDataLoaded { get; private set; }

        public bool HasPendingChanges => _data != null && _data.GetChanges() != null;

        public WorldTableEditor(WorldTable table, MainWindow mainWindow)
        {
            InitializeComponent();

            Table = table;
            _mainWindow = mainWindow;

            RefreshLocalisation();
        }

        private static Brush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public void SetDatabase(WorldDatabase database)
        {
            _database = database;
            IsDataLoaded = false;
        }

        /// <summary>Reloads the next time the tab is opened, unsaved edits are left alone.</summary>
        public void Invalidate() => IsDataLoaded = false;

        /// <summary>Lets the integration report connection trouble on the tab in view.</summary>
        public void ShowMessage(string message, bool isError) => SetStatus(message, isError);

        /// <summary>Switches spell without loading, the rows are fetched when the tab is shown.</summary>
        public void SetSpell(uint spellId)
        {
            if (_spellId == spellId)
                return;
            _spellId = spellId;
            IsDataLoaded = false;
        }

        #region Localisation
        private static string Localise(string key, string fallback)
        {
            var resource = Application.Current?.TryFindResource(key) as string;
            return string.IsNullOrWhiteSpace(resource) ? fallback : resource.Trim();
        }

        /// <summary>Reapplies every label from the language files.</summary>
        public void RefreshLocalisation()
        {
            Table.ApplyLocalisedText();

            DescriptionText.Text = Table.Description;
            AddRowButton.Content = Localise("emu_add_row", "Add");
            SaveButton.Content = Localise("emu_save_changes", "Save Changes");
            RevertButton.Content = Localise("emu_revert", "Revert");
            ReloadButton.Content = Localise("emu_reload", "Reload");
            CopySqlButton.Content = Localise("emu_copy_sql", "Copy SQL");
            CopySqlButton.ToolTip = Localise("emu_copy_sql_tip",
                "Copy the statements this save would run to the clipboard instead of applying them");

            BuildEnumSources();
            BuildRows();
            UpdateRankText();
        }

        /// <summary>Keeps values the enum does not know about so a combo box never blanks one out.</summary>
        private void BuildEnumSources()
        {
            foreach (var column in Table.Columns.Where(entry => entry.Enum != null))
            {
                var values = column.Enum.Values.ToList();
                var known = new HashSet<string>(values.Select(entry => entry.Value));

                if (_data != null)
                {
                    foreach (DataRow row in _data.Rows)
                    {
                        if (row.RowState == DataRowState.Deleted)
                            continue;
                        var raw = row[column.Name] as string;
                        if (string.IsNullOrEmpty(raw) || !known.Add(raw))
                            continue;
                        values.Add(new EnumValue(ParseLongOrZero(raw),
                            Localise("emu_unknown_value", "Unknown")));
                    }
                }

                _enumSources[column.Name] = values;
            }
        }

        private static long ParseLongOrZero(string raw) =>
            long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
        #endregion

        #region Loading
        public async void Refresh(uint spellId, bool force = false)
        {
            if (_database == null || _busy)
                return;
            if (!force && IsDataLoaded && _spellId == spellId)
                return;

            _spellId = spellId;
            await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            if (_database == null)
                return;

            if (_spellId == 0)
            {
                _data = null;
                _chain = null;
                _rowNotes.Clear();
                IsDataLoaded = false;
                BuildRows();
                UpdateRankText();
                SetStatus(Localise("emu_no_spell_selected", "Pick a spell to see its data."), false);
                return;
            }

            _busy = true;
            SetStatus(Localise("emu_loading", "Loading..."), false);
            try
            {
                // The chain decides which rows the core would apply, so resolve it first
                var spellId = _spellId;
                _chain = await System.Threading.Tasks.Task.Run(() => _database.GetRankChain(spellId));

                var columns = string.Join(", ", Table.Columns.Select(column => $"`{column.Name}`"));
                var sql = $"SELECT {columns} FROM `{Table.Name}` " +
                    $"WHERE {Table.SpellFilter(_chain)} ORDER BY {Table.OrderBy}";

                var raw = await _database.QueryAsync(sql);
                _data = ToStringTable(raw);

                _rowNotes.Clear();
                IReadOnlyList<string> notes = null;
                if (_data.Rows.Count > 0)
                {
                    var loaded = _data;
                    notes = await System.Threading.Tasks.Task.Run(() => Table.DescribeRows(_database, loaded));
                }
                for (var i = 0; i < _data.Rows.Count; ++i)
                {
                    var row = _data.Rows[i];
                    var extra = notes != null && i < notes.Count ? notes[i] : string.Empty;
                    _rowNotes[row] = Join(DescribeInheritedRow(row), extra);
                }

                BuildEnumSources();
                BuildRows();
                UpdateRankText();
                IsDataLoaded = true;

                // The status line is kept for what the rows cannot say themselves
                SetStatus(string.Empty, false);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, $"Failed to load {Table.Name}");
                _data = null;
                _rowNotes.Clear();
                IsDataLoaded = false;
                BuildRows();
                UpdateRankText();
                SetStatus(exception.Message, true);
            }
            finally
            {
                _busy = false;
            }
        }

        private DataTable ToStringTable(DataTable raw)
        {
            var table = new DataTable(Table.Name);
            foreach (var column in Table.Columns)
                table.Columns.Add(column.Name, typeof(string));

            // Owned rows first, inherited ones after. OrderBy is stable, so the database sort holds
            // within each group.
            var spellColumn = Table.PrimarySpellColumn;
            var ordered = raw.Rows.Cast<DataRow>();
            if (spellColumn != null && Table.InheritsRankChain)
                ordered = ordered.OrderBy(source => IsOwnedBySelectedSpell(ToDisplayString(source[spellColumn.Name])) ? 0 : 1);

            foreach (var source in ordered)
            {
                var row = table.NewRow();
                foreach (var column in Table.Columns)
                    row[column.Name] = ToDisplayString(source[column.Name]);
                table.Rows.Add(row);
            }

            table.AcceptChanges();
            return table;
        }

        /// <summary>True when the row is written against the spell on screen.</summary>
        private bool IsOwnedBySelectedSpell(string rawSpellId) =>
            (uint)AbsoluteOf(rawSpellId) == _spellId;

        private bool OwnsRow(DataRow row)
        {
            var column = Table.PrimarySpellColumn;
            if (column == null || _spellId == 0)
                return true;
            var version = row.RowState == DataRowState.Deleted ? DataRowVersion.Original : DataRowVersion.Default;
            return IsOwnedBySelectedSpell(row[column.Name, version] as string);
        }

        /// <summary>Says which other rank an inherited row came from.</summary>
        private string DescribeInheritedRow(DataRow row)
        {
            var column = Table.PrimarySpellColumn;
            if (!Table.InheritsRankChain || column == null || OwnsRow(row))
                return string.Empty;

            var owner = (uint)AbsoluteOf(row[column.Name] as string);
            var rank = _chain?.RankOf(owner) ?? 0;
            var label = DescribeValue(row[column.Name] as string);

            return rank == 0
                ? string.Format(Localise("emu_row_from_other_spell",
                    "Written against {0}, and the core applies it to this spell as well."), label)
                : string.Format(Localise("emu_row_from_rank",
                    "Written against rank {0}, {1}. The core applies it to every rank in the chain, including this one."),
                    rank, label);
        }

        /// <summary>The chain line above the fields, worded per table since only some inherit.</summary>
        private void UpdateRankText()
        {
            if (RankText == null)
                return;

            // This tab is the chain, saying it again above the rows adds nothing
            if (_chain == null || !_chain.HasChain || !Table.ShowsRankChainSummary)
            {
                RankText.Visibility = Visibility.Collapsed;
                return;
            }

            var first = DescribeValue(_chain.FirstSpellId.ToString(CultureInfo.InvariantCulture));
            if (!Table.InheritsRankChain)
            {
                RankText.Text = string.Format(Localise("emu_rank_chain_own",
                    "Rank {0} of {1}, chain starting at {2}. This table is not shared between ranks, each rank needs its own rows."),
                    _chain.Rank, _chain.Ranks.Count, first);
            }
            else if (_chain.IsFirstRank)
            {
                RankText.Text = string.Format(Localise("emu_rank_chain_first",
                    "Rank {0} of {1}, and the first rank of the chain. Rows written against it can cover every rank."),
                    _chain.Rank, _chain.Ranks.Count);
            }
            else
            {
                RankText.Text = string.Format(Localise("emu_rank_chain_shared",
                    "Rank {0} of {1}. Rows written against rank 1, {2}, count for this rank too and are listed below."),
                    _chain.Rank, _chain.Ranks.Count, first);
            }
            RankText.Visibility = Visibility.Visible;
        }

        private static string Join(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first))
                return second ?? string.Empty;
            if (string.IsNullOrWhiteSpace(second))
                return first;
            return first + " " + second;
        }

        private static string ToDisplayString(object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;
            // Invariant culture keeps the decimal point a dot, which is what MySQL wants
            return value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value.ToString();
        }

        /// <summary>A negative id means different things per table, but always points at the ABS.</summary>
        private string DescribeSpell(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) ||
                !int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ||
                id == 0)
                return string.Empty;

            // Some tables read a negative id as something other than a spell, such as a nested group
            if (id < 0)
            {
                var described = Table.DescribeNegativeSpellId(id);
                if (described != null)
                    return described;
            }

            var name = _mainWindow?.GetSpellNameById((uint)Math.Abs(id));
            return string.IsNullOrEmpty(name) ? Localise("emu_unknown_spell", "Not in Spell.dbc") : name;
        }
        #endregion

        #region Building the fields
        /// <summary>Lays out one card per row from scratch, on every load, add, remove and language switch.</summary>
        private void BuildRows()
        {
            if (RowsPanel == null)
                return;

            RowsPanel.Children.Clear();

            var live = LiveRows();
            // A single row table still allows a row when the only one on screen was inherited
            AddRowButton.IsEnabled = _data != null && (!Table.IsSingleRow || !live.Any(OwnsRow));

            if (_data == null)
                return;

            if (live.Count == 0)
            {
                RowsPanel.Children.Add(new TextBlock
                {
                    Text = Table.EmptyMessage,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = WideFieldWidth,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Opacity = 0.7,
                    Margin = new Thickness(5, 10, 5, 5)
                });
                return;
            }

            foreach (var row in live)
                RowsPanel.Children.Add(BuildCard(row));
        }

        private List<DataRow> LiveRows() => _data == null
            ? new List<DataRow>()
            : _data.Rows.Cast<DataRow>().Where(row => row.RowState != DataRowState.Deleted).ToList();

        private Border BuildCard(DataRow row)
        {
            var content = new StackPanel();
            content.Children.Add(BuildFieldGrid(row));

            if (_rowNotes.TryGetValue(row, out var note) && !string.IsNullOrWhiteSpace(note))
            {
                content.Children.Add(new TextBlock
                {
                    Text = note,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = WideFieldWidth,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Opacity = 0.7,
                    Margin = new Thickness(5, 0, 5, 5)
                });
            }

            var remove = new Button
            {
                Content = Localise("emu_delete_row", "Remove"),
                MinWidth = 90,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            remove.Click += (sender, args) => RemoveRow(row);
            content.Children.Add(remove);

            return new Border
            {
                BorderBrush = CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(4),
                Child = content
            };
        }

        /// <summary>Two fields per line like the Base tab, wide ones take a line of their own.</summary>
        private Grid BuildFieldGrid(DataRow row)
        {
            var grid = new Grid();
            for (var i = 0; i < 4; ++i)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var gridRow = 0;
            var pair = 0;

            void NewLine()
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                ++gridRow;
                pair = 0;
            }

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            foreach (var column in Table.Columns)
            {
                var control = BuildControl(column, row, out var fullWidth);
                var label = new Label
                {
                    Content = column.Label ?? column.Name,
                    Margin = new Thickness(5),
                    ToolTip = column.Tooltip,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (fullWidth)
                {
                    if (pair != 0)
                        NewLine();

                    label.VerticalAlignment = VerticalAlignment.Top;
                    Grid.SetRow(label, gridRow);
                    Grid.SetColumn(label, 0);
                    Grid.SetRow(control, gridRow);
                    Grid.SetColumn(control, 1);
                    Grid.SetColumnSpan(control, 3);
                    grid.Children.Add(label);
                    grid.Children.Add(control);

                    NewLine();
                    continue;
                }

                Grid.SetRow(label, gridRow);
                Grid.SetColumn(label, pair * 2);
                Grid.SetRow(control, gridRow);
                Grid.SetColumn(control, pair * 2 + 1);
                grid.Children.Add(label);
                grid.Children.Add(control);

                if (++pair == 2)
                    NewLine();
            }

            return grid;
        }

        private FrameworkElement BuildControl(WorldColumn column, DataRow row, out bool fullWidth)
        {
            fullWidth = false;

            if (column.IsPrimarySpellKey)
            {
                // On its own line so it cannot stretch the columns the other fields line up in
                fullWidth = true;
                return BuildPrimarySpellField(column, row);
            }

            if (column.Flags != null)
            {
                fullWidth = true;
                return BuildFlagField(column, row);
            }

            if (column.Enum != null)
                return BuildEnumField(column, row);

            if (column.ShowSpellPicker)
            {
                fullWidth = true;
                return BuildSpellPickerField(column, row);
            }

            fullWidth = column.IsMultiline;
            return BuildTextField(column, row);
        }

        /// <summary>Never typed in, it follows the open spell. Only the sign is up to the user.</summary>
        private FrameworkElement BuildPrimarySpellField(WorldColumn column, DataRow row)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock
            {
                Text = DescribeValue(row[column.Name] as string),
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = column.Tooltip
            });

            if (column.HasNegativeOption)
            {
                var id = AbsoluteOf(row[column.Name] as string);
                var box = new CheckBox
                {
                    Content = column.NegativeOption,
                    Margin = new Thickness(10, 5, 5, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    IsChecked = SignOf(row[column.Name] as string) < 0,
                    ToolTip = column.Tooltip
                };
                box.Checked += (sender, args) => row[column.Name] = (-id).ToString(CultureInfo.InvariantCulture);
                box.Unchecked += (sender, args) => row[column.Name] = id.ToString(CultureInfo.InvariantCulture);
                panel.Children.Add(box);
            }

            return panel;
        }

        private string DescribeValue(string raw)
        {
            var id = AbsoluteOf(raw);
            var name = DescribeSpell(id.ToString(CultureInfo.InvariantCulture));
            return string.IsNullOrEmpty(name) ? id.ToString(CultureInfo.InvariantCulture) : $"{id} - {name}";
        }

        private static int AbsoluteOf(string raw) =>
            int.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? Math.Abs(value)
                : 0;

        private static int SignOf(string raw) =>
            int.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? Math.Sign(value)
                : 0;

        /// <summary>One tick box per bit like the Attributes tab, plus a raw box for unnamed bits.</summary>
        private FrameworkElement BuildFlagField(WorldColumn column, DataRow row)
        {
            var flags = column.Flags.Flags.Where(flag => flag.Value != 0).ToList();
            var boxes = new List<CheckBox>();
            var raw = new TextBox
            {
                Width = 90,
                Margin = new Thickness(5, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right
            };
            var updating = false;

            WorldEnums.TryParseUInt(row[column.Name] as string, out var current);

            void Write(uint value)
            {
                row[column.Name] = value.ToString(CultureInfo.InvariantCulture);
                updating = true;
                foreach (var box in boxes)
                {
                    var bit = (uint)box.Tag;
                    box.IsChecked = (value & bit) == bit;
                }
                raw.Text = value.ToString(CultureInfo.InvariantCulture);
                updating = false;
            }

            var list = new WrapPanel { Orientation = Orientation.Horizontal, MaxWidth = WideFieldWidth };
            foreach (var flag in flags)
            {
                var box = new CheckBox
                {
                    Content = flag.Name,
                    Tag = flag.Value,
                    Width = FlagCheckBoxWidth,
                    Margin = new Thickness(5),
                    IsChecked = (current & flag.Value) == flag.Value,
                    ToolTip = $"0x{flag.Value:X}"
                };
                void Toggled(object sender, RoutedEventArgs args)
                {
                    if (updating)
                        return;
                    WorldEnums.TryParseUInt(row[column.Name] as string, out var value);
                    var bit = (uint)((CheckBox)sender).Tag;
                    Write(((CheckBox)sender).IsChecked == true ? value | bit : value & ~bit);
                }
                box.Checked += Toggled;
                box.Unchecked += Toggled;
                boxes.Add(box);
                list.Children.Add(box);
            }

            FrameworkElement listHost = list;
            if (flags.Count > InlineFlagLimit)
            {
                listHost = new ScrollViewer
                {
                    Content = list,
                    MaxHeight = FlagListMaxHeight,
                    MaxWidth = WideFieldWidth,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                };
            }

            raw.Text = current.ToString(CultureInfo.InvariantCulture);
            raw.TextChanged += (sender, args) =>
            {
                if (updating)
                    return;
                if (WorldEnums.TryParseUInt(raw.Text, out var value))
                    Write(value);
            };

            // Only reapplied when the button is pressed, a box must not vanish from under the
            // pointer the moment it is unchecked
            var checkedOnly = new CheckBox
            {
                IsChecked = _flagFilters.TryGetValue(column.Name, out var filtered) && filtered,
                Content = Localise("emu_checked_only", "Only checked"),
                ToolTip = Localise("emu_checked_only_tip", "Show only the flags that are checked"),
                Margin = new Thickness(5, 0, 5, 3),
                VerticalAlignment = VerticalAlignment.Center
            };
            void ApplyCheckedFilter()
            {
                var only = checkedOnly.IsChecked == true;
                _flagFilters[column.Name] = only;
                foreach (var box in boxes)
                    box.Visibility = !only || box.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            }
            checkedOnly.Checked += (sender, args) => ApplyCheckedFilter();
            checkedOnly.Unchecked += (sender, args) => ApplyCheckedFilter();
            ApplyCheckedFilter();

            var footer = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 5) };
            footer.Children.Add(new TextBlock
            {
                Text = Localise("emu_raw_value", "Raw value"),
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.7
            });
            footer.Children.Add(raw);

            var header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(3, 0, 0, 0)
            };
            header.Children.Add(checkedOnly);

            var panel = new StackPanel { Margin = new Thickness(0, 3, 0, 0) };
            panel.Children.Add(header);
            panel.Children.Add(listHost);
            panel.Children.Add(footer);
            return panel;
        }

        private FrameworkElement BuildEnumField(WorldColumn column, DataRow row)
        {
            var combo = new ComboBox
            {
                ItemsSource = _enumSources[column.Name],
                DisplayMemberPath = "Label",
                SelectedValuePath = "Value",
                SelectedValue = row[column.Name] as string,
                Width = 220,
                Margin = new Thickness(5),
                ToolTip = column.Tooltip
            };
            combo.SelectionChanged += (sender, args) =>
            {
                if (combo.SelectedValue is string value)
                    row[column.Name] = value;
            };
            return combo;
        }

        /// <summary>Another spell the row points at, typeable but with a picker and its name.</summary>
        private FrameworkElement BuildSpellPickerField(WorldColumn column, DataRow row)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var box = new TextBox
            {
                Text = AbsoluteOf(row[column.Name] as string).ToString(CultureInfo.InvariantCulture),
                Width = 90,
                Margin = new Thickness(5),
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = column.Tooltip
            };
            var name = new TextBlock
            {
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.75,
                MaxWidth = 260,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            CheckBox negative = null;

            void Write()
            {
                var id = AbsoluteOf(box.Text);
                var signed = negative?.IsChecked == true ? -id : id;
                row[column.Name] = signed.ToString(CultureInfo.InvariantCulture);
                name.Text = DescribeSpell(id.ToString(CultureInfo.InvariantCulture));
                name.ToolTip = name.Text;
            }

            box.TextChanged += (sender, args) => Write();

            var pick = new Button
            {
                Content = "...",
                Width = 30,
                Margin = new Thickness(0, 5, 5, 5),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = Localise("emu_pick_spell", "Select spell")
            };
            pick.Click += (sender, args) =>
            {
                if (_mainWindow == null)
                    return;
                var dialog = new SpellPickerDialog(_mainWindow, (uint)AbsoluteOf(box.Text),
                    Localise("emu_pick_spell", "Select spell"));
                if (dialog.ShowDialog() == true)
                    box.Text = dialog.SelectedId.ToString(CultureInfo.InvariantCulture);
            };

            panel.Children.Add(box);
            panel.Children.Add(pick);

            if (column.HasNegativeOption)
            {
                negative = new CheckBox
                {
                    Content = column.NegativeOption,
                    Margin = new Thickness(5),
                    VerticalAlignment = VerticalAlignment.Center,
                    IsChecked = SignOf(row[column.Name] as string) < 0,
                    ToolTip = column.Tooltip
                };
                negative.Checked += (sender, args) => Write();
                negative.Unchecked += (sender, args) => Write();
                panel.Children.Add(negative);
            }

            name.Text = DescribeSpell(AbsoluteOf(row[column.Name] as string).ToString(CultureInfo.InvariantCulture));
            name.ToolTip = name.Text;
            panel.Children.Add(name);
            return panel;
        }

        private FrameworkElement BuildTextField(WorldColumn column, DataRow row)
        {
            var box = new TextBox
            {
                Text = row[column.Name] as string ?? string.Empty,
                Margin = new Thickness(5),
                ToolTip = column.Tooltip,
                TextAlignment = column.IsNumeric ? TextAlignment.Right : TextAlignment.Left
            };

            if (column.IsMultiline)
            {
                box.AcceptsReturn = true;
                box.TextWrapping = TextWrapping.Wrap;
                box.Height = 46;
                box.Width = 520;
                box.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else
            {
                box.Width = column.IsNumeric ? FieldWidth : 260;
            }

            box.TextChanged += (sender, args) => row[column.Name] = box.Text;
            return box;
        }
        #endregion

        #region Saving
        /// <summary>Builds the statements from the row states. False and a message on a bad value.</summary>
        private bool TryBuildStatements(out List<string> statements, out string error)
        {
            statements = new List<string>();
            error = null;

            if (_data == null)
                return true;

            var rowNumber = 0;
            foreach (DataRow row in _data.Rows)
            {
                ++rowNumber;
                switch (row.RowState)
                {
                    case DataRowState.Added:
                        if (!TryBuildInsert(row, rowNumber, statements, out error))
                            return false;
                        break;
                    case DataRowState.Modified:
                        if (!TryBuildUpdate(row, rowNumber, statements, out error))
                            return false;
                        break;
                    case DataRowState.Deleted:
                        statements.Add($"DELETE FROM `{Table.Name}` WHERE {KeyWhere(row, DataRowVersion.Original)};");
                        break;
                }
            }
            return true;
        }

        private bool TryBuildInsert(DataRow row, int rowNumber, List<string> statements, out string error)
        {
            var values = new List<string>();
            foreach (var column in Table.Columns)
            {
                if (!TryFormat(column, row[column.Name] as string, rowNumber, out var value, out error))
                    return false;
                values.Add(value);
            }

            var names = string.Join(", ", Table.Columns.Select(column => $"`{column.Name}`"));
            statements.Add($"INSERT INTO `{Table.Name}` ({names}) VALUES ({string.Join(", ", values)});");
            error = null;
            return true;
        }

        private bool TryBuildUpdate(DataRow row, int rowNumber, List<string> statements, out string error)
        {
            var assignments = new List<string>();
            foreach (var column in Table.Columns)
            {
                if (!TryFormat(column, row[column.Name] as string, rowNumber, out var value, out error))
                    return false;
                assignments.Add($"`{column.Name}` = {value}");
            }

            statements.Add($"UPDATE `{Table.Name}` SET {string.Join(", ", assignments)} " +
                $"WHERE {KeyWhere(row, DataRowVersion.Original)};");
            error = null;
            return true;
        }

        /// <summary>Identifies a row by its key columns as loaded, spell_linked_spell has no PK.</summary>
        private string KeyWhere(DataRow row, DataRowVersion version)
        {
            var parts = new List<string>();
            foreach (var column in Table.KeyColumns)
            {
                var raw = row[column.Name, version] as string;
                if (string.IsNullOrEmpty(raw))
                {
                    parts.Add($"`{column.Name}` IS NULL");
                    continue;
                }
                parts.Add(column.Kind == ColumnKind.Text
                    ? $"`{column.Name}` = '{WorldDatabase.Escape(raw)}'"
                    : $"`{column.Name}` = {raw.Trim()}");
            }
            return string.Join(" AND ", parts);
        }

        private bool TryFormat(WorldColumn column, string raw, int rowNumber, out string value, out string error)
        {
            value = null;
            error = null;
            raw = (raw ?? string.Empty).Trim();

            if (raw.Length == 0)
            {
                if (column.IsKey && column.Kind != ColumnKind.Text)
                {
                    error = Fail(column, rowNumber, Localise("emu_error_key_empty", "this field cannot be empty"));
                    return false;
                }
                if (column.AllowNull)
                {
                    value = "NULL";
                    return true;
                }
                raw = column.DefaultValue;
            }

            switch (column.Kind)
            {
                case ColumnKind.Text:
                    value = $"'{WorldDatabase.Escape(raw)}'";
                    return true;

                case ColumnKind.Float:
                    if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    {
                        error = Fail(column, rowNumber, Localise("emu_error_not_float", "expected a number such as 0.15"));
                        return false;
                    }
                    value = number.ToString("R", CultureInfo.InvariantCulture);
                    return true;

                case ColumnKind.Int:
                    if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var signed))
                    {
                        error = Fail(column, rowNumber, Localise("emu_error_not_int", "expected a whole number"));
                        return false;
                    }
                    value = signed.ToString(CultureInfo.InvariantCulture);
                    return true;

                case ColumnKind.ULong:
                    if (!WorldEnums.TryParseULong(raw, out var wide))
                    {
                        error = Fail(column, rowNumber, Localise("emu_error_not_uint",
                            "expected a positive whole number, or a mask written as 0x1F"));
                        return false;
                    }
                    value = wide.ToString(CultureInfo.InvariantCulture);
                    return true;

                default:
                    if (!WorldEnums.TryParseUInt(raw, out var unsigned))
                    {
                        error = Fail(column, rowNumber, Localise("emu_error_not_uint",
                            "expected a positive whole number, or a mask written as 0x1F"));
                        return false;
                    }
                    var rejected = Table.Validate(column, unsigned);
                    if (rejected != null)
                    {
                        error = Fail(column, rowNumber, rejected);
                        return false;
                    }
                    value = unsigned.ToString(CultureInfo.InvariantCulture);
                    return true;
            }
        }

        private string Fail(WorldColumn column, int rowNumber, string reason) =>
            string.Format(Localise("emu_error_cell", "Row {0}, {1}: {2}"), rowNumber,
                column.Label ?? column.Name, reason);
        #endregion

        #region Handlers
        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (_data == null || _database == null)
                return;

            var context = new NewRowContext
            {
                Database = _database,
                Chain = _chain ?? new RankChain(_spellId),
                Rows = LiveRows()
            };

            var row = _data.NewRow();
            foreach (var column in Table.Columns)
            {
                if (column.NewRowValue != null)
                    row[column.Name] = column.NewRowValue(context);
                else if (column.IsPrimarySpellKey && _spellId != 0)
                    row[column.Name] = _spellId.ToString(CultureInfo.InvariantCulture);
                else
                    row[column.Name] = column.DefaultValue;
            }
            _data.Rows.Add(row);

            BuildRows();
            SetStatus(Localise("emu_row_added", "Added. It is not in the database until you save."), false);
        }

        private void RemoveRow(DataRow row)
        {
            if (_data == null)
                return;

            row.Delete();
            BuildRows();
            SetStatus(Localise("emu_row_removed", "Marked for removal. Save to apply."), false);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_data == null || _database == null || _busy)
                return;

            if (!TryBuildStatements(out var statements, out var error))
            {
                SetStatus(error, true);
                return;
            }
            if (statements.Count == 0)
            {
                SetStatus(Localise("emu_nothing_to_save", "Nothing has changed."), false);
                return;
            }

            _busy = true;
            SetStatus(Localise("emu_saving", "Saving..."), false);
            try
            {
                await _database.ExecuteTransactionAsync(statements);
                // A save can move data between ranks, so no other tab can trust what it has
                _database.ClearRankChains();
                SetStatus(string.Format(Localise("emu_saved",
                    "Saved {0} change(s). Reload the server or run .reload for it to take effect."),
                    statements.Count), false);
                _busy = false;
                await LoadAsync();
                Saved?.Invoke();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, $"Failed to save {Table.Name}");
                SetStatus(exception.Message, true);
                _busy = false;
            }
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            if (_data == null)
                return;
            _data.RejectChanges();
            BuildRows();
            SetStatus(Localise("emu_reverted", "Unsaved changes discarded."), false);
        }

        private async void Reload_Click(object sender, RoutedEventArgs e) => await LoadAsync();

        private void CopySql_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildStatements(out var statements, out var error))
            {
                SetStatus(error, true);
                return;
            }
            if (statements.Count == 0)
            {
                SetStatus(Localise("emu_nothing_to_save", "Nothing has changed."), false);
                return;
            }

            try
            {
                Clipboard.SetText(string.Join(Environment.NewLine, statements));
                SetStatus(string.Format(Localise("emu_sql_copied",
                    "{0} statement(s) copied to the clipboard."), statements.Count), false);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, true);
            }
        }

        private void SetStatus(string message, bool isError)
        {
            StatusText.Text = message ?? string.Empty;
            if (isError)
                StatusText.Foreground = Brushes.OrangeRed;
            else
                StatusText.ClearValue(TextBlock.ForegroundProperty);
        }
        #endregion
    }
}
