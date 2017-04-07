using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PlaylistWPF
{
    /// <summary>
    /// Interaction logic for Genres.xaml
    /// </summary>
    public partial class Genres
    {
        public Genres()
        {
            InitializeComponent();
            labelcount.Content = Functions.AllViewedGenres.Count + " von " + Functions.AllGenres +
                                 " Genres werden angezeigt!";
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataGridCheckBoxColumn c1 = new DataGridCheckBoxColumn
            {
                Header = "",
                Binding = new Binding("Check"),
                Width = 30,
                MaxWidth = 30,
                MinWidth = 30
            };
            dataGrid1.Columns.Add(c1);
            DataGridTextColumn c2 = new DataGridTextColumn
            {
                Header = "Genres",
                Width = 232,
                MaxWidth = 232,
                MinWidth = 232,
                Binding = new Binding("Genre"),
                IsReadOnly = true
            };
            dataGrid1.Columns.Add(c2);

            string[] tbgenresplits = {};
            if (tbgenre.Text.Contains(','))
            {
                tbgenresplits = tbgenre.Text.Split(',');
            }
            foreach (GenreItem gi in Functions.AllViewedGenres)
            {
               
                if (tbgenresplits.Length > 0)
                {
                    foreach (string genspl in tbgenresplits)
                    {
                        gi.Check = gi.Genre == genspl.TrimStart();
                        if (gi.Check) break;
                    }
                }
                else
                {
                    gi.Check = gi.Genre == tbgenre.Text;
                    
                }
            }
            dataGrid1.ItemsSource = Functions.AllViewedGenres;
            dataGrid1.CanUserAddRows = false;
            var dataView = dataGrid1.ItemsSource as DataView;
            if (dataView != null) dataView.Sort = "Genre";
        }
        private void btnsave_Click(object sender, RoutedEventArgs e)
        {
            tbgenre.Text = String.Empty;
            int counter = 0;
            string allselectedgenres = String.Empty;
            foreach (GenreItem gi in Functions.AllViewedGenres)
            {
                if (gi.Check)
                {
                    if (counter == 0)
                    {
                        counter = 1;
                        allselectedgenres = allselectedgenres + gi.Genre;
                    }
                    else
                    {
                        allselectedgenres = allselectedgenres +"," +gi.Genre;
                    }
                }
            }
            if (!String.IsNullOrEmpty(allselectedgenres))
            {
                ((MainWindow)Owner).tbFeld.Text = allselectedgenres;
            }
        }

        private void dataGrid1_CurrentCellChanged(object sender, EventArgs e)
        {
           GenreItem gi = (GenreItem)dataGrid1.CurrentItem;
            gi.Check = !gi.Check;
            dataGrid1.Items.Refresh();
        }
    }
}
