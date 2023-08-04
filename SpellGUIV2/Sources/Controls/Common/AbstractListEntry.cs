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
        private Action<IListEntry> _CancelClickAction;

        public void InvokeCopyAction() => Dispatcher?.Invoke(new Action(() => _CopyClickAction?.Invoke(this)));

        public void InvokePasteAction() => Dispatcher?.Invoke(new Action(() => _PasteClickAction?.Invoke(this)));

        public void InvokeDeleteAction() => Dispatcher?.Invoke(new Action(() => _DeleteClickAction?.Invoke(this)));

        public void InvokeCancelAction() => Dispatcher?.Invoke(new Action(() => _CancelClickAction?.Invoke(this)));

        public virtual void CopyItemClick(object sender, RoutedEventArgs args) => InvokeCopyAction();

        public virtual void PasteItemClick(object sender, RoutedEventArgs args) => InvokePasteAction();

        public virtual void DeleteItemClick(object sender, RoutedEventArgs args) => InvokeDeleteAction();

        public virtual void CancelItemClick(object sender, RoutedEventArgs args) => InvokeCancelAction();
      
        public virtual void SetCopyClickAction(Action<IListEntry> action) => _CopyClickAction = action;

        public virtual void SetPasteClickAction(Action<IListEntry> action) => _PasteClickAction = action;

        public virtual void SetDeleteClickAction(Action<IListEntry> action) => _DeleteClickAction = action;

        public virtual void SetCancelClickAction(Action<IListEntry> action) => _CancelClickAction = action;
    }
}
