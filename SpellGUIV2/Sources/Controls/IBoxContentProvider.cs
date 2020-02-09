using System.Collections.Generic;

namespace SpellEditor.Sources.Controls
{
    public interface IBoxContentProvider
    {
        List<DBCBoxContainer> GetAllBoxes();
    }
}
