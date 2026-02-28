using System.Windows.Controls;

namespace SpellEditor.Sources.Controls.Common
{
    public class FriendlyLabel : Label
    {
        public override string ToString()
        {
            return Content.ToString();
        }
    }
}
