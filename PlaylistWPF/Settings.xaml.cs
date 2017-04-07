using System;
using System.Windows;
using System.Windows.Forms;

namespace PlaylistWPF
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(Functions.StartDir) || String.IsNullOrEmpty(Functions.PlaylistXML) || String.IsNullOrEmpty(Functions.PlaylistSavePath))
            {
                Functions.Initialisieren(false);
            }
            tbmusicpath.Text = Functions.StartDir;
            tbsavepath.Text = Functions.PlaylistSavePath;
            tbxml.Text = Functions.PlaylistXML;
            cpplaylistautoload.IsChecked = Functions.PlaylistAutoLoad;
            if (Functions.ChangeMusicPath.Contains("|"))
            {
                string[] sp = Functions.ChangeMusicPath.Split(new[] { "|" }, StringSplitOptions.None);
                tbCMPOld.Text = sp[0];
                tbCMPNew.Text = sp[1];
            }
        }

        private void btnsetxml_Click(object sender, RoutedEventArgs e)
        {
            //Save Dialog öffnen
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Playlists"; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "XML (.xml)|*.xml"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                tbxml.Text = dlg.FileName;
            }
        }

        private void btnsetsavepath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fd = new FolderBrowserDialog();
            fd.ShowNewFolderButton = true;
            var result = fd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                tbsavepath.Text = fd.SelectedPath;
            }
        }

        private void btnstartdir_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fd = new FolderBrowserDialog();
            fd.ShowNewFolderButton = true;
            fd.RootFolder = Environment.SpecialFolder.MyComputer;
            var result = fd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                tbmusicpath.Text = fd.SelectedPath;
            }
        }

        private void btnsave_Click(object sender, RoutedEventArgs e)
        {
            Functions.WriteSettingsXML("playlistsavepath", tbsavepath.Text);
            Functions.WriteSettingsXML("MusicPath", tbmusicpath.Text);
            Functions.WriteSettingsXML("playlistxml", tbxml.Text);
            Functions.WriteSettingsXML("PlaylistAutoLoad", cpplaylistautoload.IsChecked.ToString());

            if (!string.IsNullOrEmpty(tbCMPNew.Text) && !string.IsNullOrEmpty(tbCMPOld.Text))
            {
                string cmp = tbCMPOld.Text + "|" + tbCMPNew.Text;
                Functions.WriteSettingsXML("ChangeMusicPath", cmp);
            }

            Functions.Initialisieren(true);
            Hide();
        }

        private void btGenreSet_Click(object sender, RoutedEventArgs e)
        {
            SettingsGenre sg = new SettingsGenre();
            sg.Owner = Owner;
            sg.Show();
        }
    }
}
