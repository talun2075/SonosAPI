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
    this.Playlist = new SonosPlaylist();
    this.PlaylistLoader = false;
    this.CurrentTrack = new SonosCurrentTrack();
    this.CurrentDuration = new Hour("0:00:00");
    this.CurrentDurationString = "INITIAL";
    this.CurrentDurationSliderActive = false;
    this.CurrentRelTime = new Hour("0:00:00");
    this.CurrentRelTimeString = "INITIAL";
    this.SetTrackTimechanged = false;
    //Propertys over DefinePropertys
    var _PlayState = "INITIAL";
    var _NumberOfTracks = 0;
    var _PlayMode = "INITIAL";
    var _ActiveZone = false;
    var _FadeMode = "INITIAL";
    var _CordinatorVolume = -1; //Initialwert
    var _HasAudioIn = "INITIAL";
    var _Mute = "initial";
    var _CurrentTrackNumber = 0;
    var _SleepMode = "INITIAL";
    var _GroupVolume = 0;
    var _RatingFilter = "INITIAL";
    var _BaseURL = "INITIAL";
    var _LastChange = 0;

    Object.defineProperty(this, "PlayState", {
        get: function () {
            return _PlayState;
        },
        set: function (value) {
            var t = this;
            if (value === null) {
                console.log("SetPlayState ist Null und wird vom Server geladen");
                var suuid = t.ZoneUUID;
                var SetPlayStaterequest = SonosAjax("GetPlayState", "", t.ZoneUUID);
                SetPlayStaterequest.success(function (data) {
                    SonosZones[suuid].PlayState = data;
                    return;
                });
                SetPlayStaterequest.fail(function (jqXHR, textStatus, errorThrown) {
                    alert("SetPlayState Request Error: " + textStatus + " Error:" + errorThrown + " jqXHR:" + jqXHR.responseText);

                });
                return;
            }
            if (value === "TRANSITIONING") {
                return;
            }

            if (value !== _PlayState) {
                //Prüfen ob initialisierung geändert wird. Ansonsten an den Server rangehen
                var op = 0;
                var playtext = "Play";
                var playinternal = "PLAYING";
                if (value === "PLAYING") {
                    op = 1; //Playstate anzeigen
                    playtext = "Pause";
                    playinternal = "Pause";
                    }
                //Jqueryelement erzeugen und anpassen
                $("#" + t.ZoneUUID).next('img').css("opacity", op);
                $("#" + t.ZoneUUID + "_GroupPlayState").html("&nbsp;&nbsp;" + playtext + "&nbsp;&nbsp;").attr("onClick", "SetPlayState('" + t.ZoneUUID + "','" + playinternal + "')");
                _PlayState = value;
                if (t.ActiveZone === true) {
                    if (value === "PLAYING") {
                        if (!SoDo.playButton.hasClass("aktiv")) {
                            SoDo.playButton.addClass("aktiv");
                        }
                    } else {
                        if (SoDo.playButton.hasClass("aktiv")) {
                            SoDo.playButton.removeClass("aktiv");
                        }
                    }
                    if (t.CurrentTrack.Stream === false) {
                        SetCurrentPlaylistSong(t.CurrentTrackNumber, "SetPlaySTate"); //Diese Methode greift auf den PlayState zu daher erst jetzt ausführen.
                    }
                }
            }
        }
    });
    Object.defineProperty(this, "PlayMode", {
        get: function () {
            return _PlayMode;
        },
        set: function (value) {
            try {
                if (value !==null && typeof value !== "undefined" && value.indexOf("SHUFFLE") !== -1 || _PlayMode !==null && _PlayMode.indexOf("SHUFFLE") !== -1) {
                    //hier nun die Playlist neu laden, weil Shuffle geändert wurde.
                    window.setTimeout("SonosZones[SonosZones.ActiveZoneUUID].SetPlaylist(true,'SetPlaystate')", 220);
                    //SetCurrentPlaylistSong(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber);
                }
            }
            catch (Ex) {
                console.log("value:"+value);
                console.log("Playmode:"+_PlayMode);
                console.log(Ex);
            }
            _PlayMode = value;
                SetPlaymodeDivs(value);
            }
    });
    Object.defineProperty(this, "ActiveZone", {
        get: function () {
            return _ActiveZone;
        },
        set: function (value) {
            var t = this;
            if (value !== _ActiveZone) {
                if (value === true) {
                    SonosZones.SetActiveZone(t.ZoneUUID);
                    if (t.Playlist.Playlist.length === 0 && t.PlaylistLoader === false) {
                        t.SetPlaylist(true, "SonosZone:ActiveZone");
                    }
                }
                _ActiveZone = value;
            }
        },
        configurable: false
    });
    Object.defineProperty(this, "FadeMode", {
        get: function () {
            return _FadeMode;
        },
        set: function (value) {
            if (value !== _FadeMode) {
                if (value === true) {
                    if (!SoDo.fadeButton.hasClass("aktiv")) {
                        SoDo.fadeButton.addClass("aktiv");
                    }
                } else {
                    if (SoDo.fadeButton.hasClass("aktiv")) {
                        SoDo.fadeButton.removeClass("aktiv");
                    }
                }
                _FadeMode = value;
            }
        }
    });
    Object.defineProperty(this, "CordinatorVolume", {
        get: function () {
            return _CordinatorVolume;
        },
        set: function (value) {
            if (value !== _CordinatorVolume) {
                value = parseInt(value);
                if (value > 100) { value = 100; }
                if (value < 1) {
                    value = 1;
                }
                if (_CordinatorVolume !== value) {
                    _CordinatorVolume = value;
                    SoDo.labelVolume.html(value);
                    SoDo.volumeSlider.slider({ value: value });
                }
                //GruppenVolumen Prüfen.
                var oldValue = $("#MultivolumesliderPrimary").slider("value");
                if (!isNaN(oldValue) && oldValue !== _CordinatorVolume) {
                    $("#MultivolumesliderPrimary").slider({ value: _CordinatorVolume });
                    $("#MultivolumePrimaryNumber").html(_CordinatorVolume);
                }
            }
        },
        configurable: false
    });
    Object.defineProperty(this, "HasAudioIn", {
        get: function () {
            return _HasAudioIn;
        },
        set: function (value) {
            var t = this;
            _HasAudioIn = value;
            if (t.ActiveZone === true) {
                t.RenderAudioIn();
            }
        }
    });
    Object.defineProperty(this, "Mute", {
        get: function () {
            return _Mute;
        },
        set: function (value) {
            var t = this;
            if (t.ActiveZone === true && _Mute !== "initial") {
                if (value === true) {
                    if (!SoDo.MuteButton.hasClass("aktiv")) {
                        SoDo.MuteButton.addClass("aktiv");
                    }
                } else {
                    if (SoDo.MuteButton.hasClass("aktiv")) {
                        SoDo.MuteButton.removeClass("aktiv");
                    }
                }
            }
            _Mute = value;
        }
    });
    Object.defineProperty(this, "CurrentTrackNumber", {
        get: function () {
            return _CurrentTrackNumber;
        },
        set: function (value) {
            var t = this;
            if (this.PlayMode === "REPEAT_ALL" || this.PlayMode === "SHUFFLE") {
                if (value > t.NumberOfTracks) {
                    value = 1;
                }
                if (value === 0) {
                    value = t.NumberOfTracks;
                }
            }
            if (value > t.NumberOfTracks) {
                return false;
            }
            var snumber = parseInt(value);
            if (_CurrentTrackNumber !== snumber) {
                _CurrentTrackNumber = snumber;
                if (t.ActiveZone === true) {
                    t.RenderPlaylistCounter("SetCurrentTrackNumber");
                    SoVa.currentplaylistScrolled = false;
                    SetCurrentPlaylistSong(snumber, "SetCurrentTrackNumber");
                }
            }
            return true;
        }
    });
    Object.defineProperty(this, "NumberOfTracks", {
        get: function () {
            return _NumberOfTracks;
        },
        set: function (value) {
            if (_NumberOfTracks === value) return true;
            var t = this;
            value = parseInt(value);
            if (isNaN(value)) {
                alert("NumberofTracks ist keine Zahl:" + value);
                return false;
            }
            if (t.ActiveZone === true && value !== _NumberOfTracks) {
                //Playlist Überprüfen und neu laden, wenn falsch
                if (t.Playlist.Playlist.length !== value) {
                    t.SetPlaylist(true, "SonosZone:NumberOfTracks");
                }
            }
            _NumberOfTracks = value;
            t.RenderPlaylistCounter("Setnumberoftracks");
            return true;
        }
    });
    Object.defineProperty(this, "SleepMode", {
        get: function () {
            return _SleepMode;
        },
        set: function (value) {
            var t = this;
            if (value !== _SleepMode) {
                if (value === null) {
                    return;
                }
                if (t.ActiveZone === true) {
                    //Wert reinschreiben.
                    if (value !== "" && value !== "aus" && value !== "00:00:00") {
                        if (!SoDo.sleepModeButton.hasClass("aktiv")) {
                            SoDo.sleepModeButton.addClass("aktiv");
                        }
                        if (SoDo.sleepModeState.text() !== value) {
                            SoDo.sleepModeState.text(value);
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
                _SleepMode = value;
            }
        }
    });
    Object.defineProperty(this, "GroupVolume", {
        get: function () {
            return _GroupVolume;
        },
        set: function(value) {
            var t = this;
            if (_GroupVolume !== value) {
                _GroupVolume = value;
            
            if (t.ActiveZone === true) {
                if (_cordinatedPlayer.length > 0) {
                    var oldValue = $("#MultivolumesliderAll").slider("value");
                    if (!isNaN(oldValue) && oldValue !== _GroupVolume) {
                        $("#MultivolumesliderAll").slider({ value: _GroupVolume });
                        $("#MultivolumeAllNumber").html(_GroupVolume);
                    }
                    //_cordinatedPlayer.forEach(function(element) {
                    //    oldValue = $("#Multivolumeslider_" + element.UUID).slider("value");
                    //    if (!isNaN(oldValue) && oldValue !== element.Volume) {
                    //        $("#Multivolumeslider_" + element.UUID).slider({ value: element.Volume });
                    //        $("#MultivolumesliderVolumeNumber_" + element.UUID).html(element.Volume);
                    //    }
                    //});
                }
            }
        }
    }
    });
    Object.defineProperty(this, "RatingFilter", {
        get: function () {
            return _RatingFilter;
        },
        set: function (value) {
            var t = this;
            if (value === null) return;
            _RatingFilter = value;
            if (t.ActiveZone) {
                t.RenderRatingFilter();
            }

        }
    });
    Object.defineProperty(this, "BaseURL", {
        get: function () {
            return _BaseURL;
        },
        set: function (value) {
            var t = this;
            if (value === null || value === "") {
                var retval = SonosAjax("BaseUrl", t.ZoneUUID);
                retval.success(function (data) {
                    if (typeof data !== "undefined" && data !== "") {
                        value = data;
                    } else {
                        alert("Baseurl für Player " + t.Name + " konnte nicht ermittelt werden. Return vom Server:" + data);
                    }
                });
            }
            _BaseURL = value;
        }
    });
    Object.defineProperty(this, "LastChange", {
        get: function () {
            return _LastChange;
        },
        set: function (value) {
            if (value === null || typeof value === "undefined") {
                _LastChange = 0;
            }
            _LastChange = value;
        }
    });
    //Methoden
    this.SendPlayState = function(value) {
        if (value === "PLAYING") {
            if (_PlayState !== "INITIAL" && this.GetPlayerChangeEventIsRunning === false) {
                //Ajax Request
                var request = SonosAjax("Play", "", this.ZoneUUID);
                request.fail(function () {
                    ReloadSite("SonosZone:SetPlayState:Play");
                });
            }
        } else {
            if (_PlayState !== "INITIAL" && this.GetPlayerChangeEventIsRunning === false) {
                //Ajax Request
                var request2 = SonosAjax("Pause", "", this.ZoneUUID);
                request2.fail(function () {
                    ReloadSite("SonosZone:SetPlayState:Pause");
                });
            }
        }
        this.PlayState = value;
    }
    this.SendPlayMode = function (value) {
        if (value === null) {
            return;
        }
        switch (value) {
            case "Shuffle":
                switch (_PlayMode) {
                    case "NORMAL":
                        value = "SHUFFLE_NOREPEAT";
                        break;
                    case "REPEAT_ALL":
                        value = "SHUFFLE";
                        break;
                    case "SHUFFLE_NOREPEAT":
                        value = "NORMAL";
                        break;
                    case "SHUFFLE":
                        value = "REPEAT_ALL";
                        break;
                    case "SHUFFLE_REPEAT_ONE":
                        value = "REPEAT_ONE";
                        break;
                    case "REPEAT_ONE":
                        value = "SHUFFLE_REPEAT_ONE";
                        break;
                }
                break;
            case "Repeat":
                switch (_PlayMode) {
                    case "NORMAL":
                        value = "REPEAT_ALL";
                        break;
                    case "REPEAT_ALL":
                        value = "REPEAT_ONE";
                        break;
                    case "SHUFFLE_NOREPEAT":
                        value = "SHUFFLE";
                        break;
                    case "SHUFFLE":
                        value = "SHUFFLE_REPEAT_ONE";
                        break;
                    case "SHUFFLE_REPEAT_ONE":
                        value = "SHUFFLE_NOREPEAT";
                        break;
                    case "REPEAT_ONE":
                        value = "NORMAL";
                        break;
                }
                break;
            default:
                alert("SendPlaymode Unbekannter Value:" + value);
        }
        this.PlayMode = value;
        SonosAjax("SetPlaymode", "", value).complete(function () { });
    }
    this.RenderTrackTime = function () {
        if (this.Playlist.CheckIsEmpty() === true || this.CurrentTrack.Stream === true && this.CurrentTrack.StreamContent !== "Dienst" && this.CurrentTrack.StreamContent !== "Apple") {
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
    this.RenderPlaylist = function (source) {
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
    this.RenderPlaylistCounter = function (source) {
        SonosLog("RenderPlaylistCounter Callby:" + source);
        if (this.Playlist.CheckIsEmpty() === true || this.CurrentTrack !== null && this.CurrentTrack.Stream === true && this.CurrentTrack.StreamContent !== "Dienst" && this.CurrentTrack.StreamContent !== "Apple") {
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
    this.CheckPlayerChangeEvent = function () {
        if (this.GetPlayerChangeEventIsRunningCounter < 5) {
            this.GetPlayerChangeEventIsRunningCounter++;
        } else {
            this.GetPlayerChangeEventIsRunning = false;
            this.GetPlayerChangeEventIsRunningCounter = 0;
        }
    };
    this.RenderAudioIn = function () {
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
    this.GetCordinatedPlayerasStringFormat = function () {
        var sfcp = "";
        for (var i = 0; i < _cordinatedPlayer.length; i++) {
            if (this.ZoneUUID !== _cordinatedPlayer[i].UUID) {
                sfcp += '<p style="font-size: 0.6em;">' + _cordinatedPlayer[i].Name + '</p>';
            }
        }
        return sfcp;
    };
    this.RenderNextTrack = function (source) {
        SonosLog("RenderNextTrack CalledBy:" + source);
        //Stream
        if (this.CurrentTrack.Stream === true && this.CurrentTrack.StreamContent !== "Dienst" && this.CurrentTrack.StreamContent !== "Apple" || this.Playlist.CheckIsEmpty()) {
            if (SoDo.nextSongWrapper.is(":visible")) {
                SoDo.nextSongWrapper.hide();
            }
            return;
        }
        var nexttracknumber = this.CurrentTrackNumber;
        if (this.Playlist.Playlist.length <= this.CurrentTrackNumber && (this.PlayMode === "REPEAT_ALL" || this.PlayMode === "SHUFFLE")) {
            nexttracknumber = 0;
        }
        if (!this.Playlist.CheckIsEmpty() && typeof this.Playlist.Playlist[nexttracknumber] !== "undefined") {
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
    this.ChangeRatingFilter = function (typ, wert) {
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
                } else {
                    this.RatingFilter.AlbpumInterpretFilter = "unset";
                }
                changed = true;
                break;
        }
        if (changed === true) {
            if (this.ActiveZone) {
                this.RenderRatingFilter();
            }
            SonosAjax("SetRatingFilter", this.RatingFilter);
        }

    };
    this.RenderRatingFilter = function () {
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
    this.CheckCurrenTrackRefesh = function () {
        if (this.Playlist.CheckIsEmpty() === false && (this.CurrentTrack.Artist === "leer" || this.CurrentTrack.MP3.Artist === "leer" || this.CurrentTrack.MP3.Genre === "leer" && this.CurrentTrack.MP3.Jahr === 0 && this.CurrentTrack.MP3.Typ === "leer")) {
            return true;
        }
        return false;
    };
    this.SetPlaylist = function (v, source) {
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
        request.success(function (data) {
            if (typeof SonosZones[cuuid] === "undefined") {
                console.warn("Fehler Player scheint es nicht mehr zu geben RINCON:" + cuuid);
                return;
            }
            SoVa.PlaylistLoadError = 0;
            SonosZones[cuuid].Playlist.Playlist = data.PlayListItems;
            if (data.TotalMatches !== null && data.TotalMatches !== 0) {
                var tm = data.TotalMatches;
                if (tm !== 0) {
                    SonosZones[cuuid].NumberOfTracks = tm;
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
        request.fail(function (jqXHR) {
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
    this.ClearPlaylist = function () {
        this.Playlist = new SonosPlaylist();
    };
    this.SetCurrentTrack = function (s, source) {
        if (typeof source === "undefined") {
            source = "Unbekannt";
        }
        SonosLog("SonosZOne: SetCurrentTRack: Quelle: " + source);
        if (this.BaseURL === "INITIAL" || typeof this.BaseURL === "undefined") {
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
    this.SetTrackTime = function (rel, dur) {
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
    this.SetAktSongInfo = function (s, mp3state) {
        mp3state = mp3state || false;
        //Daten aus AktSongInfo Verarbeiten.
        this.SetTrackTime(s.RelTime, s.CurrentTrackDuration);
        //Prüfen, ob noch alles bei den TrackNumber richtig ist
        if (this.NumberOfTracks === 0 && s.NumberOfTracks !== null) {
            this.NumberOfTracks = parseInt(s.NumberOfTracks);
        }
        if (this.CurrentTrackNumber !== parseInt(s.CurrentTrackNumber) || mp3state === true || s.CurrentTrack !== null && s.CurrentTrack.Title !== null && this.CurrentTrack.Title.toUpperCase() !== s.CurrentTrack.Title.toUpperCase()) {
            this.CurrentTrackNumber = parseInt(s.CurrentTrackNumber);
            //Erst jetzt, da beim Rendern, die TrackNumber benötigt wird.
            this.SetCurrentTrack(s.CurrentTrack, "SetAktSongInfo");
        }
    };
    this.SetBySonosItem = function (s) {
        this.ZoneUUID = s.Coordinator.UUID;
        this.ZoneName = s.Coordinator.Name;
        if (this.BaseURL !== s.Coordinator.BaseUrl) {
            this.BaseURL = s.Coordinator.BaseUrl;
        }
        if (s.Coordinator.CurrentState.LastStateChange !== this.LastChange) {
            this.LastChange = s.Coordinator.CurrentState.LastStateChange;
        }
        if (this.HasAudioIn !== s.Coordinator.HasAudioIn) {
            this.HasAudioIn =s.Coordinator.HasAudioIn;
        }
        if (this.SleepMode !== s.Coordinator.CurrentState.RemainingSleepTimerDuration) {
            this.SleepMode =s.Coordinator.CurrentState.RemainingSleepTimerDuration;
        }
        if (parseInt(s.Coordinator.CurrentState.Volume) !== this.CordinatorVolume) {
            this.CordinatorVolume =parseInt(s.Coordinator.CurrentState.Volume);
        }
        if (this.NumberOfTracks !== parseInt(s.Coordinator.CurrentState.NumberOfTracks)) {
            this.NumberOfTracks =parseInt(s.Coordinator.CurrentState.NumberOfTracks);
        }
        if (this.CurrentTrackNumber !== parseInt(s.Coordinator.CurrentState.CurrentTrackNumber)) {
            this.CurrentTrackNumber =parseInt(s.Coordinator.CurrentState.CurrentTrackNumber);
        }
        this.SetCurrentTrack(s.Coordinator.CurrentState.CurrentTrack, "SetBySonosItem");
        if (this.PlayState !== s.Coordinator.CurrentState.TransportStateString) {
            this.PlayState =s.Coordinator.CurrentState.TransportStateString;
        }
        if (this.RatingFilter !== s.Coordinator.RatingFilter) {
            this.RatingFilter =s.Coordinator.RatingFilter;
        }
        if (this.PlayMode !== s.Coordinator.CurrentState.CurrentPlayMode) {
            this.PlayMode =s.Coordinator.CurrentState.CurrentPlayMode;
        }
        if (this.FadeMode !== s.Coordinator.CurrentState.CurrentCrossfadeMode) {
            this.FadeMode = s.Coordinator.CurrentState.CurrentCrossfadeMode;
        }
        this.SetTrackTime(s.Coordinator.CurrentState.RelTime, s.Coordinator.CurrentState.CurrentTrackDuration);
        if (this.Mute !== s.Coordinator.Mute) {
            this.Mute =s.Coordinator.Mute;
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
        this.GetPlayerChangeEventIsRunning = false;
    };
}