using System.Windows;
using System.Windows.Controls;

namespace Musegician.Core
{
    public class MultiSelectTreeViewItem : TreeViewItem
    {

        static MultiSelectTreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiSelectTreeViewItem),
                   new FrameworkPropertyMetadata(typeof(MultiSelectTreeViewItem)));
        }

        public bool IsAltState
        {
            get => (bool)GetValue(IsAltStateProperty);
            set => SetValue(IsAltStateProperty, value);
        }

        public bool IsItemSelected => MultiSelectTreeView.GetIsItemSelected(this);

        public static readonly DependencyProperty IsAltStateProperty =
            DependencyProperty.RegisterAttached("IsAltState", typeof(bool),
            typeof(MultiSelectTreeViewItem), new UIPropertyMetadata(false));


        protected override DependencyObject GetContainerForItemOverride() => new MultiSelectTreeViewItem();
        protected override bool IsItemItsOwnContainerOverride(object item) => item is MultiSelectTreeViewItem;
    }
}
