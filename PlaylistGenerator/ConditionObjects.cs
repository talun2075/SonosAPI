using System;
using System.Collections.Generic;
using System.Globalization;
using MP3File;

namespace PlaylistGenerator.Conditons
{
    /// <summary>
    /// Die Condition Objekte
    /// </summary>
    public class ConditionObject
    {
        public ConditionObject()
        {
            //ctro
        }
        public ConditionObject(String feld, String wert, FieldOperator so)
        {
            //ctro
            Feld = feld;
            Wert = wert;
            Operator = so;
        }
        #region Eigenschaften
        /// <summary>
        /// Feld, welches überprüft werden soll.
        /// </summary>
        public String Feld { get; set; }
        /// <summary>
        /// Wert, der geprüft wird.
        /// </summary>
        public String Wert { get; set; }
        /// <summary>
        /// Entsprechender Operator für das Feld.
        /// </summary>
        public FieldOperator Operator { get; set; }
        #endregion eigenschaften
    }

    /// <summary>
    /// Enum für die Operatoren AND und OR
    /// </summary>
    public enum CombineOperator
    {
        And,
        Or
    }

    /// <summary>
    /// Operator für den Stringvergleich
    /// </summary>
    public enum FieldOperator
    {
        Contains,
        StartsWith,
        EndWith,
        Equal,
        NonEqual,
        Bigger,
        Smaler,
        ContainsNot,
        Yes,
        No
    }


    /// <summary>
    /// Condition Object Gruppe inkl. der Objekte
    /// </summary>
    public class ConditionObjectGroup
    {
        readonly List<ConditionObject> lcp = new List<ConditionObject>();
        readonly List<String> fieldNames = new List<string>();
        #region Methoden
        /// <summary>
        /// Hinzufügen eines Conditionobjects
        /// </summary>
        /// <param name="co"></param>
        public void Add(ConditionObject co)
        {
            lcp.Add(co);
        }
        /// <summary>
        /// Entfernen eines Condition Objekcts
        /// </summary>
        /// <param name="co"></param>
        /// <returns></returns>
        public Boolean Remove(ConditionObject co)
        {
            return lcp.Remove(co);
        }
        #endregion Methoden
        #region Eigenschaften
        /// <summary>
        /// Name der Gruppe
        /// </summary>
        public String Name { get; set; }
        /// <summary>
        /// Liste der Conditions
        /// </summary>
        public List<ConditionObject> ConditionObjectList { get { return lcp; } }
        /// <summary>
        /// EIne Liste mit den Feldern in den Conditions
        /// </summary>
        public List<String> FieldNamesList
        {
            get
            {
                fieldNames.Clear();
                foreach (ConditionObject co in ConditionObjectList)
                {
                    fieldNames.Add(co.Feld);
                }

                return fieldNames;

            }
        }
        /// <summary>
        /// Operator für alle Conditons in dieser Gruppe.
        /// </summary>
        public CombineOperator CombineOperator { get; set; }

        #endregion Eigenschaften
    }

