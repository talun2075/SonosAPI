using System;
using System.Windows;
using System.Windows.Controls;

namespace PlaylistWPF
{
    /// <summary>
    /// Interaction logic for Gelegenheit.xaml
    /// </summary>
    public partial class Gelegenheit
    {
        public Gelegenheit()
        {
            InitializeComponent();
        }


        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string gel = String.Empty;
            foreach (var con in gridgelegenheit.Children)
            {
                var k = con.GetType();
                if (k == typeof (CheckBox))
                {
                    if (((CheckBox)con).IsChecked == true)
                    {
                       if (String.IsNullOrEmpty(gel))
                       {
                           gel = ((CheckBox)con).Content.ToString();
                       }
                       else
                       {
                           gel = gel +","+ ((CheckBox)con).Content;
                       }
                        
                    }
                }
            }
            ((MainWindow) Owner).tbFeld.Text = gel;
            Hide();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string gel = ((MainWindow)Owner).tbFeld.Text;
            if (!String.IsNullOrEmpty(gel))
            {
                if (gel.Contains(","))
                {
                    string[] splitgel = gel.Split(',');
                    foreach (string sg in splitgel)
                    {
                        foreach (var con in gridgelegenheit.Children)
                        {
                            var k = con.GetType();
                            if (k == typeof(CheckBox))
                            {
                                if (((CheckBox)con).Content.ToString() == sg.TrimStart())
                                {
                                    ((CheckBox)con).IsChecked = true;
                                }
                            }
                        }

                    }

                    
                }
                else
                {
                    foreach (var con in gridgelegenheit.Children)
                    {
                        var k = con.GetType();
                        if (k == typeof(CheckBox))
                        {
                            if (((CheckBox)con).Content.ToString() ==gel)
                            {
                                ((CheckBox) con).IsChecked = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
