using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PlaylistWPF
{
    /// <summary>
    /// Interaktionslogik für SettingsGenre.xaml
    /// </summary>
    public partial class SettingsGenre
    {
        public SettingsGenre()
        {
            InitializeComponent();
            dataGrid1.ItemsSource = null;
            DataGridCheckBoxColumn c1 = new DataGridCheckBoxColumn();
            c1.Header = "";
            c1.Binding = new Binding("Check");
            c1.Width = 30;
            c1.MaxWidth = 30;
            c1.MinWidth = 30;
            dataGrid1.Columns.Add(c1);
            DataGridTextColumn c2 = new DataGridTextColumn();
            c2.Header = "Genres";
            c2.Width = 232;
            c2.MaxWidth = 232;
            c2.MinWidth = 232;
            c2.Binding = new Binding("Genre");
            c2.IsReadOnly = false;
            dataGrid1.Columns.Add(c2);
            dataGrid1.ItemsSource = Functions.AllViewedHiddenGenres;
        }

        private void btnsave_Click(object sender, RoutedEventArgs e)
        {
            Functions.WriteGenresXML();
            Functions.Initialisieren(true);
            Hide();
        }
        private void dataGrid1_CurrentCellChanged(object sender, EventArgs e)
        {
            dataGrid1.Columns[1].IsReadOnly = true;
            try
            {
                GenreItem gi = (GenreItem) dataGrid1.CurrentItem;
                if (gi.Genre != null)
                {
                    gi.Check = !gi.Check;
                    dataGrid1.Items.Refresh();
                }
            }
            catch
            {
                dataGrid1.Columns[1].IsReadOnly = false;
            }
        }

        private void dataGrid1_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {

        }
    }
}