    static class ConditionCecker
    {
        /// <summary>
        /// Überprüft ob die übergeben ConditionGroup in den Feldern enthalten ist.
        /// </summary>
        /// <param name="cog">Liste der Conditionobjekctgroup</param>
        /// <param name="mp3">Zu prüfender Song</param>
        /// <returns></returns>
        static public Boolean Ceck(ConditionObjectGroup cog, MP3File.MP3File mp3)
        {
            Boolean retval = false;
            bool[] condition = new bool[cog.ConditionObjectList.Count];
            int counter = 0;
            //Für jedes Objekt prüfen, ob die Conditon gegeben ist und setzen.
            foreach (ConditionObject b in cog.ConditionObjectList)
            {
                string checker = String.Empty;

                switch (b.Feld)
                {
                    case "Album":
                        checker = mp3.Album;
                        break;
                    case "Artist":
                        checker = mp3.Artist;
                        break;
                    case "Verlag":
                        checker = mp3.Verlag;
                        break;
                    case "Typ":
                        checker = mp3.Typ;
                        break;
                    case "Bewertung":
                        checker = mp3.Bewertung;
                        break;
                    case "BewertungMine":
                        checker = mp3.BewertungMine;
                        break;
                    case "Gelegenheit":
                        checker = mp3.Gelegenheit.ToString();
                        break;
                    case "Genre":
                        checker = mp3.Genre;
                        break;
                    case "Geschwindigkeit":
                        checker = mp3.Geschwindigkeit.ToString();
                        break;
                    case "Jahr":
                        checker = mp3.Jahr;
                        break;
                    case "Komponist":
                        checker = mp3.Komponist;
                        break;
                    case "Lyric":
                        checker = mp3.Lyric;
                        break;
                    case "Stimmung":
                        checker = mp3.Stimmung.ToString();
                        break;
                    case "Titel":
                        checker = mp3.Titel;
                        break;
                    case "Tracknumber":
                        checker = mp3.Tracknumber.ToString(CultureInfo.InvariantCulture);
                        break;
                    case "Aufwecken":
                        checker = Convert.ToString(mp3.Aufwecken);
                        break;
                    case "ArtistPlaylist":
                        checker = Convert.ToString(mp3.ArtistPlaylist);
                        break;

                }
                /* Wenn das Feld gefunden wurde, wird nun geprüft, welcher Vergleichsoperator genomen wurde und
                 * sollte dieser entsprechen wird die condition auf true gesetzt. 
                  */
                int ch;
                int wert;
                if (b.Wert == null)
                {
                    b.Wert = String.Empty;
                }
                switch (b.Operator)
                {
                    case FieldOperator.Contains:
                        if (!String.IsNullOrEmpty(b.Wert) && b.Wert.Contains(","))
                        {
                            string[] k = b.Wert.Split(',');
                            condition[counter] = false;
                            foreach (string con in k)
                            {
                                if (checker.ToLower().Contains(con.TrimStart().ToLower()))
                                {
                                    condition[counter] = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            condition[counter] = checker.ToLower().Contains(b.Wert.ToLower());
                        }
                        break;
                    case FieldOperator.Equal:
                        condition[counter] = checker == b.Wert;

                        break;
                    case FieldOperator.NonEqual:
                        condition[counter] = checker != b.Wert;
                        break;
                    case FieldOperator.StartsWith:
                        if (!String.IsNullOrEmpty(b.Wert) && b.Wert.Contains(","))
                        {
                            string[] k = b.Wert.Split(',');
                            condition[counter] = false;
                            foreach (string con in k)
                            {
                                if (checker.ToLower().StartsWith(con.TrimStart().ToLower()))
                                {
                                    condition[counter] = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            condition[counter] = !checker.ToLower().StartsWith(b.Wert.ToLower());
                        }
                        break;
                    case FieldOperator.EndWith:
                        if (!String.IsNullOrEmpty(b.Wert) && b.Wert.Contains(","))
                        {
                            string[] k = b.Wert.Split(',');
                            condition[counter] = false;
                            foreach (string con in k)
                            {
                                if (checker.ToLower().EndsWith(con.TrimStart().ToLower()))
                                {
                                    condition[counter] = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            condition[counter] = !checker.ToLower().EndsWith(b.Wert.ToLower());
                        }
                        break;
                    case FieldOperator.ContainsNot:
                        if (!String.IsNullOrEmpty(b.Wert) && b.Wert.Contains(","))
                        {
                            string[] k = b.Wert.Split(',');
                            condition[counter] = true;
                            foreach (string con in k)
                            {
                                if (checker.ToLower().Contains(con.TrimStart().ToLower()))
                                {
                                    condition[counter] = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            condition[counter] = !checker.ToLower().Contains(b.Wert.ToLower());
                        }
                        break;
                    case FieldOperator.No:
                        condition[counter] = checker.ToLower() == "false";
                        break;
                    case FieldOperator.Yes:
                        condition[counter] = checker.ToLower() == "true";
                        break;
                    case FieldOperator.Bigger:
                        try
                        {
                            ch = Convert.ToInt16(checker);
                            wert = Convert.ToInt16(b.Wert);
                            if (ch > wert)
                            {
                                condition[counter] = true;
                            }
                            else
                            {
                                condition[counter] = false;
                            }
                        }
                        catch
                        {
                            condition[counter] = false;
                        }
                        break;
                    case FieldOperator.Smaler:
                        try
                        {
                            ch = Convert.ToInt16(checker);
                            wert = Convert.ToInt16(b.Wert);
                            if (ch < wert)
                            {
                                condition[counter] = true;
                            }
                            else
                            {
                                condition[counter] = false;
                            }
                        }
                        catch
                        {
                            condition[counter] = false;
                        }
                        break;

                }


                counter++;

            }
            //CombineOperator prüfen.
            switch (cog.CombineOperator)
            {
                case CombineOperator.And:
                    foreach (bool c in condition)
                    {
                        if (c == false)
                        {
                            retval = false;
                            break;
                        }
                        retval = true;
                    }
                    break;
                case CombineOperator.Or:
                    foreach (bool c in condition)
                    {
                        if (c)
                        {
                            retval = true;
                            break;
                        }
                    }
                    break;

            }
            return retval;
        }

    }
}
