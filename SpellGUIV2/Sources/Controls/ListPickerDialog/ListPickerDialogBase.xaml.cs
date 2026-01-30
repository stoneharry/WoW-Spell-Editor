using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using SpellEditor.Sources.Controls.Common;

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

        protected readonly MainWindow _mainWindow;

        protected ListPickerDialogBase(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            Owner = mainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // ItemsHost.BorderThickness = new Thickness(1);
        }

        protected abstract void LoadItemsList();
        protected abstract uint GetSelectedItemId(); // for return value
        protected abstract void GoToId(uint id);
        protected abstract void FilterFromText(string text);

        // TODO for default list controls with any custom logic/UI, can make protected parent functions
        // GoToId just calls this instead of a custom implementation
        // example if default is just a string
        protected void GoToIdListbox(ListBox listbox, uint id)
        {
            for (int i = 0; i < listbox.Items.Count; i++)
            {
                if (listbox.Items[i] is string text)
                {
                    if (uint.Parse(text.Split(' ')[1]) == id)
                    {
                        listbox.SelectedIndex = i;
                        listbox.ScrollIntoView(listbox.Items[i]);
                        return;
                    }
                }
            }
        }

        private void Select_None_Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SelectedId = 0;

            DialogResult = true;
            Close();
        }

        private void Select_Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SelectedId = GetSelectedItemId();

            // Debug.Assert(SelectedId != 0);

            if (SelectedId == 0)
                return; // bug, no item was selected

            DialogResult = true;
            Close();
        }

        private void Manual_Select_Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SelectedId = ManualIdSelectionBox.UIntValue;

            DialogResult = true;
            Close();
        }

        private void _KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Back && (sender == FilterSpellNames))
            {
                _KeyDown(sender, new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Space));
            }
        }

        private void _KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Copy pasta from mainwindow
            if (sender == NavigateToSpell)
            {
                if (e.Key != Key.Enter)
                {
                    return;
                }
                try
                {
                    UIntTextBox box = (UIntTextBox)sender;
                    uint ID = box.UIntValue;

                    GoToId(ID);
                }
                catch (Exception ex)
                {
                    _mainWindow.HandleErrorMessage(ex.Message);
                }
            }
            else if (sender == FilterSpellNames)
            {
                var input = FilterSpellNames.Text.ToLower();

                FilterFromText(input);
            }
        }


    }
}
