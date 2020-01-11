using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SpellEditor.Sources.Controls
{
    public class ThreadSafeTextBox : TextBox
    {
        public object ThreadSafeText
        {
            get
            {
                if (Dispatcher != null && !Dispatcher.CheckAccess())
                    return Text;
                return Dispatcher?.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => Text));
            }
            set
            {
                if (Dispatcher != null && Dispatcher.CheckAccess())
                {
                    Text = value.ToString();
                    return;
                }
                Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() => Text = value.ToString()));
            }
        }
    }
}
