using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ad2AventriSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> AllowedSearchFields = new List<string> {
            "sAMAccountName",
            "userPrincipalName",
            "displayName",
            "cn",
            "GivenName",
            "sn",
            "Name",
            "Mail",
            "Company",
            "Department",
            "co",
            "l",
            "st",
            "streetAddress",
            "postalCode",
            "physicalDeliveryOfficeName",
            "roomNumber",
            "employeeType",
            "employeeNumber",
            "memberOf",
            "manager"
        };

        private string _currentOpenFile = null;

        public MainWindow()
        {
            InitializeComponent();
            TreeViewHelpers.TreeViewContextMenu = SearchTreeView.Resources["SearchTreeViewContext"] as ContextMenu;
            initAsync();
        }

        private async void initAsync()
        {
            Task t = new Task(() =>
            {
                string p = AdSearcher.GetDefaultLdapPath();
                this.Dispatcher.Invoke(() =>
                {
                    this.ldapBaseDn.Text = p;
                });
            });
            t.Start();
            await t;
        }

        private TreeViewItem CreateSearchTreeMenuItem()
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Visibility = Visibility.Visible };
            /*sp.Children.Add(new Image
            {
                Source = new BitmapImage(new Uri(img, UriKind.Relative)),
                Height = 20,
                Width = 20
            });*/
            sp.Children.Add(new TextBlock { Text = "And" });
            return new LdapSearchTreeViewItem
            {
                Header = sp,
                ContextMenu = SearchTreeView.Resources["SearchTreeViewContext"] as ContextMenu,
                SearchOperatorType = LdapSearchExprEnum.AndOperator
            };
        }


        private void PreviewSearchItemButtonClick(object sender, RoutedEventArgs e)
        {
            string search = TreeViewHelpers.GetLdapSearchQuery(SearchTreeView);
            if (search == "")
            {
                MessageBox.Show("Your search is empty!");
                return;
            }
            AdQueryBrowser b = new AdQueryBrowser(search, ldapBaseDn.Text);
            b.Show();
        }


        private void SanitizeSearchItemButtonClick(object sender, RoutedEventArgs e)
        {
            TreeViewHelpers.SanitizeTreeview(SearchTreeView);
        }

        private void CloneSelectedSearchItemClick(object sender, RoutedEventArgs e)
        {
            TreeViewHelpers.TreeViewContextMenu = SearchTreeView.Resources["SearchTreeViewContext"] as ContextMenu;
            if (SearchTreeView.SelectedItem != null)
            {
                if (!SearchTreeView.Items.Contains(SearchTreeView.SelectedItem))
                {
                    TreeViewHelpers.CloneTreeview(SearchTreeView);
                }
            }
        }

        private void SanitizeSelectedSearchItemClick(object sender, RoutedEventArgs e)
        {
            if (SearchTreeView.SelectedItem != null)
            {
                List<LdapSearchTreeViewItem> itemsToDelete = new List<LdapSearchTreeViewItem>();
                TreeViewHelpers.RunTreeviewRecursive(SearchTreeView.SelectedItem, delegate (TreeViewItem item)
                {
                    LdapSearchTreeViewItem litem = item as LdapSearchTreeViewItem;
                    if (litem == null) return;
                    if (litem.SearchOperatorType != LdapSearchExprEnum.Comparison && litem.Items.Count == 0)
                    {
                        itemsToDelete.Add(litem);
                    }
                });
                while (itemsToDelete.Count > 0)
                {
                    LdapSearchTreeViewItem item = itemsToDelete.First();
                    itemsToDelete.Remove(item);
                    TreeViewItem pitem = item.Parent as TreeViewItem;
                    if (pitem == null) continue;
                    pitem.Items.Remove(item);
                    LdapSearchTreeViewItem lpitem = pitem as LdapSearchTreeViewItem;
                    if (lpitem == null) continue;
                    if (lpitem.SearchOperatorType != LdapSearchExprEnum.Comparison && lpitem.Items.Count == 0)
                    {
                        itemsToDelete.Add(lpitem);
                    }
                }

            }
        }

        private void AddSearchItemButtonClick(object sender, RoutedEventArgs e)
        {
            TreeViewItem item;
            if (SearchTreeView.SelectedItem == null)
            {
                item = SearchTreeView.Items[0] as TreeViewItem;
            }
            else
            {
                item = SearchTreeView.SelectedItem as TreeViewItem;
                FrameworkElement f = SearchTreeView.SelectedItem as FrameworkElement;
                while (item == null && f != null)
                {
                    item = f.Parent as TreeViewItem;
                    f = f.Parent as FrameworkElement;
                }
            }

            if (item != null)
            {
                if (item is LdapSearchTreeViewItem)
                {
                    if (((LdapSearchTreeViewItem)item).SearchOperatorType == LdapSearchExprEnum.Comparison)
                    {
                        return;
                    }
                }
                item.Items.Add(CreateSearchTreeMenuItem());
                item.IsExpanded = true;
                ((ComboBoxItem)SearchOperatorCombo.Items[0]).IsEnabled = false;
            }
        }

        private void RemoveSearchItemButtonClick(object sender, RoutedEventArgs e)
        {
            if (SearchTreeView.SelectedItem != null)
            {
                if (!SearchTreeView.Items.Contains(SearchTreeView.SelectedItem))
                {
                    TreeViewItem item = SearchTreeView.SelectedItem as TreeViewItem;
                    item = item.Parent as TreeViewItem;
                    item.Items.Remove(SearchTreeView.SelectedItem);
                }
                else
                {
                    if(SearchTreeView.Items[0] == SearchTreeView.SelectedItem)
                    {
                        if((
                            SearchTreeView.Items[0] as TreeViewItem).Items.Count > 0 &&
                            MessageBox.Show("Do you want to all delete search items?", "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
                        )
                        {
                            (SearchTreeView.Items[0] as TreeViewItem).Items.Clear();
                        }
                    }
                }
            }
        }

        private void MoveUpSearchItemButtonClick(object sender, RoutedEventArgs e)
        {
            if (SearchTreeView.SelectedItem != null)
            {
                if (!SearchTreeView.Items.Contains(SearchTreeView.SelectedItem))
                {
                    TreeViewItem item = SearchTreeView.SelectedItem as TreeViewItem;
                    TreeViewItem pItem = item.Parent as TreeViewItem;
                    TreeViewItem oItem;
                    int pos = pItem.Items.IndexOf(SearchTreeView.SelectedItem);
                    if (pos - 1 >= 0)
                    {
                        oItem = (TreeViewItem)pItem.Items[pos - 1];

                        pItem.Items.RemoveAt(pos - 1);
                        pItem.Items.RemoveAt(pos - 1);
                        pItem.Items.Insert(pos - 1, oItem);
                        pItem.Items.Insert(pos - 1, item);
                        item.IsSelected = true;
                    }
                }
            }
        }
        private void MoveDownSearchItemButtonClick(object sender, RoutedEventArgs e)
        {
            if (SearchTreeView.SelectedItem != null)
            {
                if (!SearchTreeView.Items.Contains(SearchTreeView.SelectedItem))
                {
                    TreeViewItem item = SearchTreeView.SelectedItem as TreeViewItem;
                    TreeViewItem pItem = item.Parent as TreeViewItem;
                    TreeViewItem oItem;
                    int pos = pItem.Items.IndexOf(SearchTreeView.SelectedItem);
                    if (pos + 1 < pItem.Items.Count)
                    {
                        oItem = (TreeViewItem)pItem.Items[pos + 1];

                        pItem.Items.RemoveAt(pos);
                        pItem.Items.RemoveAt(pos);
                        pItem.Items.Insert(pos, item);
                        pItem.Items.Insert(pos, oItem);
                        item.IsSelected = true;
                    }
                }
            }
        }


        private void ExpandSelectedSearchItemClick(object sender, RoutedEventArgs e)
        {
            if (SearchTreeView.SelectedItem != null)
            {
                TreeViewHelpers.RunTreeviewRecursive(SearchTreeView.SelectedItem, delegate (TreeViewItem item)
                {
                    item.IsExpanded = true;
                });
            }
        }
        private void ExpandSearchItemButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (object i in SearchTreeView.Items)
            {
                TreeViewHelpers.RunTreeviewRecursive(i, delegate (TreeViewItem item)
                {
                    item.IsExpanded = true;
                });
            }
        }
        private void CollapseSelectedSearchItemClick(object sender, RoutedEventArgs e)
        {
            if (SearchTreeView.SelectedItem != null)
            {
                TreeViewHelpers.RunTreeviewRecursive(SearchTreeView.SelectedItem, delegate (TreeViewItem item)
                {
                    item.IsExpanded = false;
                });
            }
        }
        private void CollapseSearchItemButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (object i in SearchTreeView.Items)
            {
                TreeViewHelpers.RunTreeviewRecursive(i, delegate (TreeViewItem item)
                {
                    item.IsExpanded = false;
                });
            }
        }

        #region TreeView Drag&Drop
        Point startPoint;
        private void Tree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }
        private void Tree_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var mousePos = e.GetPosition(null);
                var diff = startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var treeView = sender as TreeView;
                    var treeViewItem =
                        FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                    if (treeView == null || treeViewItem == null)
                        return;

                    var folderViewModel = treeView.SelectedItem as TreeViewItem;
                    if (folderViewModel == null)
                        return;

                    var dragData = new DataObject(folderViewModel);
                    DragDrop.DoDragDrop(treeViewItem, dragData, DragDropEffects.Move);
                }
            }
        }
        private void DropTree_DragEnter(object sender, DragEventArgs e)
        {
            if (!getDragEventArgsDataPresent(e))
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private Type[] _draggableTreeViewItems = new Type[] { typeof(LdapSearchTreeViewItem), typeof(TreeViewItem) };

        private bool getDragEventArgsDataPresent(DragEventArgs e)
        {
            foreach (Type t in _draggableTreeViewItems)
            {
                if (e.Data.GetDataPresent(t)) return true;
            }
            return false;
        }

        private TreeViewItem getDragEventArgsData(DragEventArgs e)
        {
            foreach (Type t in _draggableTreeViewItems)
            {
                if (e.Data.GetDataPresent(t))
                {
                    return e.Data.GetData(t) as TreeViewItem;
                }
            }
            return null;
        }

        private void DropTree_Drop(object sender, DragEventArgs e)
        {
            if (getDragEventArgsDataPresent(e))
            {
                TreeViewItem sourceItem = getDragEventArgsData(e);
                TreeViewItem destinationItem =
                    FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                if (destinationItem == null || sourceItem == null || destinationItem == sourceItem) return;

                TreeViewItem pItem = sourceItem.Parent as TreeViewItem;
                if (pItem == null) return;

                if(destinationItem is LdapSearchTreeViewItem)
                {
                    if (((LdapSearchTreeViewItem)destinationItem).SearchOperatorType == LdapSearchExprEnum.Comparison) return;
                }

                bool destinationIsAChild = false;
                TreeViewHelpers.RunTreeviewRecursive(sourceItem, delegate (TreeViewItem item)
                {
                    if(item == destinationItem)
                    {
                        destinationIsAChild = true;
                    }
                });
                if (destinationIsAChild) return;

                pItem.Items.Remove(sourceItem);
                destinationItem.Items.Add(sourceItem);
                destinationItem.IsExpanded = true;
                sourceItem.IsSelected = true;
            }
        }
        private static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
        #endregion

        private void SearchTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            LdapSearchTreeViewItem item = SearchTreeView.SelectedItem as LdapSearchTreeViewItem;
            if (item != null)
            {
                SearchOperatorCombo.IsEnabled = true;
                switch (item.SearchOperatorType)
                {
                    case LdapSearchExprEnum.Comparison: // comparator
                        SearchOperatorCombo.SelectedIndex = 0;
                        break;
                    case LdapSearchExprEnum.AndOperator: // and
                        SearchOperatorCombo.SelectedIndex = 1;
                        break;
                    case LdapSearchExprEnum.OrOperator: // or
                        SearchOperatorCombo.SelectedIndex = 2;
                        break;
                    case LdapSearchExprEnum.NotOperator: // not
                        SearchOperatorCombo.SelectedIndex = 3;
                        break;
                }

                if (item.SearchOperatorType == LdapSearchExprEnum.Comparison)
                {
                    setComparerIsEnabled(true);
                    setComparerBoxes();
                }
                else
                {
                    setComparerIsEnabled(false);
                    if (item.Items.Count > 0)
                    {
                        ((ComboBoxItem)SearchOperatorCombo.Items[0]).IsEnabled = false;
                    }
                    else
                    {
                        ((ComboBoxItem)SearchOperatorCombo.Items[0]).IsEnabled = true;
                    }
                }
            }
            else
            {
                SearchOperatorCombo.IsEnabled = false;
                setComparerIsEnabled(false);
            }
        }

        private void setComparerIsEnabled(bool enabled)
        {
            SearchFieldInput.IsEnabled = enabled;
            SearchComparatorCombo.IsEnabled = enabled;
            SearchValueInput.IsEnabled = enabled;
            SearchFieldInput.Visibility = (enabled ? Visibility.Visible : Visibility.Hidden);
            SearchComparatorCombo.Visibility = (enabled ? Visibility.Visible : Visibility.Hidden);
            SearchValueInput.Visibility = (enabled ? Visibility.Visible : Visibility.Hidden);
        }

        private void setComparerBoxes()
        {
            LdapSearchTreeViewItem item = SearchTreeView.SelectedItem as LdapSearchTreeViewItem;
            if (item == null) return;
            SearchFieldInput.Text = item.SearchFieldName;
            switch (item.SearchComparorType)
            {
                case LdapSearchCmpEnum.equal: // =
                    SearchComparatorCombo.SelectedIndex = 0;
                    break;
                case LdapSearchCmpEnum.approxEqual: // ~=
                    SearchComparatorCombo.SelectedIndex = 1;
                    break;
                case LdapSearchCmpEnum.lessOrEqual: // <=
                    SearchComparatorCombo.SelectedIndex = 2;
                    break;
                case LdapSearchCmpEnum.moreOrEqual: // >=
                    SearchComparatorCombo.SelectedIndex = 3;
                    break;
                case LdapSearchCmpEnum.notEqual: // !=
                    SearchComparatorCombo.SelectedIndex = 4;
                    break;
                case LdapSearchCmpEnum.notApproxEqual: // !~
                    SearchComparatorCombo.SelectedIndex = 5;
                    break;
            }
            SearchValueInput.Text = item.SearchValueName;
        }

        private void SearchOperatorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LdapSearchTreeViewItem item = SearchTreeView.SelectedItem as LdapSearchTreeViewItem;
            if (item == null) return;
            switch (SearchOperatorCombo.SelectedIndex)
            {
                case 0: // comparator
                    if (item.Items.Count <= 0)
                    {
                        item.SearchOperatorType = LdapSearchExprEnum.Comparison;
                        setComparerIsEnabled(true);
                        setComparerBoxes();
                    }
                    break;
                case 1: // and
                    item.SearchOperatorType = LdapSearchExprEnum.AndOperator;
                    setComparerIsEnabled(false);
                    break;
                case 2: // or
                    item.SearchOperatorType = LdapSearchExprEnum.OrOperator;
                    setComparerIsEnabled(false);
                    break;
                case 3: // not
                    item.SearchOperatorType = LdapSearchExprEnum.NotOperator;
                    setComparerIsEnabled(false);
                    break;
            }
            item.SetHeaderText();
        }

        private void SearchComparatorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LdapSearchTreeViewItem item = SearchTreeView.SelectedItem as LdapSearchTreeViewItem;
            if (item == null) return;
            switch (SearchComparatorCombo.SelectedIndex)
            {
                case 0: // =
                    item.SearchComparorType = LdapSearchCmpEnum.equal;
                    break;
                case 1: // ~=
                    item.SearchComparorType = LdapSearchCmpEnum.approxEqual;
                    break;
                case 2: // <=
                    item.SearchComparorType = LdapSearchCmpEnum.lessOrEqual;
                    break;
                case 3: // >=
                    item.SearchComparorType = LdapSearchCmpEnum.moreOrEqual;
                    break;
                case 4: // !=
                    item.SearchComparorType = LdapSearchCmpEnum.notEqual;
                    break;
                case 5: // !~
                    item.SearchComparorType = LdapSearchCmpEnum.notApproxEqual;
                    break;
            }
            item.SetHeaderText();
        }

        private async void SearchValueInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            LdapSearchTreeViewItem item = SearchTreeView.SelectedItem as LdapSearchTreeViewItem;
            if (item == null) return;
            int len = SearchValueInput.Text.Length;
            await Task.Delay(200);
            if (item != SearchTreeView.SelectedItem || len != SearchValueInput.Text.Length) return;

            item.SearchValueName = SearchValueInput.Text;
            item.SetHeaderText();
        }

        private async void SearchFieldInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            LdapSearchTreeViewItem item = SearchTreeView.SelectedItem as LdapSearchTreeViewItem;
            if (item == null) return;
            int len = SearchFieldInput.Text.Length;
            await Task.Delay(200);
            if (item != SearchTreeView.SelectedItem || len != SearchFieldInput.Text.Length) return;

            item.SearchFieldName = SearchFieldInput.Text;
            item.SetHeaderText();
        }

        private void SearchFieldInput_LostFocus(object sender, RoutedEventArgs e)
        {
            Border border = (SearchFieldInputResultStack.Parent as ScrollViewer).Parent as Border;
            if(!border.IsMouseOver)
            {
                border.Visibility = Visibility.Collapsed;
            }
        }

        private void SearchFieldInput_KeyUp(object sender, KeyEventArgs e)
        {
            bool found = false;
            Border border = (SearchFieldInputResultStack.Parent as ScrollViewer).Parent as Border;

            TextBox textBox = sender as TextBox;
            string query = textBox.Text;

            // Clear the list   
            border.Visibility = System.Windows.Visibility.Collapsed;
            SearchFieldInputResultStack.Children.Clear();

            // Add the result   
            foreach (string text in AllowedSearchFields)
            {
                if (text.ToLower().StartsWith(query.ToLower()) && text != query )
                {
                    TextBlock block = new TextBlock();
                    Border textBlockBorder = new Border() { BorderThickness = new Thickness(1)};
                    textBlockBorder.Child = block;

                    // Add the text   
                    block.Text = text;

                    // A little style...   
                    block.Margin = new Thickness(2, 3, 2, 3);
                    block.Cursor = Cursors.Hand;

                    // Mouse events   
                    block.MouseLeftButtonUp += (ss, ee) =>
                    {
                        textBox.Text = (ss as TextBlock).Text;
                        border.Visibility = System.Windows.Visibility.Collapsed;
                    };

                    block.MouseEnter += (ss, ee) =>
                    {
                        TextBlock b = ss as TextBlock;
                        Border bb = (b.Parent as Border);
                        bb.BorderBrush = new SolidColorBrush { Color = SystemColors.MenuHighlightColor };
                        bb.Background = new SolidColorBrush { Color = SystemColors.InactiveBorderColor };
                        b.Background = new SolidColorBrush { Color = SystemColors.InactiveBorderColor };
                    };

                    block.MouseLeave += (ss, ee) =>
                    {
                        TextBlock b = ss as TextBlock;
                        Border bb = (b.Parent as Border);
                        bb.BorderBrush = Brushes.Transparent;
                        bb.Background = Brushes.Transparent;
                        b.Background = Brushes.Transparent;
                    };

                    // Add to the panel   
                    SearchFieldInputResultStack.Children.Add(textBlockBorder);

                    found = true;
                }
            }

            if (found)
            {
                border.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void SynchronizeButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window is SynchronizeWindow)
                {
                    if(((SynchronizeWindow)window).IsExporting)
                    {
                        window.Show();
                        window.Focus();
                        return;
                    }
                    else
                    {
                        window.Close();
                    }
                }
            }


            string search = TreeViewHelpers.GetLdapSearchQuery(SearchTreeView);
            if (search == "")
            {
                MessageBox.Show("Your search is empty!");
                return;
            }

            SynchronizeWindow w = new SynchronizeWindow();
            w.Show();
            w.ExportAdToAventri(search, ldapBaseDn.Text, "api-emea.eventscloud.com", AventriAccountID.Text, AventriToken.Text, AventriProxy.Text, AventriListID.Text);
        }

        private void SaveAsFileClick(object sender, RoutedEventArgs e)
        {
            TreeViewHelpers.TreeViewContextMenu = SearchTreeView.Resources["SearchTreeViewContext"] as ContextMenu;

            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "AD to Aventri Settings (*.ad2ac)|*.ad2ac",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                CheckPathExists = true
            };
            
            if (dialog.ShowDialog().GetValueOrDefault(false))
            {
                TreeViewHelpers.SaveTreeAsFile(
                    new Dictionary<string, string> {
                        {"LdapBaseDn",  ldapBaseDn.Text },
                        {"AventriProxy",  AventriProxy.Text },
                        {"AventriToken",  AventriToken.Text },
                        {"AventriListID",  AventriListID.Text },
                    },
                    SearchTreeView,
                    dialog.FileName
                );

                _currentOpenFile = dialog.FileName;
            }
        }

        private void SaveFileClick(object sender, RoutedEventArgs e)
        {
            TreeViewHelpers.TreeViewContextMenu = SearchTreeView.Resources["SearchTreeViewContext"] as ContextMenu;

            if (_currentOpenFile == null)
            {
                SaveAsFileClick(sender, e);
                return;
            }


            TreeViewHelpers.SaveTreeAsFile(
                new Dictionary<string, string> {
                    {"LdapBaseDn",  ldapBaseDn.Text },
                    {"AventriProxy",  AventriProxy.Text },
                    {"AventriAccountID",  AventriAccountID.Text },
                    {"AventriToken",  AventriToken.Text },
                    {"AventriListID",  AventriListID.Text },
                },
                SearchTreeView,
                _currentOpenFile
            );
        }

        private void LoadFromFileClick(object sender, RoutedEventArgs e)
        {
            TreeViewHelpers.TreeViewContextMenu = SearchTreeView.Resources["SearchTreeViewContext"] as ContextMenu;

            _currentOpenFile = null;
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "AD to Aventri Settings (*.ad2ac)|*.ad2ac",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                CheckPathExists = true
            };

            if (dialog.ShowDialog().GetValueOrDefault(false))
            {
                Dictionary<string, string> settings = new Dictionary<string, string> { };
                TreeViewHelpers.LoadTreeFromFile(
                    settings,
                    SearchTreeView,
                    dialog.FileName
                );

                // set all settings
                foreach(KeyValuePair<string,TextBox> k in new Dictionary<string, TextBox> {
                        {"LdapBaseDn",  ldapBaseDn },
                        {"AventriProxy",  AventriProxy },
                        {"AventriAccountID",  AventriAccountID },
                        {"AventriToken",  AventriToken },
                        {"AventriListID",  AventriListID },
                })
                {
                    if(settings.ContainsKey(k.Key))
                    {
                        k.Value.Text = settings[k.Key];
                    }
                }
                _currentOpenFile = dialog.FileName;
            }
        }

        private void CreditsClick(object sender, RoutedEventArgs e)
        {
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if(window is CreditsWindow)
                {
                    window.Close();
                }
            }

            CreditsWindow w = new CreditsWindow();
            w.Show();
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AventriTokenPwBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AventriTokenPwBox.Visibility = Visibility.Collapsed;
            AventriToken.Visibility = Visibility.Visible;
            AventriToken.Focus();
        }

        private void AventriToken_LostFocus(object sender, RoutedEventArgs e)
        {
            AventriTokenPwBox.Visibility = Visibility.Visible;
            AventriToken.Visibility = Visibility.Collapsed;
        }

        private void AventriToken_TextChanged(object sender, TextChangedEventArgs e)
        {
            AventriTokenPwBox.Password = AventriToken.Text;
        }
    }
}
