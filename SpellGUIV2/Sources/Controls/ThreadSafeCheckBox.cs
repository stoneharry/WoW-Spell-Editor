using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SpellEditor.Sources.Controls
{
    public class ThreadSafeCheckBox : CheckBox
    {
        public object threadSafeChecked
        {
            get
            {
                if (!Dispatcher.CheckAccess())
                    return IsChecked;
                return Dispatcher.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => IsChecked));
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    IsChecked = (Boolean)value;
                    return;
                }
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => IsChecked = (Boolean)value));
            }
        }
    }
}
