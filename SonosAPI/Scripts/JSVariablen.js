"use strict";
//Liste mit Allen Sonos Variablen
function SonosVariablen() {
    this.apiDeviceURL = '/sonos/Devices/'; //Url der API der Geräte
    this.apiPlayerURL = '/sonos/Player/'; //URL der API des einzelnen Players
    this.nocoverpfad = 'Images/no-cover.png'; //URL von Bild, wenn kein Cover vorhanden
    this.aktcurpopdown = "leer"; //Hält die Info ob und welcher Song in der Wiedergabeliste das Cover etc. anzeigt
    this.browsefirst = 0; //Wurde das Browseing das erste mal geladen
    this.browseParentID = "A:"; //Dient dazu um beim Durchsuchen einen Pfad nach oben zu gehen
    this.ratingerrorsCount = 0; //Gab es beim Rating Fehler wird dies zum Steuern genommen.
    this.ratingerrorsList = 0; //Steuerung ob Fehlerinformationen angezeigt wurden
    this.ratingonlycurrent = false; //Nur noch vom aktuellen Song die Ratings anzeigen.
    this.allplaylist = new Array(); //Liste aller Wiedergabelisten
    this.urldevice = ""; //Wenn über URL Parameter aufgerufen.
    this.PlaylistLoadError = 0;
    this.savePlaylistInputText = "Wiedergabeliste Speichern als..."; //Placeholder Text für Save/Export der Playlist
    this.exportPlaylistInputText = "Wiedergabeliste Exportieren als...";
    this.exportplaylist = false; //Status ob eine Wiedergabeliste gespeichert oder exportiert werden soll.
    this.updateMusikIndex = false; // Status ob der Musikindex aktualisiert wird. 
    this.setGroupMemberInitialisierung = false; //Prüft, ob die GruppenSet mitglieder neu geladen werden müssen.
    this.getanker = "leer"; //Beim Browsing gesetzt um den Sprunganker zu merken.
    this.getAnkerArt = "leer"; //Merkt sich, ob es sich um eine Interpreten oder eine Playlist handelt.
    this.groupDeviceShowBool = false; //Prüft, ob die DEvice Gruppierung angezeigt wird.
    this.masterPlayer =""; //enthält das Device, welcher für die Gruppenbildung an den Server geschickt wird.
    this.swindowlist = new Array(); //Liste aller Fenster SonosWindow
    this.szindex = 100; //Start z-Index SonosWindow
    this.overlayDVIObject = ""; //Object, welches das Overlay initialisiert hat SonosWindow
    this.selectetdivs = []; //Liste mit Objecten, die zusätzlich über dem Overlay liegen sollen.SonosWindow	
    this.TopologieChangeID=0; //TopologieChange SetTimeout ProzessID, damit nicht doppelt Topologiechange gemacht wird.
    this.GetAktSongInfoTimerID = 0; //GetAktsongInfo Timer ID
    this.metaUse = new Array("Jahr", "Genre", "Pfad", "Komponist", "Verlag", "Album", "Typ", "Kommentar"); //Propertys aus Currenttrack.MP3 die als Details angezeigt werden sollen.
    this.GlobalPlaylistLoaded = false;
    this.eventErrorChangeID = 0;// Sollten Fehler auftreten beim JSSonosEvent.js wird dieser Prozess immer und immer wieder aufgerufen, bis der Fehler weg ist, danach werden die Events wieder gestartet.
    this.eventErrorsSource = "";//Quelle, die den Fehler ursächlich gemeldet hat.
    this.currentplaylistScrolled = false; //Zeigt an, ob ein Anwender manuel gescrollt hat.
    this.VolumeConfirmCounter = 20; //Wenn die Lautstärke erhöht wird, gibt es diesen Schwellenwert ab dem gefragt wird ob die Lautstärke wirklich erhöht werden soll. 
}

