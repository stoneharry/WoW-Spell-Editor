using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace SpellEditor.Sources.Controls.ListPickerDialog
{
    public abstract partial class ListPickerDialogBase : MetroWindow
    {
        // protected ListView ListView => listView; // from XAML
        protected ContentControl ItemsHostControl
        {
            get { return ItemsHost; }
        }
        protected void SetItemsControl(ItemsControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            ItemsHost.Content = control;
        }
        protected void SetSelectionTypeText(string text)
        {
            SelectionTextLabel.Content = text;
        }

        // public object SelectedItem => ListView.SelectedItem;
        public uint SelectedId { get; protected set; } = 0;

        public ListPickerDialogBase()
        {
            InitializeComponent();
        }

        protected abstract void LoadItemsList();
        protected abstract uint GetSelectedItemId(); // for return value
        protected abstract void GoToId(uint id);
        protected abstract void FilterFromText(string text);

        private void Select_None_Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SelectedId = 0;

            DialogResult = true;
            Close();
        }

        private void Select_Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SelectedId = GetSelectedItemId();

            Debug.Assert(SelectedId != 0);

            if (SelectedId == 0)
                return; // bug, no item was selected

            DialogResult = true;
            Close();
        }
    }
}
