using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SpellEditor.Sources.Controls
{
    public class ThreadSafeComboBox : ComboBox
    {
        public object ThreadSafeIndex {
            get
            {
                if (Dispatcher != null && !Dispatcher.CheckAccess())
                    return SelectedIndex;
                return Dispatcher?.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => SelectedIndex));
            }
            set
            {
                if (Dispatcher != null && Dispatcher.CheckAccess())
                {
                    SelectedIndex = int.Parse(value.ToString());
                    return;
                }
                Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() => SelectedIndex = int.Parse(value.ToString())));
            }
        }

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
