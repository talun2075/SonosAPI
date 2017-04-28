using System;
using System.ComponentModel;
using System.Windows;
using PlaylistGenerator;

namespace PlaylistWPF
{
    /// <summary>
    /// Interaktionslogik für GeneratePlaylists.xaml
    /// </summary>
    public partial class GeneratePlaylists
    {
        private readonly BackgroundWorker initial = new BackgroundWorker();
        private Boolean readedplaylists;
        bool cancelerror;
        public GeneratePlaylists()
        {
            InitializeComponent();
            initial.DoWork += initial_DoWork;
            initial.RunWorkerCompleted += initial_RunWorkerCompleted;
            initial.ProgressChanged += initial_ProgressChanged;
            initial.WorkerReportsProgress = true;
            initial.RunWorkerAsync();
            prb1.Value = 0;
            lbprb.Content = "Variablen werden ausgelesen.";
        }
        private void initial_DoWork(object sender, DoWorkEventArgs e)
        {
            initial.ReportProgress(0, "Variablen werden ausgelesen");
            bool playlistexist = Playlists.GetPlaylistNames.Count > 0;
            //Alles initialisieren und mit Abfragen bei Fehlern ausführen, wenn kein Autorun gemacht wurde. Autorun wird unten verarbeitet.
            if (Functions.Initialisieren(playlistexist) && !Functions.Autorun)
            {
                if (Playlists.GetPlaylistNames.Count == 0)
                {
                    var resultDialog =
                        MessageBox.Show(
                            "Es wurde keine Wiedergabeliste(n) geladen. Es wird versucht diese aus dem Parameter der Config zu laden.",
                            "Keine Wiedergabeliste geladen.", MessageBoxButton.OKCancel,
                            MessageBoxImage.Information);
                    if (resultDialog == MessageBoxResult.OK)
                    {
                        Playlists.Clear();
                        var res = Playlists.Load(Functions.PlaylistXML);
                        if (res == true)
                        {
                            initial.ReportProgress(0, "Wiedergaben XML wurde geladen.");
                            readedplaylists = true;
                        }
                        else
                        {
                            cancelerror = true;
                            return;
                        }
                    }
                    if (resultDialog == MessageBoxResult.Cancel)
                    {
                        //Abbruch
                        return;
                    }
                }

                initial.ReportProgress(10, Functions.StartDir + " wird nach Musik durchsucht");
                if (Functions.ReadFiles())
                {
                    initial.ReportProgress(20, "Es wurden " + Functions.AllSongs.Count + " Lieder gefunden.");
                   new Playlistwriter(Functions.AllSongs, Functions.PlaylistSavePath, initial, 20,Functions.ChangeMusicPath,Functions.PlaylistClearFolder);
                }
                else
                {
                    var resultDialog =
                        MessageBox.Show(
                            "Beim auslesen Musikdateien ist ein Fehler aufgetreten. Klicken Sie Ok um das Settingsmenü aufzurufen oder Cancel um die Verarbeitung abzubrechen.",
                            "Fehler beim Initialisieren", MessageBoxButton.OKCancel,
                            MessageBoxImage.Information);
                    if (resultDialog == MessageBoxResult.OK)
                    {
                        Settings se = new Settings();
                        se.Owner = Owner;
                        se.Show();
                        //Hide();
                        return;
                    }
                    if (resultDialog == MessageBoxResult.Cancel)
                    {
                        cancelerror = true;
                    }
                }

            }
            else
            {
                //Autorun durchführen.
                if (Functions.Autorun)
                {
                    Playlists.Clear();
                    var res = Playlists.Load(Functions.PlaylistXML);
                    if (res == true)
                    {
                        initial.ReportProgress(0, "Wiedergaben XML wurde geladen.");
                        readedplaylists = true;
                    }
                    else
                    {
                        cancelerror = true;
                        return;
                    }
                    initial.ReportProgress(10, Functions.StartDir + " wird nach Musik durchsucht");
                    if (Functions.ReadFiles())
                    {
                        initial.ReportProgress(20, "Es wurden " + Functions.AllSongs.Count + " Lieder gefunden.");
                        new Playlistwriter(Functions.AllSongs, Functions.PlaylistSavePath, initial, 20,
                            Functions.ChangeMusicPath);
                    }
                    else
                    {
                        Close();
                        Owner.Close();
                    }
                }
                else
                {
                    var resultDialog =
                        MessageBox.Show(
                            "Beim auslesen der Config ist ein Fehler aufgetreten. Bitte öffnen Sie das Settings Menü um die notwendigen Daten zu setzen..",
                            "Fehler beim Initialisieren", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    if (resultDialog == MessageBoxResult.OK)
                    {
                        cancelerror = true;
                    }
                }
            }
        }

        private void initial_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            prb1.Value = e.ProgressPercentage;
            lbprb.Content = e.UserState;
        }
        private void initial_RunWorkerCompleted(object sender,
                                               RunWorkerCompletedEventArgs e)
        {
           // MessageBox.Show("done");
            //lbprb.Content = "Fertig";
            prb1.Value = 100;
            if (Functions.Autorun)
            {
                Close();
                Owner.Close();
            }
            if (readedplaylists)
            {
                ((MainWindow) Owner).ResetDatabinds();
            }
            if (cancelerror)
            {
                lbprb.Content = "Abbruch wegen Fehler";
            }
            else
            {
                Hide();
            }
        }

    }
}