//Globale Varibalen auf DOM Objekten
//Sonos DOM Elemente wird als SoDo bi Doc Ready initialisiert.
function SonosDOMObjects() {
    this.aktArtist = $("#Aktartist"); //Currenttrack Artist
    this.aktSongInfo = $("#AktSongInfo"); //Wrapper für aktartist, akttitle, currentmeta
    this.aktTitle = $("#Akttitle"); //Currenttrack Titel
    this.ankerlist = $("#Ankerlist"); //Beim Browsen die Buchstabenliste
    this.artistplSwitch = $(".artistplaylistonoffswitch-checkbox"); //Interpretenplaylist Schalter
    this.audioInButton = $("#AudioIn"); //Button um zu Zeigen, ob AudioIn verfügbar und ggf. aktiv bzw. aktivieren.
    this.aufweckenSwitch = $(".aufweckenonoffswitch-checkbox"); //AufweckenPlaylist Schalter
    this.bewertungWidth = $("#BewertungL"); // beim Currenttrack am Cover die Bewertungsbreite (goldene Sterne)
    this.bewertungStars = $("#BewertungN"); // beim Currenttrack am Cover die Bewertung (graue Sterne)
    this.bodydiv = $("#Bodydiv"); //Primärer Container in dem alles hinterlegt ist.
    this.BewertungsFilterButton = $("#Bewertungsfilter"); //Button im Settings für die Filter der Bewertungen.
    this.browseBackButton = $("#Browseback"); //beim Browsing der Back Button
    this.browseButton = $("#Browse"); //Button um die Browsebox zu öffnen
    this.browse = $("#Browsebox"); //Browsebox, die alles fürs Brwosing hält, wie ankerlist, browseloader, warpper etc.
    this.browseWrapper = $("#Browseboxwrapper"); //Wrapper mit der Ergebnisliste.
    this.browseLoader = $("#BrowseLoader"); //Loader beim Browse
    this.cover = $("#Cover"); //Currentcover
    this.currentBomb = $("#CurrentBomb"); //Currentbomb
    this.currentMeta = $("#CurrentMeta"); //Zeigt mit klick die aktuellen Meta Daten eines Songs an.
    this.currentplaylistwrapper = $("#Currentplaylistwrapper"); //Wrapper für die current Playlist
    this.currentplaylist = $("#Currentplaylist");
    this.currentplaylistclose = $("#CurrentPlaylistClose");
    this.deviceClass = $(".device");//Muss später initialisiert werden, weil es noch keine Klassen gibt.
    this.deviceLoader = $("#Loader"); //Loader für die Devices
    this.devices = $("#Devices"); //Oberste Box für die Player und Zonen
    this.devicesWrapper = $("#DeviceWrapper"); //Wrapper der Player/Zonen
    this.errorlogging = $('<DIV id="Showerrorloging"><DIV id ="ShowerrorlogingWrapper"></DIV></DIV>'); //DIV für das Errorlogging
    this.errorloggingDOM = $('<DIV id="ShowerrorlogingButton" class="mediabuttonring">Log</DIV>'); //DIV für das Errorlogging
    this.errorloggingwrapper = $('#ShowerrorlogingWrapper');//DIV für das Errorlogging
    this.eventError = $("#EventErrorDiv");//Wird benutzt, wenn Fehler bei der Server Kommunikation auftreten.
    this.fadeButton = $("#Fade"); //Fade Mediabutton
    this.filterListBox = $("#Filterlist"); //Filter Box
    this.filterListRatingBar = $("#Filterlist .rating_bar"); //Rating in der FIlter Box
    this.filterListStimmungChilds = $("#FilterStimmungendiv > DIV");
    this.filterListGelegenheitChilds = $("#FilterGelegenheitendiv > DIV");
    this.filterListGeschwindigkeitChilds = $("#FilterGeschwindigkeitendiv > DIV");
    this.filterListAlbumInterpretChilds = $("#Artistplaylistfilter > DIV");
    this.filterListRatingBarBomb = $("#filter_rating_bar_bomb");
    this.filterListButton = $("#Bewertungsfilter");
    this.gelegenheitenChildren = $("#Gelegenheitendiv > DIV");
    this.globalPlaylistLoader = $("#GlobalPlaylistLoader"); //Loader für die gespeicherten Wiedergabelisten
    this.groupDeviceShow = $("#GroupDeviceShow"); //Button um die Gruppierung zu aktivieren und einzelne Player zu Pausieren bzw. abzuspielen.
    this.geschwindigkeitChildren = $("#Geschwindigkeitendiv > DIV");
    this.labelVolume = $("#LabelVolume"); //Label unterhalb des Volume Sliders um die Lautstärke anzuzeigen.
    this.lyricButton = $("#Lyric"); //LyricButton
    this.lyric = $("#Lyricbox"); //Lyric Box
    this.lyricWrapper = $("#Lyricboxwrapper");//Wrapper in der Lyric Box
    this.lyricsPlaylist = $("#LyricPlaylist"); //Lyric Box aus Playlist
    this.multiVolume = $("#MultiVolume"); //Box mit der Lautstärke, falls mehr als ein Player in Zone
    this.musikIndex = $("#MusikIndex"); //Musik Index Box
    this.musikIndexCheck = $("#MIUCheck"); //Gibt Feedback, wenn Index Komplett
    this.musikIndexLoader = $("#MusikIndexLoader"); //Loader für den Musikindex
    this.MuteButton = $("#SetMute");//Mediabutton
    this.nextButton = $("#Next");//Mediabutton
    this.nextcover = $("#Nextcover"); //Nextsong Cover
    this.nextSongWrapper = $("#NextSongWrapper"); //Warpper für den Netxsong
    this.nextTitle = $("#Nextsong"); //titel für den NextSong
    this.onlyCurrentSwitch = $(".curonoffswitch-checkbox"); //Nur Currentrating anzeigen?
    this.overlay = $("#Overlay"); //Wenn beim SonosWindows ein Overlay benötigt wird.
    this.playButton = $("#Play"); //Mediabutton
    this.playlistCount = $("#PlaylistCount");//Playlist Box
    this.playlistAkt = $("#PlaylistCountAkt"); //Aktuelle Titel Nummer
    this.playlistTotal = $("#PlaylistCountTotal");//Gesamtanzahl
    this.playListLoader = $("#PlaylistLoader"); //Loader der currentplaylist
    this.playlistwrapper = $("#Playlistwrapper"); //Wrapper beim Container für alle Playlisten
    this.prevButton = $("#Pre");//Mediabutton
    this.ratingCheck = $("#RatingCheck"); //Feedback, wenn Rating erfolgreich durchgeführt
    this.ratingErrorList = $("#RatingerrorsList"); //Wenn beim Rateing ein Fehlerauftritt auf dem Server, dann wird diese liste Gefüllt.
    this.ratingErrors = $("#Ratingerrors"); //Box mit der Liste der Errors.
    this.ratingBomb = $("#rating_id_bomb");//bombe in der Ratingliste
    this.ratingFilterRatingBarComination = $("#Ratinglist .rating_bar, #Filterlist .rating_bar"); //Wird zum Animieren beim Resize genutzt. Abstände zwischen den Sternen
    this.ratingListBox = $("#Ratinglist"); //Liste für die Bewertung
    this.ratingListRatingBar = $("#Ratinglist .rating_bar"); //Aus der Ratingliste die Rating Bar
    this.ratingMineSelector = $("#RatingMineSelector"); //Selection für Mine Rating
    this.repeatButton = $("#Repeat");//Mediabutton
    this.runtimeCurrentSong = $("#CurrentSongRuntime"); //Box mit der Laufzeit
    this.runtimeRelTime = $("#CurrentSongRuntimeRelTime"); // Abgelaufene Zeit
    this.runtimeDuration = $("#CurrentSongRuntimeDuration"); //Gesamtzeit
    this.runtimeSlider = $("#Slider"); //Slider für die Laufzeit
    this.saveExportPlaylistSwitch = $(".onoffswitch-checkbox"); //Schalter der definiert ob Playlisten Exportiert oder gespeichert werden sollen.
    this.saveQueue = $("#SaveQueue"); //Inputfeld mit dem Namen der Wiedergabeliste
    this.saveQueueLoader = $("#SaveQueueLoader"); //Animation, wenn Playlist gespeichert bzw. exportiert werden soll.
    this.setGroupMembers = $("#SetGroupMembers"); //Box mit allen Playern um auszuwählen, wer mit wem zusammengehört.
    this.settingsBox = $("#Settingsbox"); //Inhalt der Settings DIV
    this.settingsbutton = $("#Settings"); //Settingsbutton
    this.settingsClosebutton = $("#Settingsclose"); //Settingsbutton
    this.BrowseClosebutton = $("#Browseclose");
    this.shuffleButton = $("#Shuffle");//Mediabutton
    this.sleepMode = $("#SleepMode"); //Box für Sleepmode
    this.sleepModeButton = $("#SleepModeButton"); //Button Sleepmode
    this.sleepModeSelection = $("#SleepModeSelection"); //Auswahl mit Zeiten
    this.sleepModeState = $("#SleepModeState"); //Text mit dem Status
    this.suggestionInput = $('#Suggestion'); //Enthält das Inputfeld, welches den Namen der zu speichernden/exportierenden Playlist enthält
    this.stimmungenChildren = $("#Stimmungendiv > DIV"); //Stimmungs DIV Kinder Elemente
    this.volumeSlider = $("#Volume");//Slider mit der Lautstärke
    this.debug = $("#Debug");//Debug Button
    //Wenn Fehler als Log angezeigt werden sollen.
    this.SetErrorLogging = function() {
        this.errorlogging.appendTo(this.bodydiv);
        this.errorloggingDOM.appendTo(this.bodydiv);
    };
}
//} Variablen Verarbeitung