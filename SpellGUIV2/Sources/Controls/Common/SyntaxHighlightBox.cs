using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls
{
    class SyntaxHighlightBox : RichTextBox
    {
        public SyntaxHighlightBox()
        {
            
        }

        public void setScript(string script)
        {
            SelectAll();
            Selection.Text = script;
        }
    }
}
