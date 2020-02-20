using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SpellEditor.Sources.Controls.Visual
{
    public class AbstractVisualListEntry : StackPanel, IVisualListEntry
    {
        private Action<IVisualListEntry> _CopyClickAction;
        private Action<IVisualListEntry> _PasteClickAction;
        private Action<IVisualListEntry> _DeleteClickAction;

        public void InvokeCopyAction() => Dispatcher?.Invoke(new Action(() => _CopyClickAction?.Invoke(this)));

        public void InvokePasteAction() => Dispatcher?.Invoke(new Action(() => _PasteClickAction?.Invoke(this)));

        public void InvokeDeleteAction() => Dispatcher?.Invoke(new Action(() => _DeleteClickAction?.Invoke(this)));

        public virtual void CopyItemClick(object sender, RoutedEventArgs args) => InvokeCopyAction();

        public virtual void PasteItemClick(object sender, RoutedEventArgs args) => InvokePasteAction();

        public virtual void DeleteItemClick(object sender, RoutedEventArgs args) => InvokeDeleteAction();

        public virtual void SetCopyClickAction(Action<IVisualListEntry> copyClickAction) => _CopyClickAction = copyClickAction;

        public virtual void SetPasteClickAction(Action<IVisualListEntry> pasteClickAction) => _PasteClickAction = pasteClickAction;

        public virtual void SetDeleteClickAction(Action<IVisualListEntry> deleteEntryAction) => _DeleteClickAction = deleteEntryAction;
    }
}
