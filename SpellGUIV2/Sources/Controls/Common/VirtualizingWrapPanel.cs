using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SpellEditor.Sources.Controls.Common
{
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        private const double LineScrollAmount = 16.0;

        public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(
            nameof(ItemWidth), typeof(double), typeof(VirtualizingWrapPanel),
            new FrameworkPropertyMetadata(72.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
            nameof(ItemHeight), typeof(double), typeof(VirtualizingWrapPanel),
            new FrameworkPropertyMetadata(72.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        private Size _Extent = new Size(0, 0);
        private Size _Viewport = new Size(0, 0);
        private Point _Offset = new Point(0, 0);
        private int _Columns = 1;

        public double ItemWidth
        {
            get => (double)GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Move:
                    RemoveInternalChildRange(args.OldPosition.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemoveInternalChildRange(0, InternalChildren.Count);
                    _Offset = new Point(0, 0);
                    ScrollOwner?.InvalidateScrollInfo();
                    break;
            }
            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var unused = InternalChildren;
            var itemCount = GetItemCount();
            var itemWidth = Math.Max(1.0, ItemWidth);
            var itemHeight = Math.Max(1.0, ItemHeight);

            var viewportWidth = double.IsInfinity(availableSize.Width) ? itemWidth * itemCount : availableSize.Width;
            var viewportHeight = double.IsInfinity(availableSize.Height) ? itemHeight : availableSize.Height;

            _Columns = Math.Max(1, (int)(viewportWidth / itemWidth));
            var rows = itemCount == 0 ? 0 : (itemCount + _Columns - 1) / _Columns;

            UpdateScrollInfo(new Size(viewportWidth, viewportHeight),
                new Size(_Columns * itemWidth, rows * itemHeight));

            var firstRow = Math.Max(0, (int)(_Offset.Y / itemHeight) - 1);
            var lastRow = (int)((_Offset.Y + _Viewport.Height) / itemHeight) + 1;
            var firstIndex = firstRow * _Columns;
            var lastIndex = Math.Min(itemCount - 1, (lastRow + 1) * _Columns - 1);

            if (itemCount == 0 || firstIndex > lastIndex)
            {
                CleanUpItems(0, -1);
                return new Size(viewportWidth, double.IsInfinity(availableSize.Height) ? 0 : viewportHeight);
            }

            RealizeItems(firstIndex, lastIndex, new Size(itemWidth, itemHeight));
            CleanUpItems(firstIndex, lastIndex);

            return new Size(viewportWidth, double.IsInfinity(availableSize.Height) ? _Extent.Height : viewportHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var generator = ItemContainerGenerator;
            if (generator == null)
                return finalSize;

            var itemWidth = Math.Max(1.0, ItemWidth);
            var itemHeight = Math.Max(1.0, ItemHeight);
            var children = InternalChildren;

            for (var i = 0; i < children.Count; ++i)
            {
                var child = children[i];
                var itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
                if (itemIndex < 0)
                    continue;
                var row = itemIndex / _Columns;
                var column = itemIndex % _Columns;
                child.Arrange(new Rect(column * itemWidth - _Offset.X, row * itemHeight - _Offset.Y,
                    itemWidth, itemHeight));
            }
            return finalSize;
        }

        private void RealizeItems(int firstIndex, int lastIndex, Size itemSize)
        {
            var generator = ItemContainerGenerator;
            if (generator == null)
                return;

            var startPosition = generator.GeneratorPositionFromIndex(firstIndex);
            var childIndex = startPosition.Offset == 0 ? startPosition.Index : startPosition.Index + 1;

            using (generator.StartAt(startPosition, GeneratorDirection.Forward, true))
            {
                for (var itemIndex = firstIndex; itemIndex <= lastIndex; ++itemIndex, ++childIndex)
                {
                    var child = generator.GenerateNext(out bool newlyRealized) as UIElement;
                    if (child == null)
                        break;
                    if (newlyRealized || !InternalChildren.Contains(child))
                    {
                        if (childIndex >= InternalChildren.Count)
                            AddInternalChild(child);
                        else
                            InsertInternalChild(childIndex, child);
                        generator.PrepareItemContainer(child);
                    }
                    child.Measure(itemSize);
                }
            }
        }

        private void CleanUpItems(int firstIndex, int lastIndex)
        {
            var generator = ItemContainerGenerator;
            if (generator == null)
                return;

            var recycler = GetVirtualizationMode(this) == VirtualizationMode.Recycling
                ? generator as IRecyclingItemContainerGenerator
                : null;
            for (var i = InternalChildren.Count - 1; i >= 0; --i)
            {
                var position = new GeneratorPosition(i, 0);
                var itemIndex = generator.IndexFromGeneratorPosition(position);
                if (itemIndex >= firstIndex && itemIndex <= lastIndex)
                    continue;
                if (recycler != null)
                    recycler.Recycle(position, 1);
                else
                    generator.Remove(position, 1);
                RemoveInternalChildRange(i, 1);
            }
        }

        private int GetItemCount()
        {
            var owner = ItemsControl.GetItemsOwner(this);
            return owner?.Items.Count ?? 0;
        }

        private void UpdateScrollInfo(Size viewport, Size extent)
        {
            var changed = false;
            if (extent != _Extent)
            {
                _Extent = extent;
                changed = true;
            }
            if (viewport != _Viewport)
            {
                _Viewport = viewport;
                changed = true;
            }
            var maxOffset = Math.Max(0, _Extent.Height - _Viewport.Height);
            if (_Offset.Y > maxOffset)
            {
                _Offset.Y = maxOffset;
                changed = true;
            }
            if (changed)
                ScrollOwner?.InvalidateScrollInfo();
        }

        public bool CanHorizontallyScroll { get; set; }

        public bool CanVerticallyScroll { get; set; }

        public double ExtentWidth => _Extent.Width;

        public double ExtentHeight => _Extent.Height;

        public double ViewportWidth => _Viewport.Width;

        public double ViewportHeight => _Viewport.Height;

        public double HorizontalOffset => _Offset.X;

        public double VerticalOffset => _Offset.Y;

        public ScrollViewer ScrollOwner { get; set; }

        public void LineUp() => SetVerticalOffset(VerticalOffset - LineScrollAmount);

        public void LineDown() => SetVerticalOffset(VerticalOffset + LineScrollAmount);

        public void LineLeft() => SetHorizontalOffset(HorizontalOffset - LineScrollAmount);

        public void LineRight() => SetHorizontalOffset(HorizontalOffset + LineScrollAmount);

        public void PageUp() => SetVerticalOffset(VerticalOffset - _Viewport.Height);

        public void PageDown() => SetVerticalOffset(VerticalOffset + _Viewport.Height);

        public void PageLeft() => SetHorizontalOffset(HorizontalOffset - _Viewport.Width);

        public void PageRight() => SetHorizontalOffset(HorizontalOffset + _Viewport.Width);

        public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - LineScrollAmount * 3);

        public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + LineScrollAmount * 3);

        public void MouseWheelLeft() => SetHorizontalOffset(HorizontalOffset - LineScrollAmount * 3);

        public void MouseWheelRight() => SetHorizontalOffset(HorizontalOffset + LineScrollAmount * 3);

        public void SetHorizontalOffset(double offset)
        {
            var maxOffset = Math.Max(0, _Extent.Width - _Viewport.Width);
            offset = Math.Max(0, Math.Min(offset, maxOffset));
            if (offset == _Offset.X)
                return;
            _Offset.X = offset;
            ScrollOwner?.InvalidateScrollInfo();
            InvalidateMeasure();
        }

        public void SetVerticalOffset(double offset)
        {
            var maxOffset = Math.Max(0, _Extent.Height - _Viewport.Height);
            offset = Math.Max(0, Math.Min(offset, maxOffset));
            if (offset == _Offset.Y)
                return;
            _Offset.Y = offset;
            ScrollOwner?.InvalidateScrollInfo();
            InvalidateMeasure();
        }

        public Rect MakeVisible(System.Windows.Media.Visual visual, Rect rectangle)
        {
            if (rectangle.IsEmpty || visual == null || ReferenceEquals(visual, this))
                return Rect.Empty;

            var transform = visual.TransformToAncestor(this);
            var bounds = transform.TransformBounds(rectangle);
            var top = bounds.Top + _Offset.Y;
            var bottom = bounds.Bottom + _Offset.Y;

            if (top < _Offset.Y)
                SetVerticalOffset(top);
            else if (bottom > _Offset.Y + _Viewport.Height)
                SetVerticalOffset(bottom - _Viewport.Height);

            return new Rect(bounds.X, bounds.Y, Math.Min(bounds.Width, _Viewport.Width),
                Math.Min(bounds.Height, _Viewport.Height));
        }
    }
}
