using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PlaylistGenerator;

namespace PlaylistWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private TextPopup tpadd;
        private AddField tp;
        //private BindingList<String> PlaylistsList = new BindingList<string>();
        private readonly BindingList<String> conditionGroupOperator = new BindingList<string>();
        private readonly BindingList<String> stringOperatorenGerman = new BindingList<string>();
        private readonly BindingList<String> intOperatorenGerman = new BindingList<string>();
        private readonly BindingList<String> boolOperatorenGerman = new BindingList<string>();
        private readonly BindingList<String> fieldList = new BindingList<string>();
        private readonly BindingList<String> plSortOrder = new BindingList<string>();
        public MainWindow()
        {

            InitializeComponent();
            GenerateOperationsList();
            GenerateFieldList();
            Functions.ReadSettingsXML();
            //Autoload der Playlist
            if(Functions.PlaylistAutoLoad && File.Exists(Functions.PlaylistXML))
            {
                Playlists.Load(Functions.PlaylistXML);
                ResetDatabinds();
            }
        }

        #region Methoden
        #region ADD
        /// <summary>
        /// ClickEventHelper Hinzufügen einer Playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void AddPlaylist(object sender, RoutedEventArgs routedEventArgs)
        {
            AddPlaylist();
        }

        /// <summary>
        /// hinzufügen einer neuen Playlist
        /// </summary>
        private void AddPlaylist()
        {
            tpadd.Visibility = Visibility.Hidden;
            string plvalue = tpadd.tbflex.Text;

            if (Playlists.GetPlaylistNames.Contains(plvalue))
            {
                var resultDialog = MessageBox.Show("Soll die Wiedergabeliste überschrieben werden?",
                                                   "Wiedergabeliste schon vorhanden", MessageBoxButton.YesNoCancel,
                                                   MessageBoxImage.Question);
                switch (resultDialog)
                {
                    case MessageBoxResult.Yes:
                        tpadd.tbflex.Text = String.Empty;
                        int pl = Playlists.GetPlaylistNames.IndexOf(plvalue);
                        Playlists.GetPlaylists[pl].Playlist = plvalue;
                        Playlists.GetPlaylists[pl].PlaylistConditionGroups.Clear();
                        ResetDatabinds();
                        break;
                    case MessageBoxResult.No:
                        tpadd.Visibility = Visibility.Visible;
                        break;


                }
            }
            else
            {
                Playlists.Add(new PlaylistClass(plvalue));
                Functions.PlaylistChanged = true;
                lbPlaylist.ItemsSource = null;
                lbPlaylist.ItemsSource = Playlists.GetPlaylistNames;
                tpadd.tbflex.Text = String.Empty;
                lbPlaylist.SelectedIndex = Playlists.GetPlaylistNames.IndexOf(plvalue);
            }
        }
        /// <summary>
        /// Hinzufgügen einer Conditongroup Wrapper
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void AddConditionGroup(object sender, RoutedEventArgs routedEventArgs)
        {
            AddConditionGroup();
        }
        /// <summary>
        /// Hinzufügen einer Conditiongroup
        /// </summary>
        private void AddConditionGroup()
        {
            var cg = new ConditionObjectGroup { Name = tpadd.tbflex.Text };
            tpadd.tbflex.Text = String.Empty;
            Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups.Add(cg);
            Functions.PlaylistChanged = true;
            lbConditionList.ItemsSource = null;
            lbConditionList.ItemsSource = Playlists.GetPlaylists[lbPlaylist.SelectedIndex].GetConditionGroupNames;
            tpadd.Visibility = Visibility.Hidden;
            lbConditionList.SelectedIndex =
                Playlists.GetPlaylists[lbPlaylist.SelectedIndex].GetConditionGroupNames.IndexOf(cg.Name);
        }
        /// <summary>
        /// Ein Feld in der Feld Liste zufügen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void AddField(object sender, RoutedEventArgs routedEventArgs)
        {
            AddField();

        }
        /// <summary>
        /// Feld Hinzufügen und Fenster entsprechend schließen. 
        /// </summary>
        private void AddField()
        {
            var cg = tp.cbFieldsToChoose.Text;
            ConditionObject k = new ConditionObject {Feld = cg};
            Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[lbConditionList.SelectedIndex].Add(k);
            Functions.PlaylistChanged = true;
            lbFelder.ItemsSource = null;
            lbFelder.ItemsSource =
                Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[lbConditionList.SelectedIndex]
                    .FieldNamesList;
            lbFelder.SelectedIndex = (Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[lbConditionList.SelectedIndex]
                    .FieldNamesList.Count - 1);
            tp.Close();
            tbFeld.Focus();

        }
        #endregion ADD
        #region Edit
        private void EditPlaylist(object sender, RoutedEventArgs routedEventArgs)
        {
            EditPlaylist();
        }

        /// <summary>
        /// Ändern einer neuen Playlist
        /// </summary>
        private void EditPlaylist()
        {
            tpadd.Visibility = Visibility.Hidden;
            string plvalue = tpadd.tbflex.Text;
            Playlists.GetPlaylists[lbPlaylist.SelectedIndex].Playlist = plvalue;
            Functions.PlaylistChanged = true;
            ResetDatabinds();
            tpadd.tbflex.Text = String.Empty;

        }
        private void EditConditionGroup(object sender, RoutedEventArgs routedEventArgs)
        {
            EditConditionGroup();
        }
        /// <summary>
        /// Hinzufügen einer Conditiongroup
        /// </summary>
        private void EditConditionGroup()
        {
            var k = lbConditionList.SelectedIndex;
            Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[k].Name = tpadd.tbflex.Text;
            Functions.PlaylistChanged = true;
            lbConditionList.ItemsSource = null;
            lbConditionList.ItemsSource = Playlists.GetPlaylists[lbPlaylist.SelectedIndex].GetConditionGroupNames;
            tpadd.Visibility = Visibility.Hidden;
            lbConditionList.SelectedIndex = Playlists.GetPlaylists[lbPlaylist.SelectedIndex].GetConditionGroupNames.IndexOf(tpadd.tbflex.Text);
            tpadd.tbflex.Text = String.Empty;
        }

        #endregion Edit


        #region Delete
        /// <summary>
        /// Löschen einer Playlist und alles neu Initialisieren.
        /// </summary>
        private void DeleteSelectedPlaylist()
        {
            Playlists.Remove(Playlists.GetPlaylists[lbPlaylist.SelectedIndex]);
            Functions.PlaylistChanged = true;
            ResetDatabinds();
            if (lbPlaylist.Items.Count > 0)
            {
                lbPlaylist.SelectedIndex = 0;
            }
        }
        /// <summary>
        /// Löschen einer Conditiongroup
        /// </summary>
        private void DeleteSelectedConditiongroup()
        {
            var condition = Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[lbConditionList.SelectedIndex];
            Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups.Remove(condition);
            Functions.PlaylistChanged = true;
            ResetDatabinds();
            lbConditionList.ItemsSource = Playlists.GetPlaylists[lbPlaylist.SelectedIndex].GetConditionGroupNames;
            if (lbConditionList.Items.Count > 0)
            {
                lbConditionList.SelectedIndex = 0;
            }

        }
        /// <summary>
        /// Löschen eines Feldes
        /// </summary>
        private void DeleteSelectedField()
        {
            var selcond = lbConditionList.SelectedIndex;
            var field = Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[lbConditionList.SelectedIndex].ConditionObjectList[lbFelder.SelectedIndex];
            Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[selcond].ConditionObjectList.Remove(field);
            Functions.PlaylistChanged = true;
            ResetDatabinds();
            lbConditionList.ItemsSource = Playlists.GetPlaylists[lbPlaylist.SelectedIndex].GetConditionGroupNames;
            lbConditionList.SelectedIndex = selcond;
            lbFelder.ItemsSource = Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[selcond].FieldNamesList;
            if (lbFelder.Items.Count > 0)
            {
                lbFelder.SelectedIndex = 0;
            }
        }
        #endregion Delete
        #region FieldtypeCheck
        /// <summary>
        /// Prüft ob das übergeben Feld ein Text ist.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        private Boolean IsFieldString(String field)
        {
            Boolean resval = false;
            switch (field)
            {
                case "Artist":
                case "Album":
                case "Lyric":
                case "Pfad":
                case "Komponist":
                case "Gelegenheit":
                case "Geschwindigkeit":
                case "Stimmung":
                case "Titel":
                case "Genre":
                case "Verlag":
                case "Typ":
                    resval = true;
                    break;
            }
            return resval;
        }
        /// <summary>
        /// Prüft ob das übergeben Feld ein Ja/Nein Feld ist.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        private Boolean IsFieldBool(String field)
        {
            Boolean resval = false;
            switch (field)
            {
                case "Aufwecken":
                case "ArtistPlaylist":
                    resval = true;
                    break;
            }
            return resval;
        }
        /// <summary>
        /// Prüft ob das übergeben Feld eine Zahl ist
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        private Boolean IsFieldInt(String field)
        {
            Boolean resval = false;
            switch (field)
            {
                case "Tracknumber":
                case "Jahr":
                case "Bewertung":
                case "BewertungMine":
                    resval = true;
                    break;
            }
            return resval;
        }

        #endregion FieldtypeCheck
        #region LoadSave
        /// <summary>
        /// XML Speichern
        /// </summary>
        private Boolean SaveXML()
        {

            //Save Dialog öffnen
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "Playlists",
                DefaultExt = ".xml",
                Filter = "XML (.xml)|*.xml"
            };
            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                try
                {
                    // Save document
                    Playlists.Save(dlg.FileName);
                    Functions.PlaylistChanged = false;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return true;//Für Abbruchbutton
        }
        /// <summary>
        /// XML Laden.
        /// </summary>
        /// <returns></returns>
        private Nullable<bool> LoadXML()
        {

            //Save Dialog öffnen
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Playlists",
                DefaultExt = ".xml",
                Filter = "XML (.xml)|*.xml"
            };

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Load document
                result = Playlists.Load(dlg.FileName);
            }
            return result;
        }
        /// <summary>
        /// Reset der ItemSources. Wird bei Änderungen an dem Playlists Objekt benötigt, damit die Anzeige wieder funktioniert.
        /// </summary>
        public void ResetDatabinds()
        {
            var selPlaylist = lbPlaylist.SelectedIndex;
            lbPlaylist.ItemsSource = null;
            lbPlaylist.ItemsSource = Playlists.GetPlaylistNames;
            lbConditionList.ItemsSource = null;
            lbFelder.ItemsSource = null;
            lbFelder.Items.Refresh();
            if (Playlists.GetPlaylistNames.Count > 0)
            {
                if (lbPlaylist.Items.Count > 0 && lbPlaylist.Items.Count > selPlaylist)
                {
                    lbPlaylist.SelectedIndex = selPlaylist;
                }
                else
                {
                    lbPlaylist.SelectedIndex = 0;
                }
            }//Ende Playlist
        }
        #endregion LoadSave
        #region Ini
        /// <summary>
        /// Erstellt eine Liste mit den möglichen Feldern.
        /// </summary>
        private void GenerateFieldList()
        {
            fieldList.Add("Album");
            fieldList.Add("Artist");
            fieldList.Add("ArtistPlaylist");
            fieldList.Add("Aufwecken");
            fieldList.Add("Bewertung");
            fieldList.Add("BewertungMine");
            fieldList.Add("Gelegenheit");
            fieldList.Add("Genre");
            fieldList.Add("Geschwindigkeit");
            fieldList.Add("Jahr");
            fieldList.Add("Komponist");
            fieldList.Add("Lyric");
            fieldList.Add("Pfad");
            fieldList.Add("Stimmung");
            fieldList.Add("Titel");
            fieldList.Add("Tracknumber");
            fieldList.Add("Verlag");
            fieldList.Add("Typ");
        }
        /// <summary>
        /// Generiert die Dropdownlisten
        /// </summary>
        private void GenerateOperationsList()
        {
            stringOperatorenGerman.Add("Enthält");
            stringOperatorenGerman.Add("Enthält Nicht");
            stringOperatorenGerman.Add("Beginnt mit");
            stringOperatorenGerman.Add("Ist Gleich");
            stringOperatorenGerman.Add("Ungleich");
            stringOperatorenGerman.Add("Endet mit");
            cbStringOperatoren.ItemsSource = stringOperatorenGerman;

            intOperatorenGerman.Add("Größer");
            intOperatorenGerman.Add("Kleiner");
            intOperatorenGerman.Add("Ist Gleich");
            intOperatorenGerman.Add("Ungleich");
            cbIntOperatoren.ItemsSource = intOperatorenGerman;

            boolOperatorenGerman.Add("Ja");
            boolOperatorenGerman.Add("Nein");
            cbBoolOperatoren.ItemsSource = boolOperatorenGerman;
            foreach (String cp in Enum.GetNames(typeof(CombineOperator)))
            {
                conditionGroupOperator.Add(cp);
            }
            cbConditionGroupOperator.ItemsSource = conditionGroupOperator;

            plSortOrder.Add("Keine");
            plSortOrder.Add("Titel");
            plSortOrder.Add("Interpret");
            plSortOrder.Add("Zufall");
            plSortOrder.Add("Bewertung");
            plSortOrder.Add("Bewertung Mine");
            cbPLSortOrder.ItemsSource = plSortOrder;

        }
        #endregion Ini
        #region GetX
        /// <summary>
        /// Macht aus ENums den entsprechenden String.
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        private String GetGermanStringoperators(FieldOperator so)
        {
            switch (so)
            {
                case FieldOperator.Contains:
                    return "Enthält";
                case FieldOperator.EndWith:
                    return "Endet mit";
                case FieldOperator.StartsWith:
                    return "Beginnt mit";

                case FieldOperator.Equal:
                    return "Ist Gleich";

                case FieldOperator.NonEqual:
                    return "Ungleich";

                case FieldOperator.Bigger:
                    return "Größer";

                case FieldOperator.Smaler:
                    return "Kleiner";

                case FieldOperator.ContainsNot:
                    return "Enthält Nicht";

                case FieldOperator.Yes:
                    return "Ja";

                case FieldOperator.No:
                    return "Nein";

            }
            return "Fehler";
        }
        /// <summary>
        /// Ermittelt den Operator aufgrund des Strings
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        private FieldOperator GetFieldOperatorsbyString(String op)
        {
            switch (op)
            {
                case "Enthält":
                    return FieldOperator.Contains;

                case "Endet mit":
                    return FieldOperator.EndWith;

                case "Beginnt mit":
                    return FieldOperator.StartsWith;

                case "Ist Gleich":
                    return FieldOperator.Equal;

                case "Ungleich":
                    return FieldOperator.NonEqual;

                case "Größer":
                    return FieldOperator.Bigger;

                case "Kleiner":
                    return FieldOperator.Smaler;

                case "Enthält Nicht":
                    return FieldOperator.ContainsNot;

                case "Ja":
                    return FieldOperator.Yes;

                case "Nein":
                    return FieldOperator.No;

            }
            return FieldOperator.Contains;

        }
        private CombineOperator GetOperatorbyString(string co)
        {
            CombineOperator c = CombineOperator.And;
            switch (co)
            {

                case "Or":
                    c = CombineOperator.Or;
                    break;

            }
            return c;

        }
        #endregion GetX
        #endregion Methoden

        #region FormularMethoden
        #region Buttons
        /// <summary>
        /// eine Playlist wird zugefügt.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            tpadd = new TextPopup {Title = "Wiedergabeliste Anlegen", Owner=this};
            tpadd.btnOk.Click += AddPlaylist;
            tpadd.tbflex.KeyDown += AddPlaylistTextBox_PreviewKeyDown;
            tpadd.Show();
            tpadd.tbflex.Focus();
        }
        /// <summary>
        /// Conditongroup wird einer Playlist zugefügt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddConditionGroup_Click(object sender, RoutedEventArgs e)
        {
            tpadd = new TextPopup();
            tpadd.Title = "Feldgruppe Hinzufügen.";
            tpadd.btnOk.Click += AddConditionGroup;
            tpadd.tbflex.KeyDown += AddConditionGroupTextBox_PreviewKeyDown;
            tpadd.Owner = this;
            tpadd.Show();
            tpadd.tbflex.Focus();
        }
        /// <summary>
        /// Feld wird einer Conditongroup zugefügt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddField_Click(object sender, RoutedEventArgs e)
        {
            tp = new AddField();
            tp.Title = "Feld Hinzufügen.";
            tp.btnOK.Click += AddField;
            tp.cbFieldsToChoose.ItemsSource = fieldList;
            tp.cbFieldsToChoose.Focus();
            tp.Owner = this;
            tp.Show();

        }
        /// <summary>
        /// Playlist wird umbenannt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditPlaylist_Click(object sender, RoutedEventArgs e)
        {
            tpadd = new TextPopup();
            tpadd.Title = "Wiedergabeliste Ändern";
            tpadd.btnOk.Click += EditPlaylist;
            tpadd.tbflex.KeyDown += EditPlaylistTextBox_PreviewKeyDown;
            tpadd.tbflex.Text = lbPlaylist.SelectedValue.ToString();
            tpadd.Owner = this;
            tpadd.Show();
            tpadd.tbflex.Focus();

        }
        /// <summary>
        /// Conditongroup wird umbenannt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditConditionGroup_Click(object sender, RoutedEventArgs e)
        {
            tpadd = new TextPopup();
            tpadd.Title = "Bedingunsgruppe Ändern";
            tpadd.btnOk.Click += EditConditionGroup;
            tpadd.tbflex.KeyDown += EditConditionGroupTextBox_PreviewKeyDown;
            tpadd.tbflex.Text = lbConditionList.SelectedValue.ToString();
            tpadd.Owner = this;
            tpadd.Show();
            tpadd.tbflex.Focus();
        }
        /// <summary>
        /// Conditiongroup wird gelöscht
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDeleteConditionGroup_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedConditiongroup();
        }
        /// <summary>
        /// Playlist wird gelöscht
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btndelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedPlaylist();
        }
        /// <summary>
        /// Feld wird gelöscht
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDeleteField_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedField();
        }
        /// <summary>
        /// Wenn es eine Situation ist, wird der Button sichtbar und läd ein entsprechendes Fenster
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnsituation_Click(object sender, RoutedEventArgs e)
        {
            var condition =
                    Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[
                        lbConditionList.SelectedIndex]
                        .ConditionObjectList[lbFelder.SelectedIndex];
            if (condition.Feld == "Stimmung" || condition.Feld == "Geschwindigkeit" || condition.Feld == "Gelegenheit")
            {
                Gelegenheit gel = new Gelegenheit();
                gel.Owner = this;
                if (condition.Feld == "Stimmung")
                {
                    gel.checkBox2.Content = "Wild";
                    gel.checkBox3.Content = "Fröhlich";
                    gel.checkBox4.Content = "Entspannt";
                    gel.checkBox5.Content = "Düster";
                    gel.checkBox6.Content = "Einschläfernd";
                }
                if (condition.Feld == "Geschwindigkeit")
                {
                    gel.checkBox2.Content = "Sehr Langsam";
                    gel.checkBox3.Content = "Langsam";
                    gel.checkBox4.Content = "Moderat";
                    gel.checkBox5.Content = "Schnell";
                    gel.checkBox6.Content = "Sehr Schnell";
                }
                if (condition.Feld == "Gelegenheit")
                {
                    gel.checkBox2.Content = "Party";
                    gel.checkBox3.Content = "Hintergrund";
                    gel.checkBox4.Content = "Romantisch";
                    gel.checkBox5.Content = "Saisonal";
                    gel.checkBox6.Content = "";
                    gel.checkBox6.Visibility = Visibility.Hidden;
                }

                gel.Show();
            }
            if (condition.Feld == "Genre")
            {
                Genres gen = new Genres();
                gen.Owner = this;
                if (!String.IsNullOrEmpty(condition.Wert))
                {
                    gen.tbgenre.Text = condition.Wert;
                }
                gen.Show();
            }
        }
        #endregion Buttons
        #region X-Changed
        /// <summary>
        /// Selection einer Playlist wurde geändert.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var k = ((ListBox)sender).SelectedIndex;
            if (k >= 0)
            {
                PlaylistClass b = Playlists.GetPlaylists[k];
                lbConditionList.ItemsSource = null;
                lbConditionList.ItemsSource = b.GetConditionGroupNames;
                if (lbConditionList.Items.Count > 0)
                {
                    lbConditionList.SelectedIndex = 0;
                }
                btnAddConditionGroup.IsEnabled = true;
                gridconditionlist.Visibility = Visibility.Visible;
                btndelete.IsEnabled = true;
                btnEditPlaylist.IsEnabled = true;
                cbPLSortOrder.SelectedValue =b.Sort;
            }
            else
            {
                lbConditionList.ItemsSource = null;
                btnAddConditionGroup.IsEnabled = false;
                gridconditionlist.Visibility = Visibility.Hidden;
                btndelete.IsEnabled = false;
                btnEditPlaylist.IsEnabled = false;
            }

        }
        /// <summary>
        /// Click auf Condition Group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbConditionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var k = ((ListBox)sender).SelectedIndex;
            if (k >= 0)
            {
                //Bedingungsgruppe ermitteln
                var conditongroup =
                    Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[
                        lbConditionList.SelectedIndex];
                //Namen der Felder hinterlegen
                lbFelder.ItemsSource = conditongroup.FieldNamesList;
                //Operator für ide Felder anzeigen.
                string z = conditongroup.CombineOperator.ToString();
                var d = conditionGroupOperator.IndexOf(z);
                cbConditionGroupOperator.SelectedIndex = d;
                btnAddField.IsEnabled = true;
                lbFelder.Visibility = Visibility.Visible;
                gridlbfelder.Visibility = Visibility.Visible;
                btnDeleteConditionGroup.IsEnabled = true;
                btnEditConditionGroup.IsEnabled = true;
            }
            else
            {
                btnDeleteConditionGroup.IsEnabled = false;
                btnEditConditionGroup.IsEnabled = false;
                btnAddField.IsEnabled = false;
                lbFelder.ItemsSource = null;
                lbFelder.Visibility = Visibility.Hidden;
                gridlbfelder.Visibility = Visibility.Hidden;
            }
            gridStringIntFelder.Visibility = Visibility.Hidden;
        }
        /// <summary>
        /// Click auf ein Feld
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbFelder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var k = ((ListBox)sender).SelectedIndex;
            if (k != -1)
            {
                gridStringIntFelder.Visibility = Visibility.Visible;
                tbFeld.Visibility = Visibility.Visible;
                btnsituation.Visibility = Visibility.Hidden;
                tbFeld.Focus();
                cbBewertungMine.Visibility = Visibility.Hidden;
                var condition =
                    Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[
                        lbConditionList.SelectedIndex]
                        .ConditionObjectList[lbFelder.SelectedIndex];
                if (IsFieldString(condition.Feld))
                {
                    cbStringOperatoren.Visibility = Visibility.Visible;
                    cbIntOperatoren.Visibility = Visibility.Hidden;
                    cbBoolOperatoren.Visibility = Visibility.Hidden;
                    tbFeld.Visibility = Visibility.Visible;
                    lboperator.Visibility = Visibility.Visible;
                    tbFeld.Text = condition.Wert;
                    cbStringOperatoren.SelectedIndex =
                    stringOperatorenGerman.IndexOf(GetGermanStringoperators(condition.Operator));
                    tbhelp.Text = "Mehrfacheinträge durch Komma separieren.\r\nGilt nicht für die Operatoren \"Ist Gleich\"\r\nund \"Ungleich\".";
                    if (condition.Feld == "Gelegenheit" || condition.Feld == "Geschwindigkeit" || condition.Feld == "Stimmung" || condition.Feld == "Genre")
                    {
                        btnsituation.Visibility = Visibility.Visible;
                    }
                }
                if (IsFieldInt(condition.Feld))
                {
                    cbStringOperatoren.Visibility = Visibility.Hidden;
                    cbIntOperatoren.Visibility = Visibility.Visible;
                    cbBoolOperatoren.Visibility = Visibility.Hidden;
                    tbFeld.Visibility = Visibility.Visible;
                    lboperator.Visibility = Visibility.Visible;
                    tbFeld.Text = condition.Wert;
                    tbhelp.Text = String.Empty;
                    cbIntOperatoren.SelectedIndex = intOperatorenGerman.IndexOf(GetGermanStringoperators(condition.Operator));
                    if (cbIntOperatoren.SelectedIndex == -1)
                    {
                        cbIntOperatoren.SelectedIndex = 0;
                    }
                    if (condition.Feld == "Bewertung")
                    {
                        tbhelp.Text = "Wert    0 = Keine Wertung \r\nWert   -1 = Bombe in Mediamonkey \r\nWert  10 = 0,5 Sterne \r\nWert  20 = 1 Stern\r\nWert  30 = 1,5 Sterne \r\nWert  40 = 2 Sterne\r\nWert  50 = 2,5 Sterne \r\nWert  60 = 3 Sterne\r\nWert  70 = 3,5 Sterne \r\nWert  80 = 4 Sterne\r\nWert  90 = 4,5 Sterne \r\nWert  100 = 5 Sterne";
                    }
                    if (condition.Feld == "BewertungMine")
                    {
                        tbFeld.Visibility = Visibility.Hidden;
                        cbBewertungMine.Visibility = Visibility.Visible;
                        try
                        {
                            int bw = 1;
                            if (!String.IsNullOrEmpty(condition.Wert))
                            {
                                bw = Convert.ToInt16(condition.Wert);
                            }
                            cbBewertungMine.SelectedIndex = (bw - 1);
                        }
                        catch
                        {
                            MessageBox.Show(
                                "Beim Konvertieren von String to Int bei diesem Feld ist ein Fehler aufgetreten.");
                        }
                    }
                }
                if (IsFieldBool(condition.Feld))
                {
                    cbBoolOperatoren.Visibility = Visibility.Visible;
                    cbStringOperatoren.Visibility = Visibility.Hidden;
                    cbIntOperatoren.Visibility = Visibility.Hidden;
                    tbFeld.Visibility = Visibility.Hidden;
                    lboperator.Visibility = Visibility.Hidden;
                    cbBoolOperatoren.SelectedIndex = boolOperatorenGerman.IndexOf(GetGermanStringoperators(condition.Operator));
                    if (condition.Operator == FieldOperator.Contains)
                    {
                        cbBoolOperatoren.SelectedIndex = 1;
                    }
                    tbhelp.Text = "Es reicht einen Operator auszuwählen.";
                }
                btnDeleteField.IsEnabled = true;
            }
            else
            {
                gridStringIntFelder.Visibility = Visibility.Hidden;
                btnDeleteField.IsEnabled = false;
            }

        }
        /// <summary>
        /// Operator für Felder wird geändert und entsprechend ins Objekt geschrieben.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbConditionGroupOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[lbConditionList.SelectedIndex].CombineOperator = GetOperatorbyString(cbConditionGroupOperator.SelectedValue.ToString());
        }
        private void cbPLSortOrder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbPLSortOrder.SelectedValue != null)
            {
                Playlists.GetPlaylists[lbPlaylist.SelectedIndex].Sort = e.AddedItems[0].ToString();
                    //cbPLSortOrder.SelectedValue.ToString();
            }
        }
        /// <summary>
        /// Wert eines String/INT Feldes wurde angepasst.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbFeld_TextChanged(object sender, TextChangedEventArgs e)
        {
            var co = Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[lbConditionList.SelectedIndex]
                .ConditionObjectList[lbFelder.SelectedIndex];
            co.Wert = tbFeld.Text;
            if (co.Feld == "Bewertung" && tbFeld.Text == "100" && co.Operator == FieldOperator.Bigger)
            {
                cbIntOperatoren.SelectedValue = "Ist Gleich";
                co.Operator = FieldOperator.Equal;
            }
        }
        /// <summary>
        /// Ein FeldOperator wurde angepasst.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbALLOperatoren_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var k = ((ComboBox)sender);
            if (k.SelectedIndex != -1)
            {
                var co = Playlists.GetPlaylists[lbPlaylist.SelectedIndex].PlaylistConditionGroups[lbConditionList.SelectedIndex]
                                .ConditionObjectList[lbFelder.SelectedIndex];
                co.Operator = GetFieldOperatorsbyString(k.SelectedValue.ToString());
            }
        }
        /// <summary>
        /// Bewertungfeld von Minde wurde geändert.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbBewertungMine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbFeld.Text = String.Empty;
            if (cbBewertungMine.SelectedIndex > -1)
            {
                tbFeld.Text = (cbBewertungMine.SelectedIndex + 1).ToString();
            }
        }
        #endregion X-Changed
        #region Menu
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveXML())
            {
                MessageBox.Show("Beim Speichern der Playlist ist ein Fehler aufgetreten.");
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            var result = LoadXML();
            if (result != null && result == true)
            {
                //Databindung für alle aktualisieren. 
                ResetDatabinds();
            }
            else
            {
                MessageBox.Show("Das XML Dokument konnte nicht geladen werden.");
            }
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            var resultDialog = MessageBox.Show("Sollen wirklich alle Daten gelöscht werden?",
                                                   "Achtung alle Eingaben werden gelöscht.", MessageBoxButton.YesNo,
                                                   MessageBoxImage.Warning);
            if (resultDialog == MessageBoxResult.Yes)
            {
                Playlists.Clear();
                ResetDatabinds();
            }
        }
        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            OpenGeneratePlaylist();
        }
        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void OpenGeneratePlaylist()
        {
            GeneratePlaylists gp = new GeneratePlaylists();
            gp.Owner = this;
            gp.Show();
        }
        public void OpenSettings()
        {
            Settings se = new Settings();
            se.Owner = this;
            se.Show();
        }
        #endregion Menu
        #region KeyPressed
        /// <summary>
        /// AddPlaylist Enter Press
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AddPlaylistTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddPlaylist();
            }
        }
        void EditConditionGroupTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EditConditionGroup();
            }
        }
        void EditPlaylistTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EditPlaylist();
            }
        }
        /// <summary>
        /// AddConditionGroup Enter Press
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AddConditionGroupTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddConditionGroup();
            }
        }
        #endregion KeyPressed
        /// <summary>
        /// Focus aber kein Change vim Index
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbConditionList_GotFocus(object sender, RoutedEventArgs e)
        {
            gridStringIntFelder.Visibility = Visibility.Hidden;
            gridlbfelder.Visibility = Visibility.Visible;
            if (lbConditionList.SelectedIndex >= 0)
            {
                btnDeleteConditionGroup.IsEnabled = true;
                btnEditConditionGroup.IsEnabled = true;
            }
            else
            {
                btnDeleteConditionGroup.IsEnabled = false;
                btnEditConditionGroup.IsEnabled = false;
            }
        }

        #endregion FormularMethoden

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Functions.Autorun)
            {
                OpenGeneratePlaylist();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (Functions.PlaylistChanged)
            {
                MessageBoxResult dlg =  MessageBox.Show("Achtung die Playlist wurde geändert! Soll diese jetzt gespeichert werden?", "Achtung!", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (dlg == MessageBoxResult.Yes)
                {
                    SaveXML();
                }
            }
        }




    }
}
