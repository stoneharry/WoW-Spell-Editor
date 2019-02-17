using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        public object threadSafeEnabled
        {
            get
            {
                if (!Dispatcher.CheckAccess())
                    return IsEnabled;
                return Dispatcher.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => IsEnabled));
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    IsEnabled = (Boolean)value;
                    return;
                }
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => IsEnabled = (Boolean)value));
            }
        }

        public object threadSafeVisibility
        {
            get
            {
                if (!Dispatcher.CheckAccess())
                    return Visibility;
                return Dispatcher.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => Visibility));
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    Visibility = (Visibility)value;
                    return;
                }
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => Visibility = (Visibility)value));
            }
        }

        public object threadSafeContent
        {
            get
            {
                if (!Dispatcher.CheckAccess())
                    return Content;
                return Dispatcher.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => Content));
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    Content = value;
                    return;
                }
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => Content = value));
            }
        }
    }
}
