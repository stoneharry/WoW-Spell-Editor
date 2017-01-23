using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SpellEditor.Sources.Controls
{
    class ThreadSafeComboBox : ComboBox
    {
        public object threadSafeIndex {
            get
            {
                if (!Dispatcher.CheckAccess())
                    return SelectedIndex;
                return Dispatcher.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => SelectedIndex));
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    SelectedIndex = Int32.Parse(value.ToString());
                    return;
                }
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => SelectedIndex = Int32.Parse(value.ToString())));
            }
        }

		public object threadSafeText
		{
			get
			{
				if (!Dispatcher.CheckAccess())
					return Text;
				return Dispatcher.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => Text));
			}
			set
			{
				if (Dispatcher.CheckAccess())
				{
					Text = value.ToString();
					return;
				}
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => Text = value.ToString()));
			}
		}
	}
}
