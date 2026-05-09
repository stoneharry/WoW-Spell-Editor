using SpellEditor.Sources.Database;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace HeadlessExport
{
    /// <summary>
    /// Wraps an IDatabaseAdapter and serves spell-by-ID lookups from an in-memory
    /// dictionary instead of hitting MySQL. All other queries pass through to the
    /// real adapter. This eliminates the per-spell SELECT that GetRecordById fires
    /// for every cross-spell reference token (e.g. $70907d) in spell descriptions.
    /// </summary>
    class CachingDatabaseAdapter : IDatabaseAdapter
    {
        private static readonly Regex SpellByIdPattern = new Regex(
            @"SELECT \* FROM `spell` WHERE `ID` = '(\d+)'",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IDatabaseAdapter _inner;
        private readonly Dictionary<uint, DataRow> _rowById;
        private readonly DataTable _schema;
        private readonly ConcurrentDictionary<uint, DataTable> _resultCache;

        public bool Updating { get => _inner.Updating; set => _inner.Updating = value; }

        /// <param name="inner">The real MySQL adapter for non-cached queries.</param>
        /// <param name="allSpells">Pre-loaded full spell table. Must remain alive.</param>
        /// <param name="resultCache">Shared cache of single-row DataTables keyed by spell ID.</param>
        public CachingDatabaseAdapter(
            IDatabaseAdapter inner,
            DataTable allSpells,
            ConcurrentDictionary<uint, DataTable> resultCache)
        {
            _inner = inner;
            _resultCache = resultCache;
            _schema = allSpells.Clone(); // empty table with correct schema, used as template

            _rowById = new Dictionary<uint, DataRow>(allSpells.Rows.Count);
            foreach (DataRow row in allSpells.Rows)
            {
                if (uint.TryParse(row["ID"].ToString(), out uint id))
                    _rowById[id] = row;
            }
        }

        public DataTable Query(string query)
        {
            var match = SpellByIdPattern.Match(query);
            if (match.Success && uint.TryParse(match.Groups[1].Value, out uint id))
            {
                return _resultCache.GetOrAdd(id, key =>
                {
                    var result = _schema.Clone();
                    if (_rowById.TryGetValue(key, out var row))
                        result.ImportRow(row);
                    return result;
                });
            }
            return _inner.Query(query);
        }

        public void Execute(string p)                                       => _inner.Execute(p);
        public void CommitChanges(string query, DataTable dataTable)        => _inner.CommitChanges(query, dataTable);
        public object QuerySingleValue(string query)                        => _inner.QuerySingleValue(query);
        public void CreateAllTablesFromBindings()                           => _inner.CreateAllTablesFromBindings();
        public string EscapeString(string str)                              => _inner.EscapeString(str);
        public string GetTableCreateString(SpellEditor.Sources.Binding.Binding binding) => _inner.GetTableCreateString(binding);
        public void Dispose() { } // lifetime managed by caller
    }
}
