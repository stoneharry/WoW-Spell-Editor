using System.Collections.Generic;

namespace SpellEditor.Sources.Controls
{
    interface IBoxContentProvider
    {
        List<DBCBoxContainer> GetAllBoxes();
    }
}
