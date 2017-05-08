"use strict";
//Cordinated Players
function SonosDevice(uuid, name, volume) {
    this.UUID = uuid;
    this.Name = name;
    this.Volume = volume;
}
//Einzelne SonosZone
function SonosZone(uuid, name) {
    var _cordinatedPlayer = [];
    this.ZoneUUID = uuid;
    this.ZoneName = name;
    this.GetPlayerChangeEventIsRunning = false; //Prüfung, ob ein Player gerade aktualisiert wird.
    this.GetPlayerChangeEventIsRunningCounter = 0; //Bei GettopologieChange können Fehler passieren, daher Counter mitgeben.
    this.CheckPlayerChangeEvent = function() {
        if (this.GetPlayerChangeEventIsRunningCounter < 5) {
            this.GetPlayerChangeEventIsRunningCounter++;
        } else {
            this.GetPlayerChangeEventIsRunning = false;
            this.GetPlayerChangeEventIsRunningCounter = 0;
        }
    };
    this.GroupVolume = 0;
    this.GroupVolumeDoms = [];
    this.RenderGroupVolume = function(source) {
        console.log(source);
        if (_cordinatedPlayer.length > 0) {
            var oldValue = $("#MultivolumesliderAll").slider("value");
            if (!isNaN(oldValue) && oldValue !== this.GroupVolume) {
                $("#MultivolumesliderAll").slider({ value: this.GroupVolume });
                $("#MultivolumeAllNumber").html(this.GroupVolume);
            }
            oldValue = $("#MultivolumesliderPrimary").slider("value");
            if (!isNaN(oldValue) && oldValue !== this.CordinatorVolume) {
                $("#MultivolumesliderPrimary").slider({ value: this.CordinatorVolume });
                $("#MultivolumePrimaryNumber").html(this.CordinatorVolume);
                SoDo.labelVolume.html(this.CordinatorVolume);
                SoDo.volumeSlider.slider({ value: this.CordinatorVolume });
            }
            //_cordinatedPlayer.forEach(function(element) {
            //    oldValue = $("#Multivolumeslider_" + element.UUID).slider("value");
            //    if (!isNaN(oldValue) && oldValue !== element.Volume) {
            //        $("#Multivolumeslider_" + element.UUID).slider({ value: element.Volume });
            //        $("#MultivolumesliderVolumeNumber_" + element.UUID).html(element.Volume);
            //    }
            //});
        }
    };
    this.RatingFilter = "INITIAL";
    this.SetRatingFilter = function(s) {
        if (s === null) return;
        this.RatingFilter = s;
        if (this.ActiveZone) {
            this.RenderRatingFilter();
        }
    };
    this.ChangeRatingFilter = function(typ, wert) {
        //nimmt den übergeben Wert und setzt diesen an den Server sowie im Objekt hier.     
        var changed = false;
        switch (typ) {
            case "Reset":
                this.RatingFilter.Rating = -2;
                this.RatingFilter.Stimmung = "unset";
                this.RatingFilter.Geschwindigkeit = "unset";
                this.RatingFilter.Gelegenheit = "unset";
                this.RatingFilter.AlbpumInterpretFilter = "unset";
                changed = true;
                break;
            case "Rating":
                if (this.RatingFilter.Rating !== wert) {
                    this.RatingFilter.Rating = wert;
                    changed = true;
                }
                break;
            case "Stimmung":
                if (this.RatingFilter.Stimmung !== wert) {
                    this.RatingFilter.Stimmung = wert;
                    changed = true;
                }
                break;
            case "Geschwindigkeit":
                if (this.RatingFilter.Geschwindigkeit !== wert) {
                    this.RatingFilter.Geschwindigkeit = wert;
                    changed = true;
                }
                break;
            case "Gelegenheit":
                if (this.RatingFilter.Gelegenheit !== wert) {
                    this.RatingFilter.Gelegenheit = wert;
                    changed = true;
                }
                break;
            case "AlbpumInterpretFilter":
                if (this.RatingFilter.AlbpumInterpretFilter !== wert) {
                    this.RatingFilter.AlbpumInterpretFilter = wert;
                    changed = true;
                }
                break;
        }
        if (changed === true) {
            if (this.ActiveZone) {
                this.RenderRatingFilter();
            }
            SonosAjax("SetRatingFilter", this.RatingFilter);
        }

    };
    this.RenderRatingFilter = function() {
            SoDo.filterListRatingBar.removeClass("rating_bar_aktiv");
            SoDo.filterListGelegenheitChilds.removeClass("selected");
            SoDo.filterListGeschwindigkeitChilds.removeClass("selected");
            SoDo.filterListStimmungChilds.removeClass("selected");
            SoDo.filterListAlbumInterpretChilds.removeClass("selected");
            var def = true;
            if (SoDo.filterListRatingBarBomb.hasClass("rating_bar_aktiv")) {
                SoDo.filterListRatingBarBomb.removeClass("rating_bar_aktiv");
            }
            if (this.RatingFilter.Rating > -2) {
                if (this.RatingFilter.Rating === -1) {
                    SoDo.filterListRatingBarBomb.addClass("rating_bar_aktiv");
                } else {
                    $("#filter_rating_bar_" + this.RatingFilter.Rating).addClass("rating_bar_aktiv");
                }
                def = false;
            }
            if (this.RatingFilter.Stimmung !== 6) {
                $("#Filterstimmung_" + this.RatingFilter.Stimmung).addClass("selected");
                def = false;
            }
            if (this.RatingFilter.Gelegenheit !== 5) {
                $("#Filtergelegenheit_" + this.RatingFilter.Gelegenheit).addClass("selected");
                def = false;
            }
            if (this.RatingFilter.Geschwindigkeit !== 6) {
                $("#Filtergeschwindigkeit_" + this.RatingFilter.Geschwindigkeit).addClass("selected");
                def = false;
            }
            if (this.RatingFilter.AlbpumInterpretFilter !== "unset") {
                $("#AlbumArtist" + this.RatingFilter.AlbpumInterpretFilter).addClass("selected");
                def = false;
            }
        if (def === false) {
            if (!SoDo.filterListButton.hasClass("akt")) {
                SoDo.filterListButton.addClass("akt");
            }
        } else {
            if (SoDo.filterListButton.hasClass("akt")) {
                SoDo.filterListButton.removeClass("akt");
            }
        }
    };
    this.BaseURL = "INITIAL";
    this.SetBaseurl = function(s) {
        if (s === null || s === "") {
            var retval = SonosAjax("BaseUrl", this.ZoneUUID);
            retval.success(function(data) {
                if (typeof data !== "undefined" && data !== "") {
                    s = data;
                }
            });
        }
        this.BaseURL = s;
    };
    this.LastChange = 0;
    this.HasAudioIn = "INITIAL";
    this.SetHasAudioIn = function (s) {
        this.HasAudioIn = s;
        if (this.ActiveZone === true) {
            this.RenderAudioIn();
        }
    };
    this.RenderAudioIn = function() {
        if (this.HasAudioIn === true) {
            if (SoDo.audioInButton.is(":hidden")) {
                SoDo.audioInButton.show();
            }
            if (this.CurrentTrack.Stream === true && (this.CurrentTrack.StreamContent === "Audio Eingang" || this.CurrentTrack.Title === "Heimkino")) {
                if (!SoDo.audioInButton.hasClass("akt")) {
                    SoDo.audioInButton.addClass("akt");
                }
            } else {
                if (SoDo.audioInButton.hasClass("akt")) {
                    SoDo.audioInButton.removeClass("akt");
                }
            }
        } else {
            if (SoDo.audioInButton.is(":visible")) {
                SoDo.audioInButton.hide();
            }
        }
    };
    this.SetLastChange = function (_lastChange) {
        if (_lastChange === null || typeof _lastChange === "undefined") {
            _lastChange = 0;
        }
        this.LastChange = _lastChange;
    };
    this.SleepMode = "INITIAL";
    this.SetSleepMode = function (s) {
        if (s !== this.SleepMode) {
            if (s === null) {
                return;
            }
            if (this.ActiveZone === true) {
                //Wert reinschreiben.
                if (s !== "" && s !== "aus" && s !== "00:00:00") {
                    if (!SoDo.sleepModeButton.hasClass("aktiv")) {
                        SoDo.sleepModeButton.addClass("aktiv");
                    }
                    if (SoDo.sleepModeState.text() !== s) {
                        SoDo.sleepModeState.text(s);
                    }
                } else {
                    if (SoDo.sleepModeButton.hasClass("aktiv")) {
                        SoDo.sleepModeButton.removeClass("aktiv");
                    }
                    if (SoDo.sleepModeState.text() !== "") {
                        SoDo.sleepModeState.text("");
                    }
                }
            }
            this.SleepMode = s;
        }
    };
    this.Playlist = new SonosPlaylist();
    this.PlaylistLoader = false;
    this.RenderPlaylist = function(source) {
        SonosLog("RenderPlaylist Callby:" + source);
        this.RenderNextTrack("RenderPlaylist");
        if (this.Playlist.CheckToRender()) {
            if (this.CurrentTrack.StreamContent === "Dienst" || this.CurrentTrack.StreamContent === "Apple") {
                this.Playlist.RenderPlaylist(false);
            } else {
                this.Playlist.RenderPlaylist(this.CurrentTrack.Stream);
            }
        } else {
            //Hier nun Stream Prüfen
            if (this.CurrentTrack.Stream === true && this.CurrentTrack.StreamContent !== "Dienst" && this.CurrentTrack.StreamContent !== "Apple") {
                if ($(".currentplaylist").length > 0) {
                    $(".currentplaylist").remove();
                }
            }
        }
        this.RenderTrackTime();
        this.RenderPlaylistCounter("Renderplaylist");
    };
    this.RenderPlaylistCounter = function(source) {
        SonosLog("RenderPlaylistCounter Callby:" + source);
        if (this.Playlist.IsEmpty === true || (this.CurrentTrack !== null && this.CurrentTrack.Stream === true && this.CurrentTrack.StreamContent !== "Dienst" && this.CurrentTrack.StreamContent !== "Apple")) {
            if (SoDo.playlistCount.is(":visible")) {
                SoDo.playlistCount.hide();
            }
        } else {
            if (SoDo.playlistCount.is(":hidden")) {
                SoDo.playlistCount.show();
            }
            if (parseInt(SoDo.playlistAkt.html()) !== this.CurrentTrackNumber) {
                SoDo.playlistAkt.html(this.CurrentTrackNumber);
            }
            if (parseInt(SoDo.playlistTotal.html()) !== this.NumberOfTracks) {
                SoDo.playlistTotal.html(this.NumberOfTracks);
            }
        }

    };
    this.SetPlaylist = function(v, source) {
        SonosLog("SetPlaylist Callby:" + source);
        if (this.PlaylistLoader === true) {
            return; //keine Zwei Aufrufe hintereinander.
        }
        this.PlaylistLoader = true;
        //v = gibt an ob auch gleich gerendert werden soll.
        if (v === true) {
            SoDo.playListLoader.slideDown();
        }
        var cuuid = this.ZoneUUID;
        var request = SonosAjax("GetPlayerPlaylist", "", cuuid);
        request.success(function(data) {
            if (typeof SonosZones[cuuid] === "undefined") {
                console.warn("Fehler Player scheint es nicht mehr zu geben RINCON:" + cuuid);
                return;
            }
            SoVa.PlaylistLoadError = 0;
            SonosZones[cuuid].Playlist.Playlist = data.PlayListItems;
            if (data.TotalMatches !== null && data.TotalMatches !== 0) {
                var tm = data.TotalMatches;
                if (SonosZones[cuuid].NumberOfTracks !== tm && tm !== 0) {
                    SonosZones[cuuid].SetNumberOfTracks(tm);
                }
            } else {
                if (data.PlayListItems.length === 1 && data.PlayListItems[0].Artist === "Leer" && data.PlayListItems[0].Album === "Leer" && data.PlayListItems[0].Title === "Leer") {
                    SonosZones[cuuid].Playlist.IsEmpty = true;
                }
            }
            if (v === true && SonosZones[cuuid].Playlist.CheckToRender()) {
                SonosZones[cuuid].RenderPlaylist("SonosZone:SetPlaylist");
            } else {
                if (SoDo.playListLoader.is(":visible")) {
                    SoDo.playListLoader.slideUp();
                }
            }
            SonosZones[cuuid].PlaylistLoader = false;
        });
        request.fail(function(jqXHR) {
            if (SoVa.PlaylistLoadError < 5) {
                SonosZones[cuuid].PlaylistLoader = false;
                SonosZones[cuuid].SetPlaylist(true, "SetPlaylistRequestFAil");
                SoVa.PlaylistLoadError++;
            } else {
                alert("Beim laden der Aktion:SetPlaylist ist nach dem " + SoVa.PlaylistLoadError + " Versuch folgender Fehler aufgetreten:" + jqXHR.statusText);
                SoVa.PlaylistLoadError = 0;
            }
        });
    };
    this.ClearPlaylist = function() {
        this.Playlist = new SonosPlaylist();
    };
    this.CurrentTrack = new SonosCurrentTrack();
    this.SetCurrentTrack = function (s, source) {
        if (typeof source === "undefined") {
            source = "Unbekannt";
        }
        SonosLog("SonosZOne: SetCurrentTRack: Quelle: " + source);
        if (this.BaseURL === "INITIAL" || typeof this.BaseURL ==="undefined") {
            ReloadSite(this.ZoneName + ":SetCurrentTrack");
        }
        if (this.CurrentTrack.BaseURL === "leer" || this.CurrentTrack.BaseURL === null) {
            this.CurrentTrack.BaseURL = this.BaseURL;
        }
        var oldstream = this.CurrentTrack.Stream;
        var oldstreamContent = this.CurrentTrack.StreamContent;
        var therearechanges = this.CurrentTrack.SetCurrentTrack(s);
        if (this.ActiveZone === true && therearechanges === true) {
            this.CurrentTrack.RenderCurrentTrack(this.CurrentTrackNumber);
            //Wenn Stream anders neu Rendern
            if (oldstream !== this.CurrentTrack.Stream || oldstreamContent !== this.CurrentTrack.StreamContent) {
                this.RenderNextTrack();
                this.RenderTrackTime();
                this.RenderAudioIn();
                this.RenderPlaylist("SetCurrentTrack");
            }
            this.RenderPlaylistCounter("SetCurrentTrack");
        }
    };
    this.CheckCurrenTrackRefesh = function() {
        if (this.Playlist.IsEmpty === false && (this.CurrentTrack.Artist === "leer" || this.CurrentTrack.MP3.Artist === "leer" || (this.CurrentTrack.MP3.Genre === "leer" && this.CurrentTrack.MP3.Jahr === 0 && this.CurrentTrack.MP3.Typ === "leer"))) {
            return true;
        }
        return false;
    };
    this.PlayState = "INITIAL";
    this.SetPlayState = function (s) {
        if (s === null) {
            console.log("SetPlayState ist Null und wird vom Server geladen");
            var suuid = this.ZoneUUID;
            var SetPlayStaterequest = SonosAjax("GetPlayState","",this.ZoneUUID);
            SetPlayStaterequest.done(function (data) {
                SonosZones[suuid].SetPlayState(data);
                return;
            });
            SetPlayStaterequest.fail(function (jqXHR, textStatus, errorThrown) {
                alert("SetPlayState Request Error: " + textStatus + " Error:" + errorThrown + " jqXHR:" + jqXHR.responseText);

            });
            return;
        }
        if (s === "TRANSITIONING") {
            return;
        }

        if (s !== this.PlayState) {
            //Prüfen ob initialisierung geändert wird. Ansonsten an den Server rangehen
            var op = 0;
            var playtext = "Play";
            var playinternal = "PLAYING";
            if (s === "PLAYING") {
                op = 1; //Playstate anzeigen
                playtext = "Pause";
                playinternal = "Pause";
                if (this.PlayState !== "INITIAL" && this.GetPlayerChangeEventIsRunning === false) {
                    //Ajax Request
                    var request = SonosAjax("Play","",this.ZoneUUID);
                    request.fail(function () {
                        ReloadSite("SonosZone:SetPlayState:Play");
                    });
                }
            } else {
                if (this.PlayState !== "INITIAL" && this.GetPlayerChangeEventIsRunning === false) {
                    //Ajax Request
                    var request2 = SonosAjax("Pause", "", this.ZoneUUID);
                    request2.fail(function () {
                        ReloadSite("SonosZone:SetPlayState:Pause");
                    });
                }
            }
            //Jqueryelement erzeugen und anpassen
            $("#" + this.ZoneUUID).next('img').css("opacity", op);
            $("#" + this.ZoneUUID + "_GroupPlayState").html("&nbsp;&nbsp;" + playtext + "&nbsp;&nbsp;").attr("onClick", "SonosZones." + this.ZoneUUID + ".SetPlayState('" + playinternal + "')");
            this.PlayState = s;
            if (this.ActiveZone === true) {
                if (s === "PLAYING") {
                    if (!SoDo.playButton.hasClass("aktiv")) {
                        SoDo.playButton.addClass("aktiv");
                    }
                } else {
                    if (SoDo.playButton.hasClass("aktiv")) {
                        SoDo.playButton.removeClass("aktiv");
                    }
                }
                if (this.CurrentTrack.Stream === false) {
                    SetCurrentPlaylistSong(this.CurrentTrackNumber, "SetPlaySTate"); //Diese Methode greift auf den PlayState zu daher erst jetzt ausführen.
                }
            }
        }
    };
    this.PlayMode = "INITIAL";
    this.SetPlayMode = function (s) {
        if (s === null) {
            return;
        }
        var ogrs = s;
        switch (s) {
            case "Shuffle":
                switch (this.PlayMode) {
                    case "NORMAL":
                        s = "SHUFFLE_NOREPEAT";
                        break;
                    case "REPEAT_ALL":
                        s = "SHUFFLE";
                        break;
                    case "SHUFFLE_NOREPEAT":
                        s = "NORMAL";
                        break;
                    case "SHUFFLE":
                        s = "REPEAT_ALL";
                        break;
                }
                break;
            case "Repeat":
                switch (this.PlayMode) {
                    case "NORMAL":
                        s = "REPEAT_ALL";
                        break;
                    case "REPEAT_ALL":
                        s = "NORMAL";
                        break;
                    case "SHUFFLE_NOREPEAT":
                        s = "SHUFFLE";
                        break;
                    case "SHUFFLE":
                        s = "SHUFFLE_NOREPEAT";
                        break;
                }
                break;
            default:
                ogrs = "Shuffle";
                //todo: Hier prüfen ob Shuffle war bzw. neu dazu gekommen ist. 
        } //ende Switch 1
        if (this.ActiveZone === true) {
            if (this.PlayMode !== "INITIAL" && this.GetPlayerChangeEventIsRunning === false && ogrs !== this.PlayMode) {
                SonosAjax("SetPlaymode","",s).complete(function () {
                    

                });
            }
            if (ogrs === "Shuffle") {
                //hier nun die Playlist neu laden, weil Shuffle geändert wurde.
                window.setTimeout("SonosZones[SonosZones.ActiveZoneUUID].SetPlaylist(true,'SetPlaystate')", 220);
                //SetCurrentPlaylistSong(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber);
            }
            SetPlaymodeDivs(s);
        }
        this.PlayMode = s;
    };
    this.ActiveZone = false;
    //Beim Wechsel der Activen Zone reagieren und methode starten. 
    this.SetLocalActiveZone = function (s) {
        if (s === true) {
            SonosZones.SetActiveZone(this.ZoneUUID);
            if (this.Playlist.Playlist.length === 0 && this.PlaylistLoader === false) {
                this.SetPlaylist(true, "SonosZone:SetLocalActiveZone");
            }
        }
        this.ActiveZone = s;


    };
    this.FadeMode = "INITIAL";
    this.SetFadeMode = function (s) {
        if (s !== this.FadeMode) {
            if (this.ActiveZone === true) {
                if (s === true) {
                    if (!SoDo.fadeButton.hasClass("aktiv")) {
                        SoDo.fadeButton.addClass("aktiv");
                    }
                } else {
                    if (SoDo.fadeButton.hasClass("aktiv")) {
                        SoDo.fadeButton.removeClass("aktiv");
                    }
                }
            }
            this.FadeMode = s;
        }
    };
    this.CordinatorVolume = -1; //Initialwert
    this.SetCordinatorVolume = function (s) {
        //todo: Überlegen ob man für die Oberfläche nicht Groupvolume nimmt. 
        s = parseInt(s);
        if (s > 100) { s = 100; }
        if (s < 1) {
            s = 1;
        }
        if (this.CordinatorVolume !== s) {
            this.CordinatorVolume = s;
            SoDo.labelVolume.html(this.CordinatorVolume);
            SoDo.volumeSlider.slider({ value: this.CordinatorVolume });
        }
    };
    this.Mute = "initial";
    this.SetMute = function (s) {
        if (this.ActiveZone === true && this.Mute !== "initial") {
            if (s === true) {
                if (!SoDo.MuteButton.hasClass("aktiv")) {
                    SoDo.MuteButton.addClass("aktiv");
                }
            } else {
                if (SoDo.MuteButton.hasClass("aktiv")) {
                    SoDo.MuteButton.removeClass("aktiv");
                }
            }
        }
        this.Mute = s;
    };
    this.NumberOfTracks = 0;
    this.SetNumberOfTracks = function (s) {
        s = parseInt(s);
        if (this.ActiveZone === true && s !== this.NumberOfTracks) {
            //Playlist Überprüfen und neu laden, wenn falsch
            if (this.Playlist.Playlist.length !== s) {
                this.SetPlaylist(true, "SonosZone:SetNumberOfTracks");
            }
        }
        this.NumberOfTracks = s;
        this.RenderPlaylistCounter("Setnumberoftracks");
    };
    this.CurrentTrackNumber = 0;
    this.SetCurrentTrackNumber = function (s) {
        if ((typeof s === "undefined" || s === null) && this.CurrentTrackNumber !== null) {
            return this.CurrentTrackNumber;
        }
        if (this.PlayMode === "REPEAT_ALL" || this.PlayMode === "SHUFFLE") {
            if (s > this.NumberOfTracks) {
                s = 1;
            }
            if (s === 0) {
                s = this.NumberOfTracks;
            }
        }
        if (s > this.NumberOfTracks) {
            return false;
        }
        var snumber = parseInt(s);
        if (this.CurrentTrackNumber !== snumber) {
            this.CurrentTrackNumber = snumber;
            if (this.ActiveZone === true) {
                this.RenderPlaylistCounter("SetCurrentTrackNumber");
                SoVa.currentplaylistScrolled = false;
                SetCurrentPlaylistSong(snumber, "SetCurrentTrackNumber");
            }
        }
        return this.CurrentTrackNumber;
    };
    this.CurrentDuration = new Hour("0:00:00");
    this.CurrentDurationString = "INITIAL";
    this.CurrentDurationSliderActive = false;
    this.CurrentRelTime = new Hour("0:00:00");
    this.CurrentRelTimeString = "INITIAL";
    this.SetTrackTimechanged = false;
    this.SetTrackTime = function(rel, dur) {
        this.SetTrackTimechanged = false;
        if (this.CurrentDurationString !== dur) {
            this.CurrentDuration.setTimeString(dur);
            this.CurrentDurationString = dur;
            if (this.ActiveZone === true) {
                this.SetTrackTimechanged = true;
                SoDo.runtimeSlider.slider("option", "max", this.CurrentDuration.valueOf());
            }
        }
        if (this.CurrentRelTimeString !== rel) {
            this.CurrentRelTimeString = rel;
            this.CurrentRelTime.setTimeString(rel);
            if (this.ActiveZone === true) {
                this.SetTrackTimechanged = true;
                if (this.CurrentDurationSliderActive === false) {
                    SoDo.runtimeSlider.slider("option", "value", this.CurrentRelTime.valueOf());
                }
            }
        }
        if (this.ActiveZone === true && this.SetTrackTimechanged === true) {
            this.RenderTrackTime();
            this.SetTrackTimechanged = false;
        }
    };
    this.RenderTrackTime = function() {
        if (this.Playlist.IsEmpty === true || (this.CurrentTrack.Stream === true && this.CurrentTrack.StreamContent !== "Dienst" && this.CurrentTrack.StreamContent !== "Apple")) {
            if (SoDo.runtimeCurrentSong.is(":visible")) {
                SoDo.runtimeCurrentSong.hide();
            }
        } else {
            if (SoDo.runtimeCurrentSong.is(":hidden")) {
                SoDo.runtimeCurrentSong.show();
            }
            if (SoDo.runtimeRelTime.html() !== this.CurrentRelTimeString) {
                SoDo.runtimeRelTime.html(this.CurrentRelTimeString);
            }
            if (SoDo.runtimeDuration.html() !== this.CurrentDurationString) {
                SoDo.runtimeDuration.html(this.CurrentDurationString);
            }
        }
    };
    this.SetAktSongInfo = function (s, mp3state) {
        mp3state = mp3state || false;
        //Daten aus AktSongInfo Verarbeiten.
        this.SetTrackTime(s.RelTime, s.CurrentTrackDuration);
        //Prüfen, ob noch alles bei den TrackNumber richtig ist
        if (this.NumberOfTracks === 0 && s.NumberOfTracks !== null && parseInt(s.NumberOfTracks) !== this.NumberOfTracks) {
            this.SetNumberOfTracks(parseInt(s.NumberOfTracks));
        }
        if (this.CurrentTrackNumber !== parseInt(s.CurrentTrackNumber) || mp3state === true || (s.CurrentTrack !== null && s.CurrentTrack.Title !== null && this.CurrentTrack.Title.toUpperCase() !== s.CurrentTrack.Title.toUpperCase())) {
            this.SetCurrentTrackNumber(parseInt(s.CurrentTrackNumber));
            //Erst jetzt, da beim Rendern, die TrackNumber benötigt wird.
            this.SetCurrentTrack(s.CurrentTrack, "SetAktSongInfo");
        }
    };
    this.GetCordinatedPlayerasStringFormat = function () {
        var sfcp = "";
        for (var i = 0; i < _cordinatedPlayer.length; i++) {
            if (this.ZoneUUID !== _cordinatedPlayer[i].UUID) {
                sfcp += '<p style="font-size: 0.6em;">' + _cordinatedPlayer[i].Name + '</p>';
            }
        }
        return sfcp;
    };
    this.RenderNextTrack = function(source) {
        SonosLog("RenderNextTrack CalledBy:" + source);
        //Stream
        if ((this.CurrentTrack.Stream === true && this.CurrentTrack.StreamContent !== "Dienst" && this.CurrentTrack.StreamContent !== "Apple") || this.Playlist.IsEmpty) {
            if (SoDo.nextSongWrapper.is(":visible")) {
                SoDo.nextSongWrapper.hide();
            }
            return;
        }
        var nexttracknumber = this.CurrentTrackNumber;
        if ((this.PlayMode === "REPEAT_ALL" || this.PlayMode === "SHUFFLE") && this.Playlist.Playlist.length <= this.CurrentTrackNumber) {
            nexttracknumber = 0;
        }
        if (!this.Playlist.IsEmpty && typeof this.Playlist.Playlist[nexttracknumber] !== "undefined") {
            if (SoDo.nextSongWrapper.is(":hidden")) {
                SoDo.nextSongWrapper.show();
            }
            var seap = " - ";
            if (this.Playlist.Playlist[nexttracknumber].Artist === 'leer' || this.Playlist.Playlist[nexttracknumber].Title === 'leer' || this.Playlist.Playlist[nexttracknumber].Artist === '' || this.Playlist.Playlist[nexttracknumber].Title === '') {
                seap = '';
            }
            var text = "";
            if (this.Playlist.Playlist[nexttracknumber].Artist !== "leer") {
                text = this.Playlist.Playlist[nexttracknumber].Artist + seap;
            }
            if (this.Playlist.Playlist[nexttracknumber].Title !== "leer") {
                text = text + this.Playlist.Playlist[nexttracknumber].Title;
            }
            if (SoDo.nextTitle.text() !== text) {
                SoDo.nextTitle.text(text);
            }
            if (this.Playlist.Playlist[nexttracknumber].AlbumArtURI === "leer") {
                if (SoDo.nextcover.is(":visible")) {
                    SoDo.nextcover.hide();
                }
            } else {
                if (SoDo.nextcover.is(":hidden")) {
                    SoDo.nextcover.show();
                }
                if (SoDo.nextcover.attr("src") !== 'http://' + this.BaseURL + this.Playlist.Playlist[nexttracknumber].AlbumArtURI) {
                    SoDo.nextcover.attr("src", 'http://' + this.BaseURL + this.Playlist.Playlist[nexttracknumber].AlbumArtURI);
                }
            }
        } else {
            //Ist nur Ein track vorhanden
            if (SoDo.nextSongWrapper.is(":visible")) {
                SoDo.nextSongWrapper.hide();
            }

        }

    };
    this.GetCordinatedPlayer = function () { return _cordinatedPlayer; };
    this.AddCordinatedPlayer = function (s) {
        _cordinatedPlayer.push(s);
        //MultiVolume falls schon irgendwie vorhanden war
        var oldValue = $("#Multivolumeslider_" + s.UUID).slider("value");
        if (!isNaN(oldValue) && oldValue !== s.Volume) {
            $("#Multivolumeslider_" + s.UUID).slider({ value: s.Volume });
            $("#MultivolumesliderVolumeNumber_" + s.UUID).html(s.Volume);
        }
    };
    this.SetBySonosItem = function (s) {
        this.ZoneUUID = s.Coordinator.UUID;
        this.ZoneName = s.Coordinator.Name;
        if (this.BaseURL !== s.Coordinator.BaseUrl) {
            this.SetBaseurl(s.Coordinator.BaseUrl);
        }
        if (parseInt(s.Coordinator.CurrentState.LastStateChange) !== this.LastChange) {
            this.SetLastChange(s.Coordinator.CurrentState.LastStateChange);
        }
        if (this.HasAudioIn !== s.Coordinator.HasAudioIn) {
            this.SetHasAudioIn(s.Coordinator.HasAudioIn);
        }
        if (this.SleepMode !== s.Coordinator.CurrentState.RemainingSleepTimerDuration) {
            this.SetSleepMode(s.Coordinator.CurrentState.RemainingSleepTimerDuration);
        }
        if (parseInt(s.Coordinator.CurrentState.Volume) !== this.CordinatorVolume) {
            this.SetCordinatorVolume(parseInt(s.Coordinator.CurrentState.Volume));
        }
        if (this.NumberOfTracks !== parseInt(s.Coordinator.CurrentState.NumberOfTracks)) {
            this.SetNumberOfTracks(parseInt(s.Coordinator.CurrentState.NumberOfTracks));
        }
        if (this.CurrentTrackNumber !== parseInt(s.Coordinator.CurrentState.CurrentTrackNumber)) {
            this.SetCurrentTrackNumber(parseInt(s.Coordinator.CurrentState.CurrentTrackNumber));
        }
        this.SetCurrentTrack(s.Coordinator.CurrentState.CurrentTrack, "SetBySonosItem");
        if (this.PlayState !== s.Coordinator.CurrentState.TransportState) {
            this.SetPlayState(s.Coordinator.CurrentState.TransportStateString);
        }
        if (this.RatingFilter !== s.Coordinator.RatingFilter) {
            this.SetRatingFilter(s.Coordinator.RatingFilter);
        }
        if (this.PlayMode !== s.Coordinator.CurrentState.CurrentPlayMode) {
            this.SetPlayMode(s.Coordinator.CurrentState.CurrentPlayMode);
        }
        if (this.FadeMode !== s.Coordinator.CurrentState.CurrentCrossfadeMode) {
            this.SetFadeMode(s.Coordinator.CurrentState.CurrentCrossfadeMode);
        }
        this.SetTrackTime(s.Coordinator.CurrentState.RelTime, s.Coordinator.CurrentState.CurrentTrackDuration);
        if (this.Mute !== s.Coordinator.Mute) {
            this.SetMute(s.Coordinator.Mute);
        }
        if (typeof SonosZones.PlayersUUIDToNames[s.CoordinatorUUID] === "undefined") {
            //Wenn nicht definiert ein neuen anlegen	
            SonosZones.PlayersUUIDToNames[s.CoordinatorUUID] = s.Coordinator.Name;
        }
        if (this.ActiveZone === true) {
            //this.RenderNextTrack();
            this.RenderPlaylist("SetBySonosItem");
        }
        if (parseInt($("#" + s.CoordinatorUUID).attr("data-players")) !== s.Players.length) {
            _cordinatedPlayer = [];
            for (var i = 0; i < s.Players.length; i++) {
                var sonosplayer = new SonosDevice(s.Players[i].UUID, s.Players[i].Name, parseInt(s.Players[i].CurrentState.Volume));
                //prüfen, ob es sich um den Cordinator handelt
                if (this.ZoneUUID !== sonosplayer.UUID) {
                    this.AddCordinatedPlayer(sonosplayer);
                    if (typeof SonosZones.PlayersUUIDToNames[sonosplayer.UUID] === "undefined") {
                        //Wenn nicht definiert ein neuen anlegen	
                        SonosZones.PlayersUUIDToNames[sonosplayer.UUID] = sonosplayer.Name;
                    }
                }
            }
        } else {
            for (var i2 = 0; i2 < s.Players.length; i2++) {
                for (var y= 0; y < _cordinatedPlayer.length; y++) {
                    if (s.Players[i2].UUID === _cordinatedPlayer[y].UUID && s.Players[i2].Volume !== _cordinatedPlayer[y].Volume) {
                        _cordinatedPlayer[y].Volume = s.Players[i2].CurrentState.Volume;
                        var oldValue = $("#Multivolumeslider_" + _cordinatedPlayer[y].UUID).slider("value");
                        if (!isNaN(oldValue) && oldValue !== _cordinatedPlayer[y].Volume) {
                            $("#Multivolumeslider_" + _cordinatedPlayer[y].UUID).slider({ value: _cordinatedPlayer[y].Volume });
                            $("#MultivolumesliderVolumeNumber_" + _cordinatedPlayer[y].UUID).html(_cordinatedPlayer[y].Volume);
                        }
                    }
                }
            }
        }
        if (this.GroupVolume !== s.Coordinator.GroupVolume) {
            this.GroupVolume = s.Coordinator.GroupVolume;
        }
        if (this.ActiveZone === true) {
            this.RenderGroupVolume("groupVolume");
        }
        this.GetPlayerChangeEventIsRunning = false;
    };
}