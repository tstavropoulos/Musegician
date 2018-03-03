using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Musegician.Core
{
    /// <summary>
    /// Partially pilfered from:
    /// https://stackoverflow.com/questions/459375/customizing-the-treeview-to-allow-multi-select
    /// </summary>
    public class MultiSelectTreeView : TreeView
    {
        #region Fields

        // Used in shift selections
        private MultiSelectTreeViewItem _lastItemSelected;

        #endregion Fields
        #region Dependency Properties

        public static readonly DependencyProperty IsItemSelectedProperty =
            DependencyProperty.RegisterAttached("IsItemSelected", typeof(bool),
                typeof(MultiSelectTreeView));

        public static void SetIsItemSelected(UIElement element, bool value)
        {
            element.SetValue(IsItemSelectedProperty, value);
        }

        public static bool GetIsItemSelected(UIElement element)
        {
            return (bool)element.GetValue(IsItemSelectedProperty);
        }

        #endregion Dependency Properties
        #region Constructors

        static MultiSelectTreeView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiSelectTreeView),
                   new FrameworkPropertyMetadata(typeof(MultiSelectTreeView)));
        }

        #endregion Constructors
        #region Properties

        private static bool IsCtrlPressed
        {
            get { return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl); }
        }
        private static bool IsShiftPressed
        {
            get { return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift); }
        }

        public IList SelectedItems
        {
            get
            {
                IEnumerable<MultiSelectTreeViewItem> selectedTreeViewItems =
                    GetTreeViewItems(this, true).Where(GetIsItemSelected);
                IEnumerable<object> selectedModelItems =
                    selectedTreeViewItems.Select(treeViewItem => treeViewItem.Header);

                return selectedModelItems.ToList();
            }
        }


        public MultiSelectTreeViewItem OneSelectedItem
        {
            get
            {
                return GetTreeViewItems(this, true).FirstOrDefault(GetIsItemSelected);
            }
        }

        #endregion Properties
        #region Event Handlers

        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            base.OnContextMenuOpening(e);
            if (e.OriginalSource is FrameworkElement elem)
            {
                if (elem.DataContext.GetType() != OneSelectedItem?.DataContext.GetType())
                {
                    //Block popups on wrong types
                    e.Handled = true;
                }
            }
        }

        private MultiSelectTreeViewItem mouseDownItem = null;
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            mouseDownItem = null;

            // If clicking on a tree branch expander...
            if (InExpanderTree(e.OriginalSource))
            {
                return;
            }


            MultiSelectTreeViewItem item = GetTreeViewItemClicked((FrameworkElement)e.OriginalSource);
            if (item != null)
            {
                if (GetIsItemSelected(item))
                {
                    mouseDownItem = item;
                    return;
                }

                SelectedItemChangedInternal(item);

                item.IsSelected = true;
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            // If clicking on a tree branch expander...
            if (InExpanderTree(e.OriginalSource))
            {
                return;
            }

            MultiSelectTreeViewItem item = GetTreeViewItemClicked((FrameworkElement)e.OriginalSource);
            if (item != null)
            {
                if (e.ChangedButton == MouseButton.Left && item == mouseDownItem)
                {
                    SelectedItemChangedInternal(item);

                    item.IsSelected = true;
                }

            }
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            if (!Convert.ToBoolean(e.NewValue) &&
                SelectedItem != null)
            {
                if (SelectedItem is TreeViewItem item)
                {
                    item.IsSelected = false;
                }
                else
                {
                    if (ItemContainerGenerator.ContainerFromItem(SelectedItem) is TreeViewItem foundItem)
                    {
                        foundItem.IsSelected = false;
                    }
                }
            }
        }

        private bool InExpanderTree(object obj)
        {
            if (obj is Path)
            {
                return true;
            }

            if (obj is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is Path)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion Event Handlers
        #region Utility Methods

        private void SelectedItemChangedInternal(MultiSelectTreeViewItem tvItem)
        {
            // Clear all previous selected item states if ctrl is NOT being held down
            if (!IsCtrlPressed)
            {
                List<MultiSelectTreeViewItem> items = GetTreeViewItems(this, true);
                foreach (MultiSelectTreeViewItem treeViewItem in items)
                {
                    SetIsItemSelected(treeViewItem, false);
                }
            }

            // Is this an item range selection?
            if (IsShiftPressed)
            {
                if (_lastItemSelected == null)
                {
                    // Otherwise, individual selection
                    SetIsItemSelected(tvItem, true);
                    _lastItemSelected = tvItem;
                }
                else
                {
                    List<MultiSelectTreeViewItem> items = GetTreeViewItemRange(_lastItemSelected, tvItem);
                    if (items.Count > 0)
                    {
                        foreach (MultiSelectTreeViewItem treeViewItem in items)
                        {
                            if (OneSelectedItem == null ||
                                treeViewItem.Header.GetType() == OneSelectedItem.Header.GetType())
                            {
                                SetIsItemSelected(treeViewItem, true);
                            }
                        }

                        _lastItemSelected = items.Last();
                    }
                }
            }
            else if (IsCtrlPressed)
            {
                //Deselect if it's selected
                if (GetIsItemSelected(tvItem))
                {
                    SetIsItemSelected(tvItem, false);
                    _lastItemSelected = tvItem;
                }
                else if (OneSelectedItem == null ||
                    OneSelectedItem.Header.GetType() == tvItem.Header.GetType())
                {
                    //Select it if it's on its own, or matches the tier of the other selected items
                    SetIsItemSelected(tvItem, true);
                    _lastItemSelected = tvItem;
                }
            }
            else
            {
                // Otherwise, individual selection
                SetIsItemSelected(tvItem, true);
                _lastItemSelected = tvItem;
            }
        }

        private static MultiSelectTreeViewItem GetTreeViewItemClicked(DependencyObject sender)
        {
            while (sender != null && !(sender is MultiSelectTreeViewItem))
            {
                sender = VisualTreeHelper.GetParent(sender);
            }

            return sender as MultiSelectTreeViewItem;
        }

        private static List<MultiSelectTreeViewItem> GetTreeViewItems(
            ItemsControl parentItem,
            bool includeCollapsedItems,
            List<MultiSelectTreeViewItem> itemList = null)
        {
            if (itemList == null)
            {
                itemList = new List<MultiSelectTreeViewItem>();
            }

            for (int index = 0; index < parentItem.Items.Count; index++)
            {
                MultiSelectTreeViewItem tvItem =
                    parentItem.ItemContainerGenerator.ContainerFromIndex(index) as MultiSelectTreeViewItem;
                if (tvItem == null)
                {
                    continue;
                }

                itemList.Add(tvItem);
                if (includeCollapsedItems || tvItem.IsExpanded)
                {
                    GetTreeViewItems(tvItem, includeCollapsedItems, itemList);
                }
            }
            return itemList;
        }

        private static MultiSelectTreeView GetTreeView(DependencyObject sender)
        {
            while (sender != null && !(sender is MultiSelectTreeView))
            {
                sender = VisualTreeHelper.GetParent(sender);
            }

            return sender as MultiSelectTreeView;
        }

        private List<MultiSelectTreeViewItem> GetTreeViewItemRange(
            MultiSelectTreeViewItem start,
            MultiSelectTreeViewItem end)
        {
            List<MultiSelectTreeViewItem> items = GetTreeViewItems(this, false);

            int startIndex = items.IndexOf(start);
            int endIndex = items.IndexOf(end);
            int rangeStart = startIndex > endIndex || startIndex == -1 ? endIndex : startIndex;
            int rangeCount = startIndex > endIndex ? startIndex - endIndex + 1 : endIndex - startIndex + 1;

            if (startIndex == -1 && endIndex == -1)
            {
                rangeCount = 0;
            }
            else if (startIndex == -1 || endIndex == -1)
            {
                rangeCount = 1;
            }

            return rangeCount > 0 ? items.GetRange(rangeStart, rangeCount) : new List<MultiSelectTreeViewItem>();
        }

        #endregion Utility Methods
        #region Overrides

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MultiSelectTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is MultiSelectTreeViewItem;
        }

        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            base.OnSelectedItemChanged(e);
        }

        #endregion Overrides
    }

    public class MultiSelectTreeViewItem : TreeViewItem
    {

        static MultiSelectTreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiSelectTreeViewItem),
                   new FrameworkPropertyMetadata(typeof(MultiSelectTreeViewItem)));
        }

        public bool IsAltState
        {
            get { return (bool)GetValue(IsAltStateProperty); }
            set { SetValue(IsAltStateProperty, value); }
        }

        public bool IsItemSelected
        {
            get { return MultiSelectTreeView.GetIsItemSelected(this); }
        }

        public static readonly DependencyProperty IsAltStateProperty =
            DependencyProperty.RegisterAttached("IsAltState", typeof(bool),
            typeof(MultiSelectTreeViewItem), new UIPropertyMetadata(false));


        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MultiSelectTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is MultiSelectTreeViewItem;
        }
    }
}
