using System;
using System.Collections.Generic;
using System.Data;

namespace SpellEditor.Sources.Emulator
{
    public enum ColumnKind
    {
        UInt,
        Int,
        /// <summary>64 bit, the 1.12 spell family masks do not fit in a uint.</summary>
        ULong,
        Float,
        Text
    }

    /// <summary>What a column gets to look at when the user adds a row.</summary>
    public class NewRowContext
    {
        public WorldDatabase Database;
        public RankChain Chain;
        /// <summary>Rows already on screen, unsaved ones included.</summary>
        public IReadOnlyList<DataRow> Rows;

        public uint SpellId => Chain?.SpellId ?? 0;
    }

    public class WorldColumn
    {
        public string Name;
        public ColumnKind Kind = ColumnKind.UInt;
        /// <summary>Part of the row identity, used in the WHERE clause on update and delete.</summary>
        public bool IsKey;
        /// <summary>Holds the spell being edited. Filled in from the selection, shown read only.</summary>
        public bool IsPrimarySpellKey;
        /// <summary>Offer the spell picker dialog on this column.</summary>
        public bool ShowSpellPicker;
        public bool AllowNull;
        public string DefaultValue = "0";
        public FlagSet Flags;
        public EnumSet Enum;
        public bool IsMultiline;
        /// <summary>From the table label list, see WorldTable.ApplyLocalisedText.</summary>
        public string Label;
        /// <summary>From the table tooltip list, see WorldTable.ApplyLocalisedText.</summary>
        public string Tooltip;
        /// <summary>Set when a negative id means something here, turning the sign into a tick box.</summary>
        public string NegativeOptionKey;
        public string FallbackNegativeOption;
        /// <summary>Override used when adding a row, e.g. the next free group id.</summary>
        public Func<NewRowContext, string> NewRowValue;

        public bool IsNumeric => Kind != ColumnKind.Text;

        public bool HasNegativeOption => FallbackNegativeOption != null;

        public string NegativeOption
        {
            get
            {
                var resource = System.Windows.Application.Current?.TryFindResource(NegativeOptionKey) as string;
                return string.IsNullOrWhiteSpace(resource) ? FallbackNegativeOption : resource.Trim();
            }
        }
    }
}
