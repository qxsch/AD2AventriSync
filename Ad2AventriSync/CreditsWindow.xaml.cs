using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class CreditsWindow : Window
    {
        public CreditsWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Paragraph_Click(object sender, MouseButtonEventArgs e)
        {
            Paragraph p = sender as Paragraph;
            if(p != null)
            {
                Process.Start(new ProcessStartInfo((string)p.DataContext));
                e.Handled = true;
            }
        }
    }
}
