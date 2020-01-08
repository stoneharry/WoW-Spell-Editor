using System.Windows.Controls;

namespace SpellEditor.Sources.Controls
{
    class FriendlyLabel : Label
    {
        public override string ToString()
        {
            return Content.ToString();
        }
    }
}
