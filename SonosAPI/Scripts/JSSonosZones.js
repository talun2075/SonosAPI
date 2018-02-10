"use strict";
//Alle SonosZonen
function SonosZonesObject() {
    var Checker = "RINCON";
    this.ZonesCount = 0;
    //this.ZonesUUIDToNames = {};
    this.PlayersUUIDToNames = {};
    this.ActiveZoneUUID = "";
    this.ActiveZoneName = "";
    this.Refreshstop = false;
    this.GetTopologieChangeTime = "INITIAL";
    this.CheckRendering = function() {
        var devplayer = $(".device");
        $.each(devplayer, function(i, item) {
            if (typeof SonosZones[item.id] !== "undefined") {
                if (parseInt(item.dataset.players) !== SonosZones[item.id].GetCordinatedPlayer().length) {
                    SonosZones.RenderZones();
                    return false;
                }
            }
            return true;
        });
    };
    this.RenderZones = function() {
        //Alles leeren
        if (SoDo.devicesWrapper.children(".groupdevicewrapper").length > 0) {
            SoDo.devicesWrapper.children(".groupdevicewrapper").remove();
        }
        try {
            //hier mal alle Zonen auflisten
            var prop = Object.getOwnPropertyNames(SonosZones);
            var zcounter = 0; //Anzahl der Zonen;
            for (var i = 0; i < prop.length; i++) {
                if (prop[i].substring(0, Checker.length) === Checker) {
                    //Es handelt sich um einen Sonosplayer
                    var p = prop[i];
                    var aktdev = "";
                    if (SonosZones[p].ZoneUUID === SonosZones.ActiveZoneUUID) {
                        aktdev = " akt_device";
                    }
                    var playstateimg = 'style="opacity:0;"';
                    var playtext = "Play";
                    var playinternal = "PLAYING";
                    if (SonosZones[p].PlayState === "PLAYING") {
                        playstateimg = 'style="opacity:1;"';
                        playtext = "Pause";
                        playinternal = playtext;
                    }
                    $('<div class="groupdevicewrapper"><div id="' + SonosZones[p].ZoneUUID + '" data-players="' + SonosZones[p].GetCordinatedPlayer().length + '" class="device' + aktdev + '" onclick="SetDevice(\'' + SonosZones[p].ZoneUUID + '\');"><p>' + SonosZones[p].ZoneName + '</p>' + SonosZones[p].GetCordinatedPlayerasStringFormat() + '</div><img id="deviceplayinggif_' + SonosZones[p].ZoneUUID + '" class="deviceplayinggif" ' + playstateimg + ' src="/images/playing.gif"><div id="GroupDevice_' + SonosZones[p].ZoneUUID + '" onclick="SetDeviceGroupFor(\'' + SonosZones[p].ZoneUUID + '\')" class="groupdeviceclass">&nbsp;&nbsp;Gruppe&nbsp;&nbsp;</div><div class="groupdeviceclass" onclick="SetPlayState(\'' + SonosZones[p].ZoneUUID + '\',\'' + playinternal + '\');" id="' + SonosZones[p].ZoneUUID + '_GroupPlayState">&nbsp;&nbsp;' + playtext + '&nbsp;&nbsp;</div></div>').appendTo(SoDo.devicesWrapper);
                    zcounter++;
                }
            }
            if (typeof SonosZones[SonosZones.ActiveZoneUUID] !== "undefined" && typeof SonosZones[SonosZones.ActiveZoneUUID].PlayState !== "undefined" && SonosZones[SonosZones.ActiveZoneUUID].PlayState === "PLAYING") {
                if (!SoDo.playButton.hasClass("aktiv")) {
                    SoDo.playButton.addClass("aktiv");
                }
            } else {
                if (SoDo.playButton.hasClass("aktiv")) {
                    SoDo.playButton.removeClass("aktiv");
                }
            }
            this.ZonesCount = zcounter;
            if (SoDo.deviceLoader.is(":visible")) {
                SoDo.deviceLoader.hide();
            }
        } catch (fehlernachricht) {
            alert(fehlernachricht + " Fehler bei:" + fehlernachricht.Name + "\n" + "Meldung:" + fehlernachricht.Message);
        }
    };
    this.CheckActiveZone = function() {
        if (SonosZones.CheckStringIsNullOrEmpty(this.ActiveZoneUUID) ||  typeof SonosZones[this.ActiveZoneUUID] === "undefined") {
            return false;
        }
        return true;
    };
    this.ClearRincons = function(zones) {
        for (var j = 0; j < zones.length; j++) {
            var delrin = zones[j];
            delete SonosZones[delrin]; //Löschen, da geändert und evtl. nicht mehr benötigt.
        }  
    };
    this.CheckServerData = function(data) {
        var allrincon = this.ZonesRincon();
        var i;
        for (i = 0; i < data.length; i++) {
            var rincon = data[i].Coordinator.UUID;
            if (typeof SonosZones[rincon] === "undefined") {
                //Wenn nicht definiert ein neuen anlegen	
                SonosZones[rincon] = new SonosZone(rincon, data[i].Coordinator.Name);
            }
            //Zonendaten neu schreiben. Aktiv Status merken.
            var ind = allrincon.indexOf(data[i].UUID);
            allrincon.splice(ind, 1); //einträge raus nehmen
            SonosZones[rincon].SetBySonosItem(data[i]);
        }
        //nun den rest löschen
        for (i = 0; i < allrincon.length; i++) {
            var rin = allrincon[i];
            delete SonosZones[rin];
        }
        //Prüfen, ob der Master nun in einer Gruppe enthalten und nicht mehr wählbar ist.
        if (SonosZones.CheckActiveZone()) {
            allrincon = this.ZonesRincon();
            for (i = 0; i < allrincon.length; i++) {
                var aktuuid = allrincon[i];
                var crdplayer = SonosZones[aktuuid].GetCordinatedPlayer();
                if (crdplayer.length > 0) {
                    for (var cp = 0; cp < crdplayer.length; cp++) {
                        var cpuuid = crdplayer[cp].UUID;
                        if (cpuuid === this.ActiveZoneUUID) {
                            SonosZones[aktuuid].ActiveZone =true; //Neuen Master definieren
                        }
                    }
                }
            }
        }
    };
    this.ZonesRincon = function() {
        var prop = Object.getOwnPropertyNames(SonosZones);
        var _zonesRincon = new Array();
        for (var i = 0; i < prop.length; i++) {
            if (prop[i].substring(0, Checker.length) === Checker) {
                //Es handelt sich um einen Sonosplayer
                _zonesRincon.push(prop[i]);
            }
        }
        return _zonesRincon;
    };
    this.SetActiveZone = function(rincon) {
        //Active Zone wurde gewechselt
        //Alle anderen auf false setzen
        var prop = Object.getOwnPropertyNames(this);
        //var zcounter=0; //Anzahl der Zonen;
        for (var i = 0; i < prop.length; i++) {
            if (prop[i].substring(0, Checker.length) === Checker && prop[i] !== rincon) {
                //Es handelt sich um einen Sonosplayer
                var p = prop[i];
                SonosZones[p].ActiveZone =false;
            }
        }
        this.ActiveZoneUUID = rincon;
        this.ActiveZoneName = SonosZones[rincon].ZoneName;
        SoDo.volumeSlider.slider({ value: SonosZones[rincon].CordinatorVolume }); //Lautstärke setzen
        SoDo.labelVolume.html(SonosZones[rincon].CordinatorVolume);
        SoDo.runtimeSlider.slider("option", "max", SonosZones[rincon].CurrentDuration.valueOf());
        SoDo.runtimeSlider.slider("option", "value", SonosZones[rincon].CurrentRelTime.valueOf());
        if (SonosZones[rincon].PlayMode !== "INITIAL") {
            SetPlaymodeDivs(SonosZones[rincon].PlayMode); //AbspielModus im Layout setzen.
        }
        if (SonosZones[rincon].FadeMode === true) {
            if (!SoDo.fadeButton.hasClass("aktiv")) {
                SoDo.fadeButton.addClass("aktiv");
            }
        } else {
            if (!SoDo.fadeButton.hasClass("aktiv")) {
                SoDo.fadeButton.removeClass("aktiv");
            }
        }
        if (SonosZones[rincon].PlayState === "PLAYING") {
            if (!SoDo.playButton.hasClass("aktiv")) {
                SoDo.playButton.addClass("aktiv");
            }
        } else {
            if (SoDo.playButton.hasClass("aktiv")) {
                SoDo.playButton.removeClass("aktiv");
            }
        }
        if (SonosZones[rincon].SleepMode !== "" && SonosZones[rincon].SleepMode !== "aus" && SonosZones[rincon].SleepMode !== "INITIAL") {
            if (!SoDo.sleepModeButton.hasClass("aktiv")) {
                SoDo.sleepModeButton.addClass("aktiv");
            }
            if (SoDo.sleepModeState.text() !== SonosZones[rincon].SleepMode) {
                SoDo.sleepModeState.text(SonosZones[rincon].SleepMode);
            }
        } else {
            if (SoDo.sleepModeButton.hasClass("aktiv")) {
                SoDo.sleepModeButton.removeClass("aktiv");
            }
            if (SoDo.sleepModeState.text() !== "") {
                SoDo.sleepModeState.text("");
            }
        }
        //Playlist
        if (SonosZones[rincon].Playlist.Playlist.length === 0 || SonosZones[rincon].Playlist.Playlist.length !== SonosZones[rincon].NumberOfTracks) {
            //Hier Playlist laden
            if (SonosZones[rincon].PlaylistLoader === false) {
                SonosZones[rincon].SetPlaylist(true,"SetActiveZone");
            }
        } else {
            if (SonosZones[rincon].CurrentTrack.ClassType !== "object.item.audioItem.audioBroadcast") {
                SonosZones[rincon].RenderPlaylist("SonosZones:SetActiveZone");
            }
        }
        SonosZones[rincon].RenderTrackTime();
        //CurrentTRack
        SonosZones[rincon].CurrentTrack.RenderCurrentTrack(SonosZones[rincon].CurrentTrackNumber);
        if (SonosZones[rincon].Mute === true) {
            if (!SoDo.MuteButton.hasClass("aktiv")) {
                SoDo.MuteButton.addClass("aktiv");
            }
        } else {
            if (SoDo.MuteButton.hasClass("aktiv")) {
                SoDo.MuteButton.removeClass("aktiv");
            }
        }
        SonosZones[rincon].RenderAudioIn();
        SonosZones[rincon].RenderRatingFilter();
        SetCurrentPlaylistSong(SonosZones[rincon].CurrentTrackNumber, "SetActiveZone");
    };
    this.SetFirstZonetoActive = function() {
        //Alle Player durchlaufen und den ersten auf activ setzen, da noch keiner gewählt wurde.
        var prop = Object.getOwnPropertyNames(this);
        //var zcounter=0; //Anzahl der Zonen;
        for (var i = 0; i < prop.length; i++) {
            if (prop[i].substring(0, Checker.length) === Checker) {
                //Es handelt sich um einen Sonosplayer
                var p = prop[i];
                SonosZones[p].ActiveZone =true;
                break;
            }
        }

    };
    this.SetZonetoActiveByName = function(s) {
        //Alle Player durchlaufen und nach Namen Prüfen
        var prop = Object.getOwnPropertyNames(this);
        //var zcounter=0; //Anzahl der Zonen;
        var found = false;
        for (var i = 0; i < prop.length; i++) {
            if (prop[i].substring(0, Checker.length) === Checker) {
                //Es handelt sich um einen Sonosplayer
                var p = prop[i];
                var tname = SonosZones[p].ZoneName.toLowerCase();
                if (tname === s) {
                    SonosZones[p].ActiveZone = true;
                    found = true;
                    break;
                } else {
                    var corplayer = SonosZones[p].GetCordinatedPlayer();
                    if (corplayer.length > 0) {
                        for (var cp = 0; cp < corplayer.length; cp++) {
                            var tname2 = corplayer[cp].Name.toLowerCase();
                            if (tname2 === s) {
                                SonosZones[p].ActiveZone=true;
                                found = true;
                                break;
                            }
                        }
                    }
                }
                if (found === true) {
                    break;
                }
            }
        }
        if (!found) {
            SonosLog("Es wurde Device übergeben aber kein Player gefunden! Daher wird die erste Zone als Aktiv markiert.");
            this.SetFirstZonetoActive();
        }
    };
    this.CheckStringIsNullOrEmpty = function(s) {
        if (typeof s === "undefined" || s === null || s === "leer" || s === "Leer" || s === "") return true;

        return false;
    }
}