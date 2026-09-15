using NLog;
using SpellEditor.Sources.BLP;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SpellEditor.Sources.DBC
{
    class SpellIconDBC : AbstractDBC
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private MainWindow main;
        private IDatabaseAdapter adapter;

        private readonly Dictionary<uint, Icon_DBC_Lookup> _LookupsById = new Dictionary<uint, Icon_DBC_Lookup>();
        private bool _EntriesRequested;

        public List<Icon_DBC_Lookup> Lookups = new List<Icon_DBC_Lookup>();
        public readonly IconGalleryModel Gallery = new IconGalleryModel();

        public SpellIconDBC(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellIcon.dbc");
        }

        public override void LoadGraphicUserInterface()
        {
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                uint offset = (uint)record["Name"];
                if (offset == 0)
                    continue;
                string name = LookupStringOffset(offset);
                uint id = (uint)record["ID"];

                Icon_DBC_Lookup lookup;
                lookup.ID = id;
                lookup.Offset = offset;
                lookup.Name = name;
                Lookups.Add(lookup);
                _LookupsById[id] = lookup;
            }

            // In this DBC we don't actually need to keep the DBC data now that
            // we have extracted the lookup tables. Nulling it out may help with
            // memory consumption.
            CleanStringsMap();
            CleanBody();
        }

        public void LoadImages()
        {
            if (main.IconGrid == null)
                return;
            if (!ReferenceEquals(main.IconGrid.DataContext, Gallery))
                main.IconGrid.DataContext = Gallery;

            UpdateSelectedIcon();

            if (_EntriesRequested)
                return;
            _EntriesRequested = true;

            Task.Run(() =>
            {
                var entries = new List<IconEntry>(Lookups.Count);
                foreach (var lookup in Lookups)
                {
                    if (string.IsNullOrEmpty(lookup.Name) || !File.Exists(lookup.Name + ".blp"))
                        continue;
                    entries.Add(new IconEntry(lookup.ID, lookup.Offset, lookup.Name));
                }
                main.Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    Gallery.SetEntries(entries);
                    Logger.Info($"Loaded {entries.Count} icons into the icon gallery");
                }));
            });
        }

        public void SetIconSize(double newSize)
        {
            Gallery.ItemSize = newSize;
        }

        public void SetIconFilter(string filter)
        {
            Gallery.SetFilter(filter);
        }

        private void UpdateSelectedIcon()
        {
            if (adapter == null || main.selectedID == 0)
                return;

            Task.Run(() =>
            {
                var container = adapter.Query(string.Format("SELECT `SpellIconID`,`ActiveIconID` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID));
                if (container == null || container.Rows.Count == 0)
                {
                    return;
                }
                var res = container.Rows[0];
                uint iconInt = uint.Parse(res[0].ToString());
                var path = GetIconPath(iconInt);
                if (path.Length == 0)
                    return;
                // Update currently selected icon, we don't currently handle ActiveIconID
                var source = BlpManager.GetInstance().GetImageSourceFromBlpPath(path + ".blp");
                main.Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(
                    () => main.CurrentIcon.Source = source));
            });
        }

        public string GetIconPath(uint iconId)
        {
            return _LookupsById.TryGetValue(iconId, out var lookup) && lookup.Name != null ? lookup.Name : "";
        }

        public struct Icon_DBC_Lookup
        {
            public uint ID;
            public uint Offset;
            public string Name;
        }
    };
}
