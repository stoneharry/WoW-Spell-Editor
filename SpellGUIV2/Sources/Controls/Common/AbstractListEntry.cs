using System;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls.Common
{
    public class AbstractListEntry : StackPanel, IListEntry
    {
        private Action<IListEntry> _CopyClickAction;
        private Action<IListEntry> _PasteClickAction;
        private Action<IListEntry> _DeleteClickAction;

        public void InvokeCopyAction() => Dispatcher?.Invoke(new Action(() => _CopyClickAction?.Invoke(this)));

        public void InvokePasteAction() => Dispatcher?.Invoke(new Action(() => _PasteClickAction?.Invoke(this)));

        public void InvokeDeleteAction() => Dispatcher?.Invoke(new Action(() => _DeleteClickAction?.Invoke(this)));

        public virtual void CopyItemClick(object sender, RoutedEventArgs args) => InvokeCopyAction();

        public virtual void PasteItemClick(object sender, RoutedEventArgs args) => InvokePasteAction();

        public virtual void DeleteItemClick(object sender, RoutedEventArgs args) => InvokeDeleteAction();

        public virtual void SetCopyClickAction(Action<IListEntry> copyClickAction) => _CopyClickAction = copyClickAction;

        public virtual void SetPasteClickAction(Action<IListEntry> pasteClickAction) => _PasteClickAction = pasteClickAction;

        public virtual void SetDeleteClickAction(Action<IListEntry> deleteEntryAction) => _DeleteClickAction = deleteEntryAction;
    }
}
