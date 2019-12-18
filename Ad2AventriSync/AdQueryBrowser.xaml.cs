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
using System.Windows.Shapes;

namespace Ad2AventriSync
{
    /// <summary>
    /// Interaction logic for AdQueryBrowser.xaml
    /// </summary>
    public partial class AdQueryBrowser : Window
    {
        public AdQueryBrowser(string query, string baseDN)
        {
            InitializeComponent();
            ldapQuery.Text = query;
            ldapBaseDN.Text = baseDN;
            
            ldapSearchResultGrid.Items.Clear();

            AdSearcher ads = new AdSearcher();
            ads.BaseDn = baseDN;
            System.Data.DataView dv = ads.SearchAsDatatable(query).DefaultView;
            Title = Title + " - " + dv.Count + " AD Users";
            ldapSearchResultGrid.ItemsSource = dv;
        }
    }
}
