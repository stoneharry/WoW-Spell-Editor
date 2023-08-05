using SpellEditor.Sources.Database;
using SpellEditor.Sources.VersionControl;

namespace SpellEditor.Sources.Locale
{
    public class LocaleManager
    {
        private static LocaleManager instance = new LocaleManager();

        public static LocaleManager Instance { get { return instance; } }

        private int _storedLocale = -1;
        private bool _dirty = false;

        private LocaleManager()
        {
            // NOOP
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        public int GetLocale(IDatabaseAdapter adapter)
        {
            if (_storedLocale != -1 && !_dirty)
                return _storedLocale;

            // Attempt localisation on Death Touch, HACKY
            var aboveClassic = WoWVersionManager.GetInstance().SelectedVersion().Identity > 112;
            var name8 = aboveClassic ? ",`SpellName8` " : "";
            using (var res = adapter.Query("SELECT `id`,`SpellName0`,`SpellName1`,`SpellName2`,`SpellName3`,`SpellName4`," +
                "`SpellName5`,`SpellName6`,`SpellName7`" + name8 + " FROM `spell` WHERE `ID` = '5'"))
            {
                var rows = res.Rows;
                if (rows.Count == 0)
                    return 0;

                int locale = 0;
                if (rows[0] != null)
                {
                    for (int i = 1; i < rows[0].Table.Columns.Count; ++i)
                    {
                        if (rows[0][i].ToString().Length > 3)
                        {
                            locale = i;
                            break;
                        }
                    }
                }
                _storedLocale = locale;
                _dirty = false;
                return locale;
            }
        }
    }
}
