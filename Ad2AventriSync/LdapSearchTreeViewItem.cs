using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Ad2AventriSync
{
    class LdapSearchTreeViewItem : TreeViewItem
    {
        public LdapSearchExprEnum SearchOperatorType = LdapSearchExprEnum.AndOperator;

        public string SearchFieldName = "";

        public LdapSearchCmpEnum SearchComparorType = LdapSearchCmpEnum.equal;

        public string SearchValueName = "";

        public string GetSearchString()
        {
            if(SearchOperatorType == LdapSearchExprEnum.Comparison)
            {
                switch (SearchComparorType)
                {
                    case LdapSearchCmpEnum.equal:
                        return "(" + SearchFieldName + "=" + SearchValueName + ")";
                    case LdapSearchCmpEnum.approxEqual:
                        return "(" + SearchFieldName + "~=" + SearchValueName + ")";
                    case LdapSearchCmpEnum.lessOrEqual:
                        return "(" + SearchFieldName + "<=" + SearchValueName + ")";
                    case LdapSearchCmpEnum.moreOrEqual:
                        return "(" + SearchFieldName + ">=" + SearchValueName + ")";
                    case LdapSearchCmpEnum.notEqual:
                        return "(!(" + SearchFieldName + "=" + SearchValueName + "))";
                    case LdapSearchCmpEnum.notApproxEqual:
                        return "(!(" + SearchFieldName + "~=" + SearchValueName + "))";
                    default:
                        return "";
                }
            }

            string search = "";
            foreach (object i in Items)
            {
                LdapSearchTreeViewItem item = i as LdapSearchTreeViewItem;
                if (item == null) continue;
                search += item.GetSearchString();
            }

            switch (SearchOperatorType)
            {
                case LdapSearchExprEnum.AndOperator:
                    return "(&" + search + ")";
                case LdapSearchExprEnum.OrOperator:
                    return "(|" + search + ")";
                case LdapSearchExprEnum.NotOperator:
                    return "(!" + search + ")";
            }
            return "";
        }

        public void SetHeaderText()
        {
            string text = "";
            switch(SearchOperatorType)
            {
                case LdapSearchExprEnum.Comparison:
                    switch(SearchComparorType)
                    {
                        case LdapSearchCmpEnum.equal:
                            text = SearchFieldName + "  =  " + SearchValueName;
                            break;
                        case LdapSearchCmpEnum.approxEqual:
                            text = SearchFieldName + "  ~=  " + SearchValueName;
                            break;
                        case LdapSearchCmpEnum.lessOrEqual:
                            text = SearchFieldName + "  <=  " + SearchValueName;
                            break;
                        case LdapSearchCmpEnum.moreOrEqual:
                            text = SearchFieldName + "  >=  " + SearchValueName;
                            break;
                        case LdapSearchCmpEnum.notEqual:
                            text = "!(" + SearchFieldName + "  =  " + SearchValueName + ")";
                            break;
                        case LdapSearchCmpEnum.notApproxEqual:
                            text = "!(" + SearchFieldName + "  ~=  " + SearchValueName + ")";
                            break;
                        default:
                            text = "Unknwon comparor";
                            break;
                    }
                    break;
                case LdapSearchExprEnum.AndOperator:
                    text = "And";
                    break;
                case LdapSearchExprEnum.OrOperator:
                    text = "Or";
                    break;
                case LdapSearchExprEnum.NotOperator:
                    text = "Not";
                    break;
            }

            StackPanel p = Header as StackPanel;
            if (p == null) return;
            TextBlock b = p.Children[0] as TextBlock;
            if (b == null) return;
            b.Text = text;
        }
    }
}
