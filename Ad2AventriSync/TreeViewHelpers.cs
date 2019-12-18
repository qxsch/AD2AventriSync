using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Ad2AventriSync
{
    class TreeViewHelpers
    {

        public static ContextMenu TreeViewContextMenu;

        private static string TreeViewItemToJson(LdapSearchTreeViewItem item)
        {
            if (item.SearchOperatorType == LdapSearchExprEnum.Comparison)
            {
                return 
                    "{ \"type\": " + JsonConvert.SerializeObject(item.SearchOperatorType) + "," +
                    " \"field\": " + JsonConvert.SerializeObject(item.SearchFieldName) + ", " +
                    " \"cmp\": " + JsonConvert.SerializeObject(item.SearchComparorType) + ", " +
                    " \"value\": " + JsonConvert.SerializeObject(item.SearchValueName) +
                    " }";
            }

            string json = "";
            foreach(object i in item.Items)
            {
                LdapSearchTreeViewItem citem = i as LdapSearchTreeViewItem;
                if (citem == null) continue;
                json += ", " + TreeViewItemToJson(citem);
            }
            if (json.Length > 0) json = json.Substring(2);
            return "{ \"type\": " + JsonConvert.SerializeObject(item.SearchOperatorType) + ", \"items\": [ " + json +  " ]}";
        }

        public static void SaveTreeAsFile(Dictionary<string,string>settings, TreeView tview, string filename)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
            {
                file.WriteLine("{");

                file.Write(JsonConvert.SerializeObject("Ad2AventriSyncVersion") + ": ");
                file.WriteLine(JsonConvert.SerializeObject((float)1) + ", ");

                file.Write(JsonConvert.SerializeObject("Settings") + ": ");
                file.WriteLine(JsonConvert.SerializeObject(settings) + ",");

                string json = "";
                foreach (object i in (tview.Items[0] as TreeViewItem).Items)
                {
                    LdapSearchTreeViewItem citem = i as LdapSearchTreeViewItem;
                    if (citem == null) continue;
                    json += ", " + TreeViewItemToJson(citem);
                }
                if (json.Length > 0) json = json.Substring(2);
                file.WriteLine("\"LdapQuery\": [" + json + " ]");

                file.Write("}");
            }
        }


        private static TreeViewItem CreateSearchTreeMenuItem(LdapSearchExprEnum op, string field, LdapSearchCmpEnum cmp, string value)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Visibility = Visibility.Visible };
            /*sp.Children.Add(new Image
            {
                Source = new BitmapImage(new Uri(img, UriKind.Relative)),
                Height = 20,
                Width = 20
            });*/
            sp.Children.Add(new TextBlock { Text = "" });
            LdapSearchTreeViewItem item = new LdapSearchTreeViewItem
            {
                Header = sp,
                ContextMenu = TreeViewContextMenu,
                SearchOperatorType = op,
                SearchFieldName = field,
                SearchComparorType = cmp,
                SearchValueName = value,
                IsExpanded = true
            };
            item.SetHeaderText();
            return item;
        }

        private static void JsonToTreeViewItem(dynamic json, TreeViewItem item)
        {


            if(json.type == LdapSearchExprEnum.Comparison)
            {
                item.Items.Add(CreateSearchTreeMenuItem(
                    LdapSearchExprEnum.Comparison,
                    (string)json.field,
                    (LdapSearchCmpEnum)json.cmp,
                    (string)json.value
                ));
            }
            else
            {
                TreeViewItem pitem = CreateSearchTreeMenuItem(
                    (LdapSearchExprEnum)json.type,
                    "",
                    0,
                    ""
                );
                item.Items.Add(pitem);
                for (int i = 0; i < json.items.Count; i++)
                {
                    JsonToTreeViewItem(json.items[i], pitem);
                }
            }
        }

        public static void LoadTreeFromFile(Dictionary<string, string> settings, TreeView tview, string filename)
        {
            using (System.IO.StreamReader file = new System.IO.StreamReader(filename))
            {
                dynamic def = JObject.Parse(file.ReadToEnd());
                if (def.Ad2AventriSyncVersion < 1 && def.Ad2AventriSyncVersion >= 2) return;
                foreach(JProperty p in def.Settings)
                {
                    settings[p.Name] = p.Value.ToString();
                }
                TreeViewItem root = tview.Items[0] as TreeViewItem;
                root.Items.Clear();
                root.IsExpanded = true;
                for (int i = 0; i < def.LdapQuery.Count; i++)
                {
                    JsonToTreeViewItem(def.LdapQuery[i], root);
                }
                // JsonToTreeViewItem(p, tview.Items[0] as TreeViewItem)
            }
        }

        public static bool TreeviewItemHasParent(TreeViewItem child, FrameworkElement parent)
        {
            FrameworkElement el = child as FrameworkElement;
            while(el.Parent != null)
            {
                if(el.Parent == parent)
                {
                    return true;
                }
                el = el.Parent as FrameworkElement;
            }
            return false;
        }

        private static LdapSearchTreeViewItem CloneRecursive(LdapSearchTreeViewItem selectedItem)
        {
            if (selectedItem == null) return null;
            LdapSearchTreeViewItem newItem = CreateSearchTreeMenuItem(selectedItem.SearchOperatorType, selectedItem.SearchFieldName, selectedItem.SearchComparorType, selectedItem.SearchValueName) as LdapSearchTreeViewItem;
            if (selectedItem.Items.Count > 0)
            {
                foreach (object i in selectedItem.Items)
                {
                    LdapSearchTreeViewItem ci = CloneRecursive(i as LdapSearchTreeViewItem);
                    if(ci != null)
                    {
                        newItem.Items.Add(ci);
                    }
                }
            }
            return newItem;
        }

        public static void CloneTreeview(TreeView tview)
        {
            LdapSearchTreeViewItem selectedItem = tview.SelectedItem as LdapSearchTreeViewItem;
            if (selectedItem == null) return;
            TreeViewItem parent = selectedItem.Parent as TreeViewItem;
            if (parent == null) return;
            LdapSearchTreeViewItem newItem = CreateSearchTreeMenuItem(selectedItem.SearchOperatorType, selectedItem.SearchFieldName, selectedItem.SearchComparorType, selectedItem.SearchValueName) as LdapSearchTreeViewItem;
            if(selectedItem.Items.Count > 0)
            {
                foreach(object i in selectedItem.Items)
                {
                    LdapSearchTreeViewItem ci = CloneRecursive(i as LdapSearchTreeViewItem);
                    if (ci != null)
                    {
                        newItem.Items.Add(ci);
                    }
                }
            }
            parent.Items.Add(newItem);
        }

        public static void SanitizeTreeview(TreeView tview)
        {
            List<LdapSearchTreeViewItem> itemsToDelete = new List<LdapSearchTreeViewItem>();
            foreach (object i in tview.Items)
            {
                RunTreeviewRecursive(i, delegate (TreeViewItem item)
                {
                    LdapSearchTreeViewItem litem = item as LdapSearchTreeViewItem;
                    if (litem == null) return;
                    if (litem.SearchOperatorType != LdapSearchExprEnum.Comparison && litem.Items.Count == 0)
                    {
                        itemsToDelete.Add(litem);
                    }
                });
            }
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

        public static void RunTreeviewRecursive(object o, Action<TreeViewItem> a)
        {
            TreeViewItem item = o as TreeViewItem;
            if (item == null) return;
            a(item);
            foreach (object i in item.Items)
            {
                RunTreeviewRecursive(i, a);
            }
        }

        public static string GetLdapSearchQuery(TreeView tview)
        {
            SanitizeTreeview(tview);
            if ((tview.Items[0] as TreeViewItem).Items.Count == 0)
            {
                return "";
            }
            string search = "(&(objectClass=user)";
            foreach (object i in (tview.Items[0] as TreeViewItem).Items)
            {
                LdapSearchTreeViewItem item = i as LdapSearchTreeViewItem;
                if (i == null) continue;
                search += item.GetSearchString();
            }
            search += ")";
            return search;
        }
    }
}
