using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SpellEditor.Sources.Controls
{
    public class ThreadSafeCheckBox : CheckBox
    {
        public object ThreadSafeChecked
        {
            get
            {
                if (Dispatcher != null && !Dispatcher.CheckAccess())
                    return IsChecked;
                return Dispatcher?.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => IsChecked));
            }
            set
            {
                if (Dispatcher != null && Dispatcher.CheckAccess())
                {
                    IsChecked = (bool)value;
                    return;
                }
                Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() => IsChecked = (bool)value));
            }
        }

        public object ThreadSafeEnabled
        {
            get
            {
                if (Dispatcher != null && !Dispatcher.CheckAccess())
                    return IsEnabled;
                return Dispatcher?.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => IsEnabled));
            }
            set
            {
                if (Dispatcher != null && Dispatcher.CheckAccess())
                {
                    IsEnabled = (bool)value;
                    return;
                }
                Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() => IsEnabled = (bool)value));
            }
        }

        public object ThreadSafeVisibility
        {
            get
            {
                if (Dispatcher != null && !Dispatcher.CheckAccess())
                    return Visibility;
                return Dispatcher?.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => Visibility));
            }
            set
            {
                if (Dispatcher != null && Dispatcher.CheckAccess())
                {
                    Visibility = (Visibility)value;
                    return;
                }
                Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() => Visibility = (Visibility)value));
            }
        }

        public object ThreadSafeContent
        {
            get
            {
                if (Dispatcher != null && !Dispatcher.CheckAccess())
                    return Content;
                return Dispatcher?.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => Content));
            }
            set
            {
                if (Dispatcher != null && Dispatcher.CheckAccess())
                {
                    Content = value;
                    return;
                }
                Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() => Content = value));
            }
        }
    }
}
