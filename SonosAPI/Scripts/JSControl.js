"use strict";
window.onerror = Fehlerbehandlung;
var debug = false; //Wenn true wird kein Refesh gemacht		
var showerrors = false; //Wenn auf true, wird ein Button eingebunden und die Console logt zusätzlich in ein DIV welches man über den button öffnen kann.
var wroteDebugInfos = false;
function Fehlerbehandlung(Nachricht, Datei, Zeile) {
    var fehler = "Fehlermeldung:\n" + Nachricht + "\n" + Datei + "\n" + Zeile;
    alert(fehler);
    return true;
}
function WroteSysteminfos() {
    var fehler = "SonosZones:ActiveSonosZone:" + SonosZones.ActiveZoneUUID + "<br />" +
        "SonosZones:ActiveName:" + SonosZones.ActiveZoneName + "<br />" +
        "SonosPlayer:Name:" + SonosZones[SonosZones.ActiveZoneUUID].ZoneName + "<br />" +
        "SonosPlayer:Baseurl:" + SonosZones[SonosZones.ActiveZoneUUID].BaseURL + "<br />" +
        "SonosPlayer:NumberofTRacks:" + SonosZones[SonosZones.ActiveZoneUUID].NumberOfTracks + "<br />" +
        "SonosPlayer:CurrentTRackTitel:" + SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Title + "<br />" +
        "SonosPlayer:Active:" + SonosZones[SonosZones.ActiveZoneUUID].ActiveZone + "<br />" +
        "GetTopologieID:" + SoVa.TopologieChangeID + "<br />" +
        "GetAktsongInfoID:" + SoVa.GetAktSongInfoTimerID + "<br />" +
        "AktuellerSong aus der Playlist:" + SoVa.aktcurpopdown + "<br />" +
        "API PlayerURL:" + SoVa.apiPlayerURL + "<br />";
    return fehler;
}
function WroteDebugInfos() {
    if (wroteDebugInfos === false) {
        SoDo.lyricWrapper.empty();
    }
    wroteDebugInfos = true;
    if (typeof SoDo.lyric === "undefined") {
        SoDo.lyric = $("#Lyricbox");
    }
    if (typeof SoDo.lyricWrapper === "undefined") {
        SoDo.lyricWrapper = $("#Lyricboxwrapper");
    }
    SonosWindows(SoDo.lyric);
    if (SoDo.lyric.is(":visible")) {
        $('<div>' + WroteSysteminfos() + '</div>').appendTo(SoDo.lyricWrapper);
    }
}
function AddDebugInfos(mess) {
    if (wroteDebugInfos === true) {
        $('<div>' + mess + '</div>').appendTo(SoDo.lyricWrapper);
    }
}
/*
Erklärungen:
		
URL Parameter
Mit dem URL Parameter device kann man eine vorauswahl treffen, welches Gerät ausgewählt werden soll.
Beispiel: device=Wohnzimmer
Ohne den Parameter wird immer der erste Eintrag vorausgewählt
		
Java Variablen
apiDeviceURL

Dieser Parameter wird genommen um die API zu initialisieren. Diese liefert die Geräte zurück
apiPlayerURL	
Dieser Parameter liefert alle Informationen zurück und wird für fast alle Functionen benötigt
*/


//Dokument Ready
$(document).ready(function () {
    //SonosLog("Document Ready");
    window.SoDo = new SonosDOMObjects();
    window.SoVa = new SonosVariablen();
    if (document.body.clientWidth < 770) {
        SoVa.smallDevice = true;
    }
    window.SonosZones = new SonosZonesObject();
    SoDo.errorloggingDOM.on("click", function () {
        SonosWindows(SoDo.errorlogging);
    });
    $("#Apps").on("click", function() {
        var al = $("#AppList");
        al.toggle(200,function() {
            if (al.is(":hidden")) {
                $("#AppIframe").hide();
            }
        });

    });
    if (showerrors === true) {
        SoDo.SetErrorLogging();
        SoDo.errorloggingDOM.show();
    }
    LoadDevices();
    $(window).on("resize", function () {
        SetHeight();
    });
    $("#AppIframeClose").on("click", function() {
        $("#AppIframeWrapper").hide(100);
    });
    SoDo.lyricButton.on("click", function () {
        ShowPlaylistLyricCurrent();
    });
    //Changes für das Exportieren /Speichern von Playlisten festhalten
    SoDo.saveExportPlaylistSwitch.on("change", function () {
        var c = SoDo.saveExportPlaylistSwitch.prop("checked");
        if (c) {
            SoDo.saveQueue.attr("placeholder", SoVa.exportPlaylistInputText);
            SoVa.exportplaylist = true;
        } else {
            SoDo.saveQueue.attr("placeholder", SoVa.savePlaylistInputText);
            SoVa.exportplaylist = false;
        }
    });
    //ScrollEvents
    //Prüfvariable wird gesetzt, wenn gescrollt wird. Manuelles Scrollen
    SoDo.currentplaylistwrapper.on("scroll", function() {
        SoVa.currentplaylistScrolled = true;
    });
    //Prüfen ob nur noch current Ratings angezeigt werden soll.
    SoDo.onlyCurrentSwitch.on("change", function () {
        var c = SoDo.onlyCurrentSwitch.prop("checked");
        if (c) {
            SoVa.ratingonlycurrent = true;
        } else {
            SoVa.ratingonlycurrent = false;
        }
    });
    //Initialisierung Musikindexaktualisierung
    SoDo.musikIndex.on("click", function () { UpdateMusicIndex(); });
    //Ratingmine änderunbgen abfangen
    SoDo.ratingMineSelector.on("change", function () {
        SetRatinMineSelection(SoDo.ratingMineSelector.find("option:selected").val());
    });
    SoDo.BewertungsFilterButton.on("click", function () { SonosWindows(SoDo.filterListBox); });
    //Settingswurde gedrückt
    SoDo.settingsbutton.on("click", function () {
        SonosWindows(SoDo.settingsBox);
        SoDo.settingsbutton.toggleClass("akt");
    });
    //Settingswurde gedrückt
    SoDo.settingsClosebutton.on("click", function () {
        SonosWindows(SoDo.settingsBox,true);
        SoDo.settingsbutton.toggleClass("akt");
    });
    SoDo.BrowseClosebutton.on("click", function () {
        BrowsePress();
        console.log("ok");
    });
    //Events verarbeiten, wenn ein Button geklickt wurde.
    SoDo.nextButton.on("click", function () {
        //Curenttrack ändern, danach die Nummer Ändern und somit den Nexttrack rendern.
        if (SonosZones.CheckActiveZone()) {
            SoVa.currentplaylistScrolled = false;
            SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber =(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber + 1);
            SonosZones[SonosZones.ActiveZoneUUID].SetCurrentTrack(SonosZones[SonosZones.ActiveZoneUUID].Playlist.Playlist[(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber - 1)], "Nextclick");
            doit('Next');
            if (SoVa.ratingonlycurrent === false && SoDo.ratingListBox.is(":visible")) {
                SonosWindows(SoDo.ratingListBox, true);
            }
        }
    });
    //Events verarbeiten, wenn ein Button geklickt wurde.
    SoDo.prevButton.on("click", function () {
        if (SonosZones.CheckActiveZone()) {
            SoVa.currentplaylistScrolled = false;
            if ((SonosZones[SonosZones.ActiveZoneUUID].PlayMode !== "REPEAT_ALL" && SonosZones[SonosZones.ActiveZoneUUID].PlayMode !== "SHUFFLE") && SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber === 1) {
                return false;
            }
            SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber =(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber - 1);
            SonosZones[SonosZones.ActiveZoneUUID].SetCurrentTrack(SonosZones[SonosZones.ActiveZoneUUID].Playlist.Playlist[SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber], "PreClick");
            doit('Previous');
            SonosWindows(SoDo.ratingListBox, true);
            return true;
        }
        return true;
    });
    //Abspieldauerslider
    SoDo.runtimeSlider.slider({
        orientation: "horizontal",
        range: "min",
        min: 0,
        max: 100,
        value: 50,
        stop: function (event, ui) {
            SonosZones[SonosZones.ActiveZoneUUID].CurrentDurationSliderActive = false;
            SonosZones[SonosZones.ActiveZoneUUID].CurrentDuration.set(ui.value);
            doitValue("Seek", SonosZones[SonosZones.ActiveZoneUUID].CurrentDuration.toString());

        }, start: function () {
            SonosZones[SonosZones.ActiveZoneUUID].CurrentDurationSliderActive = true;
        },
        slide: function (event, ui) {
            SonosZones[SonosZones.ActiveZoneUUID].CurrentRelTime.set(ui.value);
            SoDo.runtimeRelTime.html(SonosZones[SonosZones.ActiveZoneUUID].CurrentRelTime.toString());
        }
    });

    //Lautstärkeregler initialisieren.
    SoDo.volumeSlider.slider({
        orientation: "vertical",
        range: "min",
        min: 1,
        max: 100,
        value: 1,
        stop: function (event, ui) {
            //Prüfen, ob die Läutstärke über 80% verändert wird. 
            if (ui.value > SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume && (ui.value - SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume) > SoVa.VolumeConfirmCounter) {
                var answer = confirm("Du willst die Lautstärke um " + SoVa.VolumeConfirmCounter + " von 100 Schritten erhöhen. Klicke Ok, wenn das gewollt ist");
                if (!answer) {
                    SoDo.labelVolume.html(SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume);
                    SoDo.volumeSlider.slider({ value: SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume });
                    return false;
                }
            }
            SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume =ui.value;
            SetVolumeDevice(SonosZones.ActiveZoneUUID, ui.value);
            return true;
        },
        slide: function (event, ui) {
            SoDo.labelVolume.html(ui.value);
        }
    });
    //Autovervollständigung
    SoDo.saveQueue.keyup(function (e) {
        // 'enter' key was pressed
        var $suggest = SoDo.suggestionInput;
        var code = (e.keyCode ? e.keyCode : e.which);
        if (code === 13) {
            $(this).val($suggest.val());
            $suggest.val("");
            return false;
        }

        // some other key was pressed
        var needle = $(this).val();

        // is the field empty?
        if (!$.trim(needle).length) {
            $suggest.val("");
            return false;
        }

        // compare input with haystack
        for (var i = 0; i < SoVa.allplaylist.length; i++) {
            var regex = new RegExp('^' + needle, 'i');
            if (regex.test(SoVa.allplaylist[i])) {
                $suggest.val(needle + SoVa.allplaylist[i].slice(needle.length));
                // use first result
                return false;
            }
            $suggest.val("");
        }
        return false;
    });

    SetHeight();
    if (wroteDebugInfos === true) {
        SoDo.debug.show();
    }
    SonosLog("Document Ready Ende");

});     //ENDE DOK READY

//Gerät Url ermitteln und laden
var GetZonesTimer = 0;
function LoadDevices() {
    SonosLog("Geräte laden");
    SoVa.urldevice = GetURLParameter('device');
    SonosAjax("LoadDevice").done(function (data) {
        if (data !== "Ready") {
                    //alert("LoadDevices: "+data);
            LoadDevices();
            return;
        }
                SoVa.GetZonesTimer = window.setTimeout("GetZones()", 500);
            if (SoDo.playlistwrapper.children().length === 0) {
                window.setTimeout("GetPlaylists()", 1000);
            }
            //nun den TopologieChanger alle 5 Sekunden aufrufen und nach aktualisierungen prüfen. 
                if (debug === false) {
                    clearTimeout(SoVa.TopologieChangeID);
                    clearTimeout(SoVa.GetAktSongInfoTimerID);
                    SoVa.TopologieChangeID = window.setTimeout("GetTopologieChange()", 3000);
                    SoVa.GetAktSongInfoTimerID = window.setTimeout("GetAktSongInfo()", 1000);
                }
                SonosZones.Refreshstop = false;
                SonosLog("Geräte geladen");
            });
} //Ende LoadDevices
/*Zonemanagement*/
function GetZones() {
    SonosLog("Zones laden");
    clearTimeout(SoVa.GetZonesTimer);
    SonosAjax("GetCoordinator").success(function (data) {
        if (data === null || typeof data === "undefined" || data.length === 0 || data[0].ZoneName === "Leer") {
            SoVa.GetZonesTimer = window.setTimeout("GetZones()", 800);
            SonosLog("Zones leer beim Laden");
            return;
        }
        SoDo.groupDeviceShow.show();
        SoDo.deviceLoader.show();
        //hier alle vorhandenen durchlaufen.
        var devplayer = $(".device");
        $.each(devplayer, function (i, item) {
            var found = false;
            var id = item.id;
            for (var i2 = 0; i2 < data.length; i2++) {
                if (id === data[i2].CoordinatorUUID) {
                    found = true;
                    break;
                }
            }
            if (found === false) {
                item.parentNode.remove();
                delete SonosZones[id];
            }
        });
        var renderZones = false;
        for (var i = 0; i < data.length; i++) {
            //Nun durch alle vom Server durchlaufen
            var rincon = data[i].Coordinator.UUID;
            if (typeof SonosZones[rincon] !== "undefined") {
                //Zone Existiert.
                if (parseInt($("#" + data[i].CoordinatorUUID).attr("data-players")) !== data[i].Players.length) {
                    //Anzahl der Player hat sich geändert.
                    renderZones = true;
                }
                SonosZones[rincon].SetBySonosItem(data[i]);
            } else {
                SonosZones[rincon] = new SonosZone(rincon, data[i].Coordinator.Name);
                SonosZones[rincon].SetBySonosItem(data[i]);
                if (renderZones === false) {
                    renderZones = true;
                }
            }
        }
        if (!SonosZones.CheckActiveZone()) {
            SonosLog("Zones SetActiveZone Start");
            if (SoVa.urldevice === "" || SoVa.urldevice === "leer") {
                SonosLog("Zones SetActiveZone First Start");
                SonosZones.SetFirstZonetoActive();
            } else {
                SonosLog("Zones SetActiveZone by Name Start");
                SonosZones.SetZonetoActiveByName(SoVa.urldevice);
            }
            SonosLog("Zones SetActiveZone Ende");
            SonosLog("Zones SetDevice");
        } else {
            SonosZones[SonosZones.ActiveZoneUUID].ActiveZone = true;
        }
        if (renderZones === true) {
            window.setTimeout("SonosZones.RenderZones()", 500);
            window.setTimeout("SonosZones.CheckRendering()", 1000);
        } else {
            if (SoDo.deviceLoader.is(":visible")) {
                SoDo.deviceLoader.hide();
            }
        }
        SonosZones.Refreshstop = false;
        SonosLog("Zones geladen");

        return;
    }).fail(function (jqXHR) {
        if (jqXHR.statusText === "Internal Server Error") {
            ReloadSite("GetZones");
        } else { alert("Beim laden der Zonen ist ein Fehler aufgetreten."); ReloadSite("GetZones"); }
    });
}
//Erweitert das Device Fenster um die Gruppen Buttons
function GroupDeviceShow() {
    SonosLog("GroupDeviceShow");
    if (SoVa.groupDeviceShowBool === false) {
        SoDo.devicesWrapper.addClass("groupdevicesshown").css("z-index", SoVa.szindex);
        SoDo.devicesWrapper.animate({ width: "360px" }, 500, function () {
            $(".groupdeviceclass").css("display", "table");
            SoDo.groupDeviceShow.text("<<");
        });
        SoDo.devices.animate({ width: "360px" }, 500);
        SoVa.groupDeviceShowBool = true;
        SonosZones.Refreshstop = true;
    } else {
        $(".groupdeviceclass").css("display", "none");
        SoDo.devicesWrapper.animate({ width: "165px" }, 1000, function () {
            SoDo.devicesWrapper.removeClass("groupdevicesshown").css("z-index", 100);
            SoDo.groupDeviceShow.text(">>");
        });
        SoDo.devices.animate({ width: "165" }, 1000);
        SoVa.groupDeviceShowBool = false;
        SonosZones.Refreshstop = false;
    }
    SonosLog("GroupDeviceShow Ende");
}
//Auswahlliste um Gruppen zu bilden.
function SetDeviceGroupFor(v) {
    SonosLog("SetDeviceGroupFor");
    if (SoVa.setGroupMemberInitialisierung === false) {
        SoDo.setGroupMembers.empty();
        $('<br>').appendTo(SoDo.setGroupMembers);
        var prop = Object.getOwnPropertyNames(SonosZones.PlayersUUIDToNames);
        for (var i = 0; i < prop.length; i++) {
            var p = prop[i];
            $('<div class="groupcheck"><input type="checkbox" id="groupcheckchecker_' + p + '" class="groupcheckchecker" value="' + p + '"><span onclick="$(this).parent().children(\'INPUT\').prop(\'checked\', !$(this).parent().children(\'INPUT\').prop(\'checked\'));">' + SonosZones.PlayersUUIDToNames[p] + '</span></div>').appendTo(SoDo.setGroupMembers);
        }
        $('<div id="Groupcheckset" onclick="SetGroup()">Set</div>').appendTo(SoDo.setGroupMembers);
        $('<div id="GroupCheckClose" onclick="HideGroupFor()">X</div>').appendTo(SoDo.setGroupMembers);
        SoVa.setGroupMemberInitialisierung = true;
    }
    $(".groupcheckchecker").prop('checked', false);

    $("#groupcheckchecker_" + v).prop('checked', true);
    //Zonen durchlaufen
    var cordzones = SonosZones[v].GetCordinatedPlayer();
    for (var z = 0; z < cordzones.length; z++) {
        $("#groupcheckchecker_" + cordzones[z].UUID).prop('checked', true);
    }
    SoVa.masterPlayer = v;
    SonosWindows(SoDo.setGroupMembers, false, { overlay: true, selecteddivs: [SoDo.nextButton, SoDo.playButton] });
}
//Schließen der Gruppenauswahl
function HideGroupFor() {
    SonosLog("HideGroupFor");
    SonosWindows(SoDo.setGroupMembers, true);
}
//Setzen der Gruppen.
function SetGroup() {
    SonosLog("SetGroup");
    SonosZones.Refreshstop = true;
    HideGroupFor();
    //setGroupMemberInitialisierung=false;
    GroupDeviceShow();
    //Ausgewählte ermitteln
    var g = [];

    $('.groupcheckchecker:checkbox:checked').each(function () {
        g.push($(this).val());
    });

    if (g.length === 0) {
        var ret = confirm("Wenn nicht mindestens ein Player ausgewählt ist, werden alle Player angehalten. Soll das passieren?");
        if (ret === false) return;
        g = "leer";
    }

    //Daten senden
    SonosAjax("SetGroups",{ '': g }).success(function () {
        if (g !== "leer") {
            SonosZones.ClearRincons(g);
        }
        SonosZones.Refreshstop = false;
    });
    SonosLog("SetGroup Ende");
}
//Setzt den entsprechenden Player als Abspieler
function SetDevice(dev) {
    SonosLog("SetDevice");
    //Volumne Bar reseten.
    SonosWindows(SoDo.multiVolume, true);
    SonosWindows(SoDo.ratingListBox, true);
    SoDo.onlyCurrentSwitch.prop("checked", false);
    SoVa.ratingonlycurrent = false;
    SoDo.devicesWrapper.children("DIV").children("DIV").removeClass("akt_device");
    $("#" + dev).addClass("akt_device");
    SonosZones[dev].ActiveZone = true;
    document.title = 'Sonos::' + SonosZones.ActiveZoneName;
    SonosLog("SetDevice Ende");
} //Ende SetDevice
//Sucht den übergebenen Parameter in der URL um ein Device auszuwählen.
function GetURLParameter(sParam) {
    SonosLog("GetURLParameter");
    var sPageURL = window.location.search.substring(1);
    var sURLVariables = sPageURL.split('&');
    for (var i = 0; i < sURLVariables.length; i++) {
        var sParameterName = sURLVariables[i].split('=');
        if (sParameterName[0] === sParam) {
            SonosLog("Found Device URLParameter:" + sParameterName[1]);
            return decodeURIComponent(sParameterName[1]);
        }
    }
    return "leer";
}
//Wenn auf Play gedrückt wird.
function PlayPress() {
    SonosLog("PlayPress");
    if (SonosZones.CheckActiveZone()) {
        SonosLog("PlayPress State:" + SonosZones[SonosZones.ActiveZoneUUID].PlayState);
        if (SonosZones[SonosZones.ActiveZoneUUID].PlayState === "PLAYING") {
            SonosZones[SonosZones.ActiveZoneUUID].PlayState ="PAUSED_PLAYBACK";
        } else {
            SonosZones[SonosZones.ActiveZoneUUID].PlayState="PLAYING";
        }
    }
    SonosLog("PlayPress Done");
}
//Es wurde einmal Play aus der Playlist gedrückt
function PlayPressSmall(k) {
    var playid = $(k).parent().parent().attr("id");
    SonosLog("PlayPressSmall:" + playid);
    var PressKey = (GetIDfromCurrentPlaylist(playid) + 1);
    SonosLog("PlayPressSmall:" + PressKey);
    if (SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber === PressKey) {
        //Play bei aktuellem Song
        SonosLog("PlayPressSmall läd PlayPress");
        PlayPress();
    } else {
        SonosLog("PlayPressSmall läd doitValue");
        SonosZones[SonosZones.ActiveZoneUUID].SetCurrentTrack(SonosZones[SonosZones.ActiveZoneUUID].Playlist.Playlist[(PressKey - 1)], "PlaypressSmall");
        SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber =PressKey;
        SonosZones[SonosZones.ActiveZoneUUID].PlayState ="PLAYING";
        doitValue("SetSongInPlaylist", PressKey);
    }

}
//Setzen des Wiedergabemodus
function SetPlaymode(v) {
    SonosLog("SetPlaymode:" + v);
    SonosZones[SonosZones.ActiveZoneUUID].PlayMode =v;
}
//Setzen des Layout des Wiedergabemodus
function SetPlaymodeDivs(v) {
    switch (v) {
        case "NORMAL":
            if (SoDo.repeatButton.hasClass("aktiv")) {
                SoDo.repeatButton.removeClass("aktiv");
            }
            if (SoDo.shuffleButton.hasClass("aktiv")) {
                SoDo.shuffleButton.removeClass("aktiv");
            }
            break;
        case "REPEAT_ALL":
            if (!SoDo.repeatButton.hasClass("aktiv")) {
                SoDo.repeatButton.addClass("aktiv");
            }
            if (SoDo.shuffleButton.hasClass("aktiv")) {
                SoDo.shuffleButton.removeClass("aktiv");
            }
            break;
        case "SHUFFLE_NOREPEAT":
            if (SoDo.repeatButton.hasClass("aktiv")) {
                SoDo.repeatButton.removeClass("aktiv");
            }
            if (!SoDo.shuffleButton.hasClass("aktiv")) {
                SoDo.shuffleButton.addClass("aktiv");
            }
            break;
        case "SHUFFLE":
            if (!SoDo.repeatButton.hasClass("aktiv")) {
                SoDo.repeatButton.addClass("aktiv");
            }
            if (!SoDo.shuffleButton.hasClass("aktiv")) {
                SoDo.shuffleButton.addClass("aktiv");
            }
            break;
        default:
            alert("SetPlaymodeDivs:" + v);
    }
}
//Übergangbutton geklickt.
function SetFade() {
    SonosLog("SetFade");
    SonosAjax("SetFadeMode");
    SonosZones[SonosZones.ActiveZoneUUID].FadeMode = !SonosZones[SonosZones.ActiveZoneUUID].FadeMode;
}
//{ Lautstärke
//Setzt Mute
function SetMute() {
    SonosLog("SetMute");
    doit('SetMute');
    SoDo.MuteButton.toggleClass("aktiv");
}
//Lautstärke anpassen
function SetVolume(k) {
    SonosLog("SetVolume");
    //Multivolume
    k = parseInt(k);
    var cordplayer = SonosZones[SonosZones.ActiveZoneUUID].GetCordinatedPlayer();
    if (cordplayer.length > 0) {
        SonosLog("SetVolume Multi");
        SoDo.multiVolume.empty();
        //SonosWindows(multiVolumeDIV,false);
        SonosWindows(SoDo.multiVolume, false, { overlay: true, selecteddivs: [SoDo.playButton, SoDo.MuteButton, SoDo.nextButton] });
        var mvc = $('<div id="multivolume_close">X</DIV>').appendTo(SoDo.multiVolume);
        mvc.on("click", function () { SonosWindows(SoDo.multiVolume, true); });
        //PrimärPlayer zufügen
        $('<div id="MultivolumePrimary">' + SonosZones.ActiveZoneName + '<DIV id="MultivolumesliderPrimary" class="multivolumeslider"></div><div class="multivolumesliderVolumeNumber" id="MultivolumePrimaryNumber">' + SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume + '</DIV></DIV>').appendTo(SoDo.multiVolume);
        $("#MultivolumesliderPrimary").slider({
            orientation: "horizontal",
            range: "min",
            min: 1,
            max: 100,
            value: SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume,
            stop: function (event, ui) {
                //Prüfen, ob die Läutstärke über 80% verändert wird. 
                if (ui.value > SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume && (ui.value - SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume) > SoVa.VolumeConfirmCounter) {
                    var answer = confirm("Du willst die Lautstärke um " + SoVa.VolumeConfirmCounter + " von 100 Schritten erhöhen. Klicke Ok, wenn das gewollt ist");
                    if (!answer) {
                        SoDo.labelVolume.html(SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume);
                        $("#MultivolumesliderPrimary").slider({ value: SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume });
                        SoDo.volumeSlider.slider({ value: SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume });
                        return false;
                    }
                }
                SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume =ui.value;
                SoDo.volumeSlider.slider({ value: ui.value });
                $("#MultivolumePrimaryNumber").html(ui.value);
                SetVolumeDevice(SonosZones.ActiveZoneUUID, ui.value);
                return true;
            },
            slide: function (event, ui) {
                SoDo.volumeSlider.slider({ value: ui.value });
                SoDo.labelVolume.html(ui.value);
                $("#MultivolumePrimaryNumber").html(ui.value);
            }
        });
        //Ende Primärplayer nun rest
        $.each(cordplayer, function (i, item) {
            SonosLog("SetVolume CordinatedPlayerName:" + item.Name);
            $('<div id="multivolume_' + item.UUID + '">' + item.Name + '<DIV id="Multivolumeslider_' + item.UUID + '" class="multivolumeslider"></div><div class="multivolumesliderVolumeNumber" id="MultivolumesliderVolumeNumber_' + item.UUID + '">' + item.Volume + '</div></DIV>').appendTo(SoDo.multiVolume);
            $("#Multivolumeslider_" + item.UUID).slider({
                orientation: "horizontal",
                range: "min",
                min: 1,
                max: 100,
                value: item.Volume,
                stop: function (event, ui) {
                    //Prüfen, ob die Läutstärke über 80% verändert wird. 
                    if (ui.value > item.Volume && (ui.value - item.Volume) > SoVa.VolumeConfirmCounter) {
                        var answer = confirm("Du willst die Lautstärke um " + SoVa.VolumeConfirmCounter + " von 100 Schritten erhöhen. Klicke Ok, wenn das gewollt ist");
                        if (!answer) {
                            $("#Multivolumeslider_" + item.UUID).slider({ value: item.Volume });
                            $("#MultivolumesliderVolumeNumber_" + item.UUID).html(item.Volume);
                            return false;
                        }
                    }
                    item.Volume = ui.value;
                    $("#MultivolumesliderVolumeNumber_" + item.UUID).html(ui.value);
                    SetVolumeDevice(item.UUID, ui.value);
                    return true;
                },
                slide: function (event, ui) {
                    $("#MultivolumesliderVolumeNumber_" + item.UUID).html(ui.value);
                }
            });
        });
        //Hier nun den Player für alle machen.
        $('<div id="MultivolumeAll">Alle<DIV id="MultivolumesliderAll" class="multivolumeslider"></div><div class="multivolumesliderVolumeNumber" id="MultivolumeAllNumber">' + SonosZones[SonosZones.ActiveZoneUUID].GroupVolume + '</DIV></DIV>').prependTo(SoDo.multiVolume);
        $("#MultivolumesliderAll").slider({
            orientation: "horizontal",
            range: "min",
            min: 1,
            max: 100,
            value: SonosZones[SonosZones.ActiveZoneUUID].GroupVolume,
            stop: function (event, ui) {
                //Prüfen, ob die Läutstärke über 80% verändert wird. 
                if (ui.value > SonosZones[SonosZones.ActiveZoneUUID].GroupVolume && (ui.value - SonosZones[SonosZones.ActiveZoneUUID].GroupVolume) > SoVa.VolumeConfirmCounter) {
                    var answer = confirm("Du willst die Lautstärke um " + SoVa.VolumeConfirmCounter + " von 100 Schritten erhöhen. Klicke Ok, wenn das gewollt ist");
                    if (!answer) {
                        $("#multivolumesliderAll").slider({ value: SonosZones[SonosZones.ActiveZoneUUID].GroupVolume });
                        return false;
                    }
                    SetGroupVolumeDevice(SonosZones.ActiveZoneUUID, ui.value);
                    return true;
                }
                SetGroupVolumeDevice(SonosZones.ActiveZoneUUID, ui.value);
                return true;
            },
            slide: function (event, ui) {
                $("#MultivolumeAllNumber").html(ui.value);
            }
        });
    } else {
        SonosLog("SetVolume Only Current");
        //Steps von 5 oder 1
        var v = 5;
        if (SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume >= 90 || SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume <= 20) {
            v = 1;
        }
        var newvolume;
        if (k === 1) {
            newvolume = SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume + v;
        } else {
            newvolume = SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume - v;
        }
        SonosZones[SonosZones.ActiveZoneUUID].CordinatorVolume =newvolume;
        SetVolumeDevice(SonosZones.ActiveZoneUUID, newvolume);
    }
}
//Setzt die Lautstärke für ein spezielles Gerät.
function SetVolumeDevice(dev, v) {
    SonosLog("SetVolumeDevice:" + dev + " Volume:" + v);
    SonosAjax("SetVolume", "", dev, v).fail(function () { alert("Beim setzen der Lautstäke für Player " + dev + " ist ein Fehler aufgetreten."); });
}
function SetGroupVolumeDevice(dev, v) {
    SonosLog("SetVolumeDevice:" + dev + " Volume:" + v);
    SonosAjax("SetGroupVolume", "", dev, v).fail(function () { alert("Beim setzen der Lautstäke für Player " + dev + " ist ein Fehler aufgetreten."); });
}
//} Lautstärke

//{ Aktuelle Wiedergabeliste
//Speichern/Exportieren der aktuellen Playlist
function SaveQueue() {
    SonosLog("SaveQueue Export:" + SoVa.exportplaylist);
    var title = SoDo.saveQueue.val();
    var queuetype = "SaveQueue";
    if (title.length > 0) {
        SoDo.saveQueueLoader.show();
        if (SoVa.exportplaylist === true) {
            queuetype = "ExportQueue";
        }

        var request = SonosAjax(queuetype, { '': title });
        request.success(function (data) {
            if (data === true) {
                GetPlaylists();
                SonosLog("SaveQueue Done:" + data);
            } else {
                alert("Beim laden der Aktion:" + queuetype + "(" + title + ") ist ein Fehler aufgetreten.");
            }
            SoDo.saveQueueLoader.hide();
        });
        request.fail(function (jqXHR) {
            if (jqXHR.statusText === "Internal Server Error") {
                ReloadSite("SaveQueue");
            } else { alert("Beim laden der Aktion:SaveQueue(" + title + ") ist ein Fehler aufgetreten."); }
            SoDo.saveQueueLoader.hide();
        });
    }
}
//Items der Playlist zufügen
function AddToPlaylist(item) {
    SonosLog("AddToPlaylist");
    var uri = $(item).parent().attr("data-containerid");
    SonosLog("AddToPlaylist URI:" + uri);
    var request = SonosAjax("Enqueue", { '': uri });
    request.success(function () {
        SonosZones[SonosZones.ActiveZoneUUID].SetPlaylist(true,"AddToPlaylist");
    });
    request.fail(function (jqXHR) {
        if (jqXHR.statusText === "Internal Server Error") {
            ReloadSite("AddToPlaylist");
        } else { alert("Beim laden der Aktion:AddToPlaylist ist ein Fehler aufgetreten."); }
    });
}
//Setzen des Songs in der aktuellen Playlist
function SetCurrentPlaylistSong(apsnumber, source) {
    if (typeof apsnumber == "undefined" || apsnumber == null) {
        if (typeof SonosZones[SonosZones.ActiveZoneUUID] !== "undefined" && typeof SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber !== "undefined" && SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber !== null && SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber !== 0) {
            apsnumber = SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber;
        } else {
            SonosLog("SetCurrentPlaylistSong nicht möglich, da kein Currenttrack übergeben wurde bzw. ermittelt werden konnte.");
            return false;
        }
    } else {
        apsnumber = parseInt(apsnumber);
    }
    if (typeof source === "undefined") {
        source = "Unbekannt";
    }
    SonosLog("SetCurrentPlaylistSong:" + apsnumber + " Quelle:" + source);
    var contactTopPosition = $("#Currentplaylist_" + (apsnumber - 1));
    if (contactTopPosition.length !== 0) {
        //prüfen, ob es sich um den selben Song handelt.
        var NewEntry = $("#Currentplaylist_" + (apsnumber - 1) + " > .currentrackinplaylist");
        if (NewEntry.hasClass("aktsonginplaylist")) {
            return false;
        }
        $(".currentrackinplaylist").removeClass("aktsonginplaylist");
        NewEntry.addClass("aktsonginplaylist");
        $(".playlistplaysmall").removeClass("akt");
        if (SonosZones[SonosZones.ActiveZoneUUID].PlayState === "PLAYING") {
            $("#Currentplaylist_" + (apsnumber - 1) + " > .curpopdown > .playlistplaysmall").addClass("akt");
        }
        //Ermitteln der Position des aktuellen Songs und dahin scrollen, wenn nicht manuell gescrollt wurde
        if (SoVa.currentplaylistScrolled === false) {
            SoDo.currentplaylistwrapper.scrollTop(0);
            var ctop = contactTopPosition.position().top;
            SoDo.currentplaylistwrapper.scrollTop(ctop - 30);
            window.setTimeout("SoVa.currentplaylistScrolled = false;", 100);//Beim Scrollen wird das auf true gesetzt, daher wieder rückgänig machen.
        }
    } else {
        if (SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Stream === false) {
            window.setTimeout("SetCurrentPlaylistSong()", 1000);
        }
    }
    return true;
}
//Ersetzen der Playlist und entsprechend der Auswahl daten neu laden.
function ReplacePlaylist(item) {
    SonosLog("ReplacePlaylist");
    SonosZones.Refreshstop = true;
    SoDo.playListLoader.slideDown();
    SoDo.browseLoader.slideDown();
    $(".currentplaylist").remove();
    var uri = $(item).parent().attr("data-containerid");
    //Damit beim gleichem Lied kein Problem entsteht Artist leeren.
    if (SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack !== null && SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Artist !== null) {
        SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Artist = "leer";
    }
    SonosAjax("ReplacePlaylist", { '': uri }).success(function () {
        SonosZones[SonosZones.ActiveZoneUUID].ClearPlaylist();
        SonosZones.Refreshstop = false;
        SoDo.browseLoader.slideUp();
        SonosLog("ReplacePlaylist Done");
    });
}
function RemoveFavItem(item) {
    SonosLog("RemoveFavItem");
    var con = confirm("Dieser Favoriten Eintrag wird gelöscht!");
    if (!con)return;
    SonosAjax("RemoveFavItem", { '': item }).success(function () {
        LoadBrowse("FV:2");
        SonosLog("RemoveFavItem Done");
    });
}
function AddFavItem(item, calltype) {
    
    SonosLog("AddFavItem");
    if (typeof calltype === "undefined" || calltype === "") {
        calltype = "browse";
    }
    var itemdata;
    switch(calltype) {
        case "browse":
            itemdata = $(item).parent().attr("data-containerid");
            break;
        case "playlist":
            itemdata = $(item).parent().children(".playlistcover").attr("data-uri");
            break;

    }
    if (typeof itemdata !== "undefined") {
        SonosAjax("AddFavItem", { '': itemdata }).success(function() {
            SonosLog("AddFavItem Done");
        });
    } else {
        SonosLog("AddFavItem FEHLER itemdata undefined");
    }
}
//Songinfos anzeigen in der Playlist
function ShowSongInfos(t) {
    SonosLog("ShowSongInfos");
    try {
        var newcurpopdown = $(t).parent().attr("id");
        //alt ausblenden
        if (newcurpopdown === SoVa.aktcurpopdown) {
            $("#" + SoVa.aktcurpopdown).children(".currentrackinplaylist").removeClass("aktiv");
            $("#" + SoVa.aktcurpopdown).children(".curpopdown").hide();
            SoVa.aktcurpopdown = "leer";
            return;
        }
        if (SoVa.aktcurpopdown !== "leer") {
            $("#" + SoVa.aktcurpopdown).children(".currentrackinplaylist").removeClass("aktiv");
            $("#" + SoVa.aktcurpopdown).children(".curpopdown").hide();
        }
        var podo = $("#" + newcurpopdown).children(".curpopdown");
        var plcover = podo.children(".playlistcover");

        podo.show();
        $(t).addClass("aktiv");
        SoVa.aktcurpopdown = newcurpopdown;

        //Cover Laden aus dem Data Attribut und dieses entsprechend leeren.
        if (plcover.attr("data-url") !== "geladen") {
            $('<img class="currentplaylistcover" onclick="ShowPlaylistLyric(this)" src="' + plcover.attr("data-url") + '">').appendTo(plcover);
            plcover.attr("data-url", "geladen");
        }
        SoDo.playListLoader.slideDown();
        //Metadaten des Songs laden.

        var request = SonosAjax("GetSongMeta",{ '': plcover.attr("data-uri") });
        request.success(function(data) {
            //Metadaten erhalten
            plcover.attr("data-gelegenheit", data.Gelegenheit);
            plcover.attr("data-stimmung", data.Stimmung);
            plcover.attr("data-bewertungmine", data.BewertungMine);
            plcover.attr("data-aufwecken", data.Aufwecken);
            plcover.attr("data-geschwindigkeit", data.Geschwindigkeit);
            plcover.attr("data-artistplaylist", data.ArtistPlaylist);
            plcover.attr("data-rating", data.Bewertung);
            plcover.attr("data-lyric", data.Lyric);
            if (parseInt(data.Bewertung) === -1) {
                if (podo.children(".bomb").is(":hidden")) {
                    podo.children(".bomb").show();
                }
                if (podo.children(".rating_bar").is(":visible")) {
                    podo.children(".rating_bar").hide();
                }
            } else {
                if (podo.children(".bomb").is(":visible")) {
                    podo.children(".bomb").hide();

                }
                if (podo.children(".rating_bar").is(":hidden")) {
                    podo.children(".rating_bar").show();
                }
                podo.children(".rating_bar").children().css("width", data.Bewertung);
            }
            SoDo.playListLoader.slideUp();
        });
        request.fail(function() {
            ReloadSite("JsControl:ShowSongInfos");
        });
    }
    catch (Ex) {
        alert("Es ist ein Fehler beim ShowSongInfos aufgetreten:<br>" + Ex.Message);
    }
}
//Popup laden mit den Lyrics aus Songs in der Wiedergabeliste
function ShowPlaylistLyric(t) {
    SonosLog("ShowPlaylistLyric");
    var dataparent = $(t).parent();
    var curentid = GetIDfromCurrentPlaylist(dataparent.parent().parent().attr("id"));
    var uri = dataparent.attr("data-uri");
    if ((curentid + 1) === SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber) {
        SonosLog("ShowPlaylistLyric current");
        //Wenn der gewählte Song dem current entspricht soll die currentbox aufgehen.
        $('#Lyric').click();
    } else {
        SonosLog("ShowPlaylistLyric Playlist");
        //Es ist nicht current also die Lyric laden und anzeigen, wenn noch nicht vorhanden
        SoDo.lyricsPlaylist.empty();
        $('<DIV class="righttopclose" onclick="ClosePlaylistLyric()">X</DIV>').appendTo(SoDo.lyricsPlaylist);
        var dataloaded = dataparent.attr("data-geladen");
        if (dataloaded === "geladen") {
            var datalyric = dataparent.attr("data-lyric");
            $('<DIV class="lyricplaylistclass">' + datalyric + '</DIV>').appendTo(SoDo.lyricsPlaylist);
        } else {
            console.log("hier kann man eigentlich nie hinkommen");
            var request = SonosAjax("GetSongMeta", { '': uri });
            request.success(function (data) {
                SonosLog("ShowPlaylistLyric Playlist Data loaded");
                $('<DIV class="lyricplaylistclass">' + data.Lyric + '</DIV>').appendTo(SoDo.lyricsPlaylist);
            });
            request.fail(function (jqXHR) {
                if (jqXHR.statusText === "Internal Server Error") {
                    ReloadSite("ShowPlaylistLyric");
                } else { alert("Beim laden der Aktion:GetRating(" + uri + ") ist ein Fehler aufgetreten."); }
            });
        }
        SonosWindows(SoDo.lyricsPlaylist);
        MoveAktArtist();
    }
}
function ShowPlaylistLyricCurrent() {
    if (SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Uri !== "leer") {
        SoDo.lyricButton.toggleClass("akt");
        SonosWindows(SoDo.lyric);
        MoveAktArtist();
    }
}
function MoveAktArtist() {

    if (SoVa.smallDevice === true) {
        return;
    }
    var hivi = (SoDo.lyric.is(":hidden") && SoDo.browse.is(":hidden") && SoDo.lyricsPlaylist.is(":hidden"));
    if (SoVa.ratingonlycurrent === false && hivi === false) {
        SoDo.aktSongInfo.addClass("moveright");
        SoDo.cover.addClass("moveright");
        SoDo.playlistCount.addClass("movedown");
    }
    if (SoVa.ratingonlycurrent === false && hivi === true) {
        SoDo.aktSongInfo.removeClass("moveright");
        SoDo.cover.removeClass("moveright");
        SoDo.playlistCount.removeClass("movedown");
    }
    if (SoVa.ratingonlycurrent === true && hivi === false && SoDo.ratingListBox.is(":hidden")) {
        SoDo.aktSongInfo.addClass("moveright");
        SoDo.cover.addClass("moveright");
        SoDo.playlistCount.addClass("movedown");
    }
    if (SoVa.ratingonlycurrent === true && hivi === true && SoDo.ratingListBox.is(":hidden")) {
        SoDo.aktSongInfo.removeClass("moveright");
        SoDo.cover.removeClass("moveright");
        SoDo.playlistCount.removeClass("movedown");
    }
    if (SoVa.ratingonlycurrent === true && hivi === true && SoDo.ratingListBox.is(":visible")) {
        SoDo.playlistCount.removeClass("movedown");
    }
    if (SoVa.ratingonlycurrent === true && hivi === false && SoDo.ratingListBox.is(":visible")) {
        SoDo.playlistCount.addClass("movedown");
    }
}
//Entfernt ein Song aus der Playlist
function RemoveFromPlaylist(k) {
    SonosLog("RemoveFromPlaylist");
    SoVa.aktcurpopdown = "leer"; //Reset der Playlist Informationen
    var playid = $(k).parent().parent().attr("id");
    var PressKey = GetIDfromCurrentPlaylist(playid);
    SonosAjax("RemoveSongInPlaylist", "", (PressKey + 1)).success(function (data) {
        if (data === true) {
            if (PressKey < SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber) {
                SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber =(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber - 1);
            }
            //Playlist wird automatisch neu geladen. 
            SonosZones[SonosZones.ActiveZoneUUID].Playlist.RemoveFromPlaylist(PressKey);
            SetCurrentPlaylistSong();
        }
        SonosLog("RemoveFromPlaylist Done");
    });
}
//Schließen der Lyric von Songs aus der Wiedergabeliste
function ClosePlaylistLyric() {
    SonosLog("ClosePlaylistLyric");
    SonosWindows(SoDo.lyricsPlaylist);
    MoveAktArtist();
}
//Helper um die aktuelle ID aus dem übergeben Item der Playlist zu erhalten.
function GetIDfromCurrentPlaylist(k) {
    SonosLog("GetIDfromCurrentPlaylist:" + k);
    var toRemove = 'Currentplaylist_';
    var t = k.replace(toRemove, '');
    return parseInt(t);
}

//Playlist Sortierbar machen
function MakeCurrentPlaylistSortable() {
    SonosLog("MakeCurrentPlaylistSortable");
    SoDo.currentplaylistwrapper.sortable({ disabled: false, axis: "y", placeholder: "currentplaylistplaceholder", stop: function (event, ui) { ResortPlaylist(ui); } });
}
//Playlist wurde umsortiert und nun neu geschrieben.
function ResortPlaylist(ui) {
    SonosLog("ResortPlaylist");
    var cpl = ui.item.attr("id");
    $("#" + cpl).children(".curpopdown").hide();
    SoVa.aktcurpopdown = "leer";//Damit das nächste Aufgehen wieder ohne Probleme geht.
    $("#" + cpl).children(".currentrackinplaylist").removeClass("aktiv");
    SonosLog("ResortPlaylist ID:" + cpl);
    //var cplnumber = GetIDfromCurrentPlaylist(cpl);
    $(".currentplaylist").each(function (i, item) {
        var jitem = $(item).attr("id");

        if (jitem === cpl) {
            //Wenn man hier angekommen ist, dann ist man beim umsortierten objekt und wir zählen nun die Playlist neu durch und setzen den Song entsprechend neu.
            //var positionen = GetIDfromCurrentPlaylist(cpl) + "#" + (i + 1);
            var old = GetIDfromCurrentPlaylist(cpl);
            SonosZones[SonosZones.ActiveZoneUUID].Playlist.ReorderPlaylist(old, i);
            SonosAjax("ReorderTracksinQueue","", (old + 1),(i + 1)).fail(function (jqXHR) {
                if (jqXHR.statusText === "Internal Server Error") {
                    ReloadSite("ResortPlaylist");
                } else { alert("Beim der Aktion:ResortPlaylist(" + ui + ") ist ein Fehler aufgetreten."); }
            });
            SonosLog("ResortPlaylist IDOld:" + cpl + " IdNew:" + i + " send to server Done");
        }
        $(item).attr("id", "Currentplaylist_" + (i));
    });
    ResortPlaylistDisable();
}
//Sortierbarkeit deaktivieren.
function ResortPlaylistDisable() {
    SonosLog("ResortPlaylistDisable");
    SoDo.currentplaylistwrapper.sortable({ disabled: true });
}

function SetAudioIn() {
    //Übergabe an Methode, Server steuert das selber.
    SonosAjax("SetAudioIn");
}

//} Aktuelle Wiedergabeliste

//{ PrüfMethoden, Hintergrundaktualisierungen
//Seite neu laden
function ReloadSite(source) {
    $("#Aktartist").text("ReloadSite by: " + source);
    //var k = confirm("Die Seite wird durch '" + source + "' neu geladen");
    //if (k === false) return;
    SoVa.eventErrorsSource = "";
    SonosZones.Refreshstop = true;
    LoadDevices();
    return;
}
//Aufgrund der Fenstergröße Elemente verschieben und von der GRöße her anpassen
function SetHeight() {
    SonosLog("SetHeight");
    var wh = SoDo.bodydiv.height();
    //alert(wh);
    //Atomic und Mercury IPAD
    if (wh >= 600 && wh <= 687) {
        SoDo.ratingFilterRatingBarComination.animate({ 'margin-top': "8px" }, 100);
        SoDo.ankerlist.children("DIV").animate({ 'margin-bottom': "0" }, 100);
    }
    if (wh >= 688 && wh <= 740) {
        SoDo.ratingFilterRatingBarComination.animate({ 'margin-top': "11px" }, 100);
        SoDo.ankerlist.children("DIV").animate({ 'margin-bottom': "0.1em" }, 100);
    }
    if (wh >= 741 && wh <= 800) {
        SoDo.ratingFilterRatingBarComination.animate({ 'margin-top': "13px" }, 100);
        SoDo.ankerlist.children("DIV").animate({ 'margin-bottom': "0.2em" }, 100);
    }
    if (wh > 801 && wh <= 850) {
        SoDo.ratingFilterRatingBarComination.animate({ 'margin-top': "15px" }, 100);
        SoDo.ankerlist.children("DIV").animate({ 'margin-bottom': "0.3em" }, 100);
    }
    if (wh > 851 && wh <= 920) {
        SoDo.ratingFilterRatingBarComination.animate({ 'margin-top': "15px" }, 100);
        SoDo.ankerlist.children("DIV").animate({ 'margin-bottom': "0.4em" }, 100);
    }
    if (wh > 921) {
        SoDo.ratingFilterRatingBarComination.animate({ 'margin-top': "15px" }, 100);
        SoDo.ankerlist.children("DIV").animate({ 'margin-bottom': "0.5em" }, 100);
    }
}
//Prüft auf Fehler bei Covern und setzt das NoCoverBild
function UpdateImageOnErrors() {
    SonosLog("UpdateImageOnErrors");
    $("img").error(function () {
        $(this).attr('src', SoVa.nocoverpfad);
    });
}
//Wird bei einem Fehler aufgerufen um die Darstellung zurückzusetzen
function ResetAll() {
    SonosLog("ResetAll");
    SoDo.cover.attr("src", SoVa.nocoverpfad);
    SoDo.nextcover.attr("src", SoVa.nocoverpfad).hide();
    $(".akt").text("");
    SoDo.runtimeDuration.html("");
    SoDo.runtimeRelTime.html("");
    $(".next").text("");
    SoDo.aktArtist.html("");
    SoDo.aktTitle.html("");
    SoDo.playlistAkt.html("0");
    SoDo.playlistTotal.html("0");
    SoDo.bewertungWidth.width("0%");
    SoDo.devicesWrapper.children(".groupdevicewrapper").remove();
    SoDo.deviceLoader.show();
    SonosLog("ResetAll Done");
} //Ende Reset

//} PrüfMethoden, Hintergrundaktualisierungen

//{ Bewertung
//Im Layoutbereich wird nur das Layout gesetzt
//In Situationen wird ein Wert in das Datafield geschrieben und das Layout getriggert
//{ Layoutänderungen

function SetRatingSituation(situation) {
    SonosLog("SetRatingSituation:" + situation);
    SoDo.gelegenheitenChildren.removeClass("selected");
    $("#gelegenheit_" + situation).addClass("selected");
}

function SetRatingTempo(tempo) {
    SonosLog("SetRatingTempo:" + tempo);
    SoDo.geschwindigkeitChildren.removeClass("selected");
    $("#geschwindigkeit_" + tempo).addClass("selected");
}
function SetRatingStimmung(stimmung) {
    SonosLog("SetRatingStimmung:" + stimmung);
    SoDo.stimmungenChildren.removeClass("selected");
    $("#stimmung_" + stimmung).addClass("selected");
}
function SetRatingaktRating(rating) {
    SonosLog("SetRatingaktRating:" + rating);
    $(SoDo.ratingListRatingBar).removeClass("rating_bar_aktiv");
    SoDo.ratingBomb.removeClass("rating_bar_aktiv");
    if (parseInt(rating) === -1) {
        //Bombe
        SoDo.ratingBomb.addClass("rating_bar_aktiv");
    } else {
        $("#rating_id_" + rating).addClass("rating_bar_aktiv");
    }
}
function SetRatingMine(rmine) {
    SonosLog("SetRatingMine:" + rmine);
    SoDo.ratingMineSelector.val(rmine);
}
//} Layoutänderungen

//{ Auswahl aus der Ratingliste
function SetGeschwindigkeit(tempo) {
    SoDo.ratingListBox.attr("data-geschwindigkeit", tempo);
    SetRatingTempo(tempo);
}
//Stimmung setzen
function SetStimmung(stimmung) {
    SoDo.ratingListBox.attr("data-stimmung", stimmung);
    SetRatingStimmung(stimmung);
}
//Aufwecken setzten
function SetRatingAufwecken(aufwecken) {
    SoDo.aufweckenSwitch.prop("checked", aufwecken);
}
//Interpretenplaylist setzten
function SetRatingArtistpl(arpl) {
    SoDo.artistplSwitch.prop("checked", arpl);
}
//Wird aus der Ratinglistaufgerufen um entsprechend das Layout zu definieren.
function ChangeRating(v) {
    SoDo.ratingListBox.attr("data-rating", v);
    SetRatingaktRating(v);
}
//Gelegenheit in das Data beim Rating schreiben
function SetSituation(situation) {
    SoDo.ratingListBox.attr("data-gelegenheit", situation);
    SetRatingSituation(situation);
}
function SetRatinMineSelection(r) {
    SoDo.ratingListBox.attr("data-bewertungMine", r);
    SetRatingMine(r);
}
//} Situationen

//{ Vorbereitung Verarbeitung
//Ratinglist vorbereiten von Songs aus der Wiedergabeliste
function ShowPlaylistRating(t) {
    SonosLog("ShowPlaylistRating");
    //Bei Streaming immer schließen und Return;
    if (SoVa.ratingonlycurrent === true) {
        SonosLog("ShowPlaylistRating Show only Currentrating");
        return;
    }
    SonosWindows(SoDo.ratingListBox, false, { overlay: true, selecteddivs: [SoDo.nextButton, SoDo.playButton, SoDo.MuteButton] });
    if (SoDo.ratingListBox.is(":hidden")) {
        return;
    }
    var dataparent = $(t).parent().children(".playlistcover");
    SoDo.ratingListBox.attr("data-uri", $(dataparent).attr("data-uri"));
    SoDo.ratingListBox.attr("data-type", "playlist");
    SoDo.ratingListBox.attr("data-playlistid", $(t).parent().parent().attr("id").substring(16));
    var dgelegenheit = $(dataparent).attr("data-gelegenheit");
    var dgeschw = $(dataparent).attr("data-geschwindigkeit");
    var dstimm = $(dataparent).attr("data-stimmung");
    var drating = $(dataparent).attr("data-rating");
    var dratingmine = $(dataparent).attr("data-bewertungmine");
    var daufweck = ($(dataparent).attr("data-aufwecken") === 'true');
    var dartistpl = ($(dataparent).attr("data-artistplaylist") === 'true');
    SoDo.ratingListBox.attr("data-gelegenheit", dgelegenheit);
    SoDo.ratingListBox.attr("data-geschwindigkeit", dgeschw);
    SoDo.ratingListBox.attr("data-stimmung", dstimm);
    SoDo.ratingListBox.attr("data-aufwecken", daufweck);
    SoDo.ratingListBox.attr("data-artistplaylist", dartistpl);
    SoDo.ratingListBox.attr("data-rating", drating);
    SetRatingSituation(dgelegenheit);
    SetRatingTempo(dgeschw);
    SetRatingStimmung(dstimm);
    SetRatingAufwecken(daufweck);
    SetRatingArtistpl(dartistpl);
    SetRatingaktRating(drating);
    SetRatinMineSelection(dratingmine);
    SonosLog("ShowPlaylistRating Done");
}
//Ratinglist vorbereiten vom current Song
function ShowCurrentRating(t) {
    SonosLog("ShowCurrentRating");
    //Bei Streaming immer schließen und Return;
    if (SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Stream === true && SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.StreamContent !=="Apple") {
        if (SoDo.ratingListBox.is(":visible")) {
            SonosWindows(SoDo.ratingListBox, true);
        }
        return;
    }


    //Wenn nur current angezeigt werden soll, dann nicht schließen. 
    if (SoVa.ratingonlycurrent === true && t !== "hide") {
        if (!SoDo.ratingListBox.is(":visible")) {
            SonosWindows(SoDo.ratingListBox);
        }
        //Verschieben des Titels.

        SoDo.aktSongInfo.addClass("moveleft");
        SoDo.playlistCount.addClass("moveleft");
    }
    if (SoVa.ratingonlycurrent === true && t === "hide") {
        SonosWindows(SoDo.ratingListBox);
        MoveAktArtist();
        SoDo.aktSongInfo.removeClass("moveleft");
        SoDo.playlistCount.removeClass("moveleft");
        return;
    }
    if (SoVa.ratingonlycurrent === false) {
        if (!SoDo.ratingListBox.is(":visible")) {
            SonosWindows(SoDo.ratingListBox, false, { overlay: true, selecteddivs: [SoDo.nextButton, SoDo.playButton, SoDo.MuteButton] });
        } else {
            SonosWindows(SoDo.ratingListBox);
        }
        //SonosWindows(ratinglist);
        if (t === "hide") {
            SonosWindows(SoDo.ratingListBox, true);
            return;
        }
    }
    SoDo.ratingListBox.attr("data-type", "current");
    SoDo.ratingListBox.attr("data-playlistid", (SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber - 1));
    SoDo.ratingListBox.attr("data-uri", SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Uri);
    SoDo.ratingListBox.attr("data-gelegenheit", SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Gelegenheit);
    SoDo.ratingListBox.attr("data-geschwindigkeit", SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Geschwindigkeit);
    SoDo.ratingListBox.attr("data-stimmung", SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Stimmung);
    SoDo.ratingListBox.attr("data-aufwecken", SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Aufwecken);
    SoDo.ratingListBox.attr("data-artistplaylist", SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.ArtistPlaylist);
    SoDo.ratingListBox.attr("data-rating", SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Bewertung);
    SoDo.ratingListBox.attr("data-bewertungmine", SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.BewertungMine);
    SetRatingSituation(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Gelegenheit);
    SetRatingTempo(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Geschwindigkeit);
    SetRatingStimmung(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Stimmung);
    SetRatingAufwecken(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Aufwecken);
    SetRatingArtistpl(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.ArtistPlaylist);
    SetRatingaktRating(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Bewertung);
    SetRatingMine(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.BewertungMine);
    SonosLog("ShowCurrentRating Done");
}

//} Vorbereitung Verarbeitung

//Filter für das Rating setzen und Filterlist schließen.
function SetRatingFilter(type,v) {
    SonosLog("SetRatingFilter");
    if (type === "hide") {
        SonosWindows(SoDo.filterListBox, true);
        return;
    }
    SonosZones[SonosZones.ActiveZoneUUID].ChangeRatingFilter(type, v);
   SonosLog("SetRatingFilter Done");
}
//Das Rating und Gelegenheiten für einen Song setzen
function SetRatingLyric() {
    SonosLog("SetRatingLyric");
    var uri = SoDo.ratingListBox.attr("data-uri");
    var gelegenheit = SoDo.ratingListBox.attr("data-gelegenheit");
    var geschwindigkeit = SoDo.ratingListBox.attr("data-geschwindigkeit");
    var stimmung = SoDo.ratingListBox.attr("data-stimmung");
    var rating = parseInt(SoDo.ratingListBox.attr("data-rating"));
    var rmine = SoDo.ratingListBox.attr("data-bewertungmine");
    var aufwecken = SoDo.aufweckenSwitch.prop("checked");
    var artistpl = SoDo.artistplSwitch.prop("checked");
    var pid = parseInt(SoDo.ratingListBox.attr("data-playlistid"));
    SonosAjax("SetSongMeta",{ '': uri + "#" + rating + "#" + gelegenheit + "#" + geschwindigkeit + "#" + stimmung + "#" + aufwecken + "#" + artistpl + "#" + rmine }).done(function () {
        SonosLog("SetRatingLyric rated to Server");
        if ((SoDo.ratingListBox.attr("data-type") === "current" || pid === (SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber - 1)) && SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3 !== null) {
            SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Gelegenheit = gelegenheit;
            SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Geschwindigkeit = geschwindigkeit;
            SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Stimmung = stimmung;
            SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Aufwecken = aufwecken;
            SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.ArtistPlaylist = artistpl;
            SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.Bewertung = rating;
            SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3.BewertungMine = rmine;
            SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.RenderCurrentTrack(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber);
        }
        //Setzen des Rating in der Playlist
        if (pid >= 0) {
            var playlistsong = $("#Currentplaylist_" + pid).children(".curpopdown");
            var plcover = playlistsong.children(".playlistcover");
            plcover.attr("data-gelegenheit", gelegenheit);
            plcover.attr("data-geschwindigkeit", geschwindigkeit);
            plcover.attr("data-stimmung", stimmung);
            plcover.attr("data-aufwecken", aufwecken);
            plcover.attr("data-artistplaylist", artistpl);
            plcover.attr("data-rating", rating);
            plcover.attr("data-bewertungMine", rmine);
            if (plcover.attr("data-url") !== "geladen") {
                $('<img class="currentplaylistcover" onclick="ShowPlaylistLyric(this)" src="' + plcover.attr("data-url") + '">').appendTo(plcover);
                plcover.attr("data-url", "geladen");
            }
            //Setzen einer Bombe
            if (rating !== -1) {
                if ($("#Currentplaylist_" + pid).children(".curpopdown").children(".bomb").is(":visible")) {
                    $("#Currentplaylist_" + pid).children(".curpopdown").children(".bomb").hide();
                }
                playlistsong.children(".moveCurrentPlaylistTrack").css("margin-left", "24px");
            } else {
                if ($("#Currentplaylist_" + pid).children(".curpopdown").children(".bomb").is(":hidden")) {
                    $("#Currentplaylist_" + pid).children(".curpopdown").children(".bomb").show();
                }
                playlistsong.children(".moveCurrentPlaylistTrack").css("margin-left", "0");
            }
            playlistsong.children(".rating_bar").children("DIV").width(rating + "%");
        }
        //Prüfen ob verarbeitungsfehler vorhanden sind
        window.setTimeout('GetRatingErrors()', 250);
        if (SoVa.ratingonlycurrent === false) {
            //Wenn nur Current Rating genommen wird, dann nicht ausblenden.
            SonosWindows(SoDo.ratingListBox, true);
        } else {
            ShowCurrentRating("blub");
        }
        SoDo.ratingCheck.show().hide(2000);
        SonosLog("SetRatingLyric rated to Server Done");
    })
    .fail(function (jqXHR) {
        if (jqXHR.statusText === "Internal Server Error") {
            ReloadSite("SetRating Lyric");
        } else { SonosWindows(SoDo.ratingListBox, true); alert("Es ist ein Fehler bei SetRatingLyric aufgetreten"); }
    });
}
//Laden der Anzahl Rating Erros um zu verhindern, das man die Seite mit Fehlern verläßt
function GetRatingErrors() {
    SonosLog("GetRatingErrors");
    var request = SonosAjax("GetErrorListCount");
    request.success(function (data) {
        //Es gibt keine Fehler mehr, daher alles ausblenden
        if (data === null || data === 0) {
            if (SoVa.ratingerrorsCount !== 0) {
                if (SoDo.ratingErrors.is(":visible")) {
                    SoDo.ratingErrors.hide();
                }
                SonosWindows(SoDo.ratingErrorList, true);
                SoVa.ratingerrorsList = 0;
                SoVa.ratingerrorsCount = 0;
            }
        } else {
            //Bei einer Änderung diese Anzeigen.
            if (SoVa.ratingerrorsCount !== data) {
                if (SoDo.ratingErrors.is(":hidden")) {
                    SoDo.ratingErrors.show();
                }
                SoDo.ratingErrors.text(data);
                SoVa.ratingerrorsCount = data;
            }
            //Prüfen ob verarbeitungsfehler vorhanden sind
            window.setTimeout('GetRatingErrors()', 1500);
        }
        SonosLog("GetRatingErrors Done");
    });
    request.fail(function (jqXHR) {
        if (jqXHR.statusText === "Internal Server Error") {
            ReloadSite("GetRatingErrors");
        } else { alert("Beim laden von GetRatingErrors ist ein Fehler aufgetreten."); }
    });
}
//Laden der Rating Erros Objekte
function ShowRatingErrorNames() {
    SonosLog("ShowRatingErrorNames");
    if (SoVa.ratingerrorsList === 0) {
        $(".ratingerrorslistclass").remove();
        var request = SonosAjax("GetErrorList");
        request.success(function (data) {
            $.each(data, function (i, item) {
                $('<div class="ratingerrorslistclass">' + item.Titel + '</div>').appendTo(SoDo.ratingErrorList);
            });
            SonosLog("ShowRatingErrorNames Done List Errors");
        });
        request.fail(function (jqXHR) {
            if (jqXHR.statusText === "Internal Server Error") {
                ReloadSite("ShowRatingErrorNames");
            } else { alert("Beim laden von GetRatingErrors ist ein Fehler aufgetreten."); }
        });
        SoVa.ratingerrorsList = 1;
        SonosWindows(SoDo.ratingErrorList);
    } else {
        SonosLog("ShowRatingErrorNames Done No  to shown Errors");
        SoVa.ratingerrorsList = 0;
        SonosWindows(SoDo.ratingErrorList);

    }
}


//} Bewertung

//{ Suchen nach Songs
//Durchsuchen der Bibliothek starten
function BrowsePress() {
    SonosLog("BrowsePress");
    SonosWindows(SoDo.browse);
    if (document.body.clientWidth > 420) {
        MoveAktArtist(250);
    }
    SoDo.browseButton.toggleClass("akt");
    if (SoVa.browsefirst === 0) {
        window.setTimeout("LoadBrowse('A:ALBUMARTIST')", 300);
        SoVa.browsefirst = 1;
    }
}
//Bibliothekt durchsuchen und darstellen
function LoadBrowse(v) {
    SonosLog("LoadBrowse:" + v);
    SoDo.browseLoader.show();
    var loadbrowse;
    if (v === "bb") {
        loadbrowse = SoDo.browseBackButton.attr("data-parent");
        SonosLog("LoadBrowse:" + v);
    } else {
        if (v === "A:ALBUMARTIST" || v === "A:PLAYLISTS" || v === "A:GENRE" || v === "FV:2") {
            loadbrowse = v;
        } else {
            loadbrowse = $(v).parent().attr("data-containerid");
        }
    }
    if (typeof loadbrowse === 'undefined') {
        loadbrowse = v;
    }
    $(".currentbrowse").remove();
    if (SoDo.browseBackButton.is(":visible")) {
        SoDo.browseBackButton.hide();
    }
    SoDo.ankerlist.empty();
    var request = SonosAjax("Browsing", { '': loadbrowse });
    request.success(function (data) {
        SonosLog("LoadBrowse Data loaded");
        var abc = [];
        if (data.length > 0) {
            $.each(data, function (i, item) {
                //Erster Durchlauf und nicht im Root
                if (i === 0 && item.ParentID !== "A:ALBUMARTIST" && item.ParentID !== "A:PLAYLISTS" && item.ParentID !== "A:GENRE" && item.ParentID !== "FV:2") {
                    //Es gibt auch noch ein Parenteintrag, diesen anpassen und entsprechend darstellen ansonsten den alten nehmen
                    if (item.ParentID !== null) {
                        if (item.ParentID.lastIndexOf("/") > 0) {
                            SoVa.browseParentID = item.ParentID.substring(0, item.ParentID.lastIndexOf("/"));
                        } else {
                            SoVa.browseParentID = "A:ALBUMARTIST";
                        }
                    } else {
                        if (loadbrowse.substring(0, 1) !== "S") {
                            SoVa.browseParentID = loadbrowse.substring(0, loadbrowse.lastIndexOf("/"));
                        } else {
                            SoVa.browseParentID = "A:PLAYLISTS";
                        }
                    }
                    SoDo.browseBackButton.show().attr("data-parent", SoVa.browseParentID);
                }
                var im = "";
                //Entweder ein Container oder ein Song
                var browsetitlewidth;
                if (item.ContainerID !== null) {
                    //Prüfen ob schon im Array.
                    var alink = "";
                    if (loadbrowse === "A:ALBUMARTIST" || loadbrowse === "A:PLAYLISTS" || loadbrowse === "A:GENRE") {
                        var buchstabe = item.Title.substring(0, 1).toUpperCase();
                        if ($.inArray(buchstabe, abc) === -1) {
                            abc.push(buchstabe);
                            alink = '<a href="#" name="' + buchstabe + '"></a>';
                        }
                    }
                    browsetitlewidth = 320;
                    if (item.AlbumArtURI !== null && item.MP3.HatCover === true) {
                        im = '<div class="browsingCover"><img src="' + SoVa.nocoverpfad + '" class="lazy" data-original="http://' + SonosZones[SonosZones.ActiveZoneUUID].BaseURL + item.AlbumArtURI + '"></div>';
                        browsetitlewidth = 280;
                    }
                    //Wenn All, kann nicht zu den Favoriten zugefügt werden.
                    var addfav = '<DIV class="addFavItem" onclick="AddFavItem(this,\'browse\')"></DIV>';
                    if (item.Title === "All") {
                        addfav = '<DIV class="addFavItemhidden"></DIV>';
                    }

                    $('<div id="Browsing' + (i + 1) + '" data-containerid="' + item.ContainerID + '" class="currentbrowse">' + im + '<DIV class="browsetitle" style="width: ' + browsetitlewidth + 'px;" onclick="LoadBrowse(this)">' + item.Title + alink + '</div><DIV class="browseaddcontainertoplaylist" onclick="AddToPlaylist(this)"></DIV><DIV class="browsereplacecontainertoplaylist" onclick="ReplacePlaylist(this)"></DIV>'+addfav+'</DIV>').appendTo(SoDo.browseWrapper);
                } else {
                        //Es handelt sich um einen song
                        //Rating wird mitgeliefert und angezeigt, wenn es . 
                        var rating = '<div class="bomb" Style="display:block;"><img src="/Images/bombe.png" alt="playlistbomb"/></div>';
                            if (parseInt(item.MP3.Bewertung) !== -1) {
                                rating = '<div style="margin-left: 10px;margin-top: 10px;" class="rating_bar"><div style="width:' + item.MP3.Bewertung + '%;"></div></div>';
                            }
                        //Bei Favoriten das Rating überschreiben durch ein Remove von Favoriten
                        if (item.ParentID === "FV:2") {
                            rating = '<DIV class="browseRemoveFav" onclick="RemoveFavItem(\'' + item.ItemID + '\')"></DIV>';
                        }

                        im = "";
                        if (item.AlbumArtURI !== null && item.MP3.HatCover === true) {
                            var itmAAURi = "http://" + SonosZones[SonosZones.ActiveZoneUUID].BaseURL + item.AlbumArtURI;
                            //Prüfen woher das cover stammt.
                            if (!item.AlbumArtURI.startsWith("/getaa?u=")) {
                                itmAAURi = item.AlbumArtURI;
                            }
                            im = '<div class="browsingCover"><img src="' + SoVa.nocoverpfad + '" class="lazy" data-original="' + itmAAURi + '"></div>';
                        } else {
                            im = '<div class="browsingCover"><img src="' + SoVa.nocoverpfad + '"></div>';
                        }
                        var itmuri = item.Uri;
                        //var addtopl = '<DIV class="browseaddcontainertoplaylist" onclick="AddToPlaylist(this)"></DIV>';
                        if (item.ItemID.startsWith("FV:2")) {
                            itmuri = item.ItemID;
                          //  addtopl = '<DIV class="browseRemoveFav" onclick="RemoveFavItem(\''+itmuri+'\')"></DIV>';
                        }
                        $('<div id="Browsing' + (i + 1) + '" data-containerid="' + itmuri + '" class="currentbrowse">' + im + '<DIV class="browsetitle" style="width:210px;">' + item.Title + '</div><DIV class="browseaddcontainertoplaylist" onclick="AddToPlaylist(this)"></DIV><DIV class="browsereplacecontainertoplaylist" onclick="ReplacePlaylist(this)"></DIV>' + rating + '</DIV>').appendTo(SoDo.browseWrapper);
                    
                }


            }); //Ende each
            UpdateImageOnErrors();
            LazyLoad();
            //Anker wurde vorbereitet und kann nun angezeigt werden.
            if (abc.length > 0) {
                $.each(abc, function (i, item) {
                    $('<div onClick="SetAnker(\'' + item + '\',\'' + v + '\')" id="ankerlist_' + item + '"><a href="#' + item + '">' + item + '</a></DIV>').appendTo(SoDo.ankerlist);
                });
            }
        } else {
            //ES wurden keine Elemente zurückgeliefert
            if (loadbrowse.substring(0, 1) !== "S") {
                SoVa.browseParentID = loadbrowse.substring(0, loadbrowse.lastIndexOf("/"));
            } else {
                SoVa.browseParentID = "A:ALBUMARTIST";
            }
            SoDo.browseBackButton.show().attr("data-parent", SoVa.browseParentID);
        }
        //Sprunganker ansteuern.
        if (SoVa.getanker !== "leer") {
            //Prüfen, ob ein anderer Buchstabe gewählt wurde und diesen entsprechend setzen.      	
            var interpretfirstletterindex = loadbrowse.indexOf("/");
            var interpretfirstletter = loadbrowse.substr(interpretfirstletterindex + 1, 1);
            if (interpretfirstletterindex > -1 && interpretfirstletter !== SoVa.getanker) {
                SoVa.getanker = interpretfirstletter;
            }
            if (loadbrowse.indexOf(SoVa.getAnkerArt) > -1) {
                //Safari und Chrome können damit nicht umgehen, wenn es der gleiche Sprunganker ist, daher zweimal setzen.     	
                location.hash = "#leer";
                location.hash = "#" + SoVa.getanker;
            } else {
                console.log("Funzt nicht:"+loadbrowse);
            }
        }
        SetHeight();
        SoDo.browseLoader.hide(1000);
        SonosLog("LoadBrowse Data loaded End");
    });
    request.fail(function (jqXHR) {
        if (jqXHR.statusText === "Internal Server Error") {
            ReloadSite("LoadBrowse");
        } else { SoDo.browseLoader.hide(); alert("Beim laden der Aktion:Browse(" + v + ") ist ein Fehler aufgetreten."); }
    });
    SonosLog("LoadBrowse End");
}

function LazyLoad() {
    $("img").lazyload({ container: SoDo.browseWrapper });

}
//Vorbereitung um beim Backklick auch wieder zurück zu springen in der liste. 
function SetAnker(buchstabe, art) {
    SonosLog("SetAnker Letter:" + buchstabe + " Type:" + art);
    SoVa.getanker = buchstabe;
    SoVa.getAnkerArt = art;
}
//} Suchen nach Songs

//{ Settings und Global Functions
//Aktualisieren des Musikindexes
function UpdateMusicIndex() {
    SonosLog("UpdateMusicIndex:" + SoVa.updateMusikIndex);
    if (SoVa.updateMusikIndex === false) {
        SoDo.musikIndexLoader.show();
        var request = SonosAjax("SetUpdateMusicIndex");
        request.success(function () {
            SonosLog("UpdateMusicIndex send to Server");
            SonosZones.Refreshstop = true;
            window.setTimeout("GetMusicIndexInProgress()", 500);
        });
        request.fail(function (jqXHR) {
            if (jqXHR.statusText === "Internal Server Error") {
                ReloadSite("UpdateMusicIndex");
            } else { alert("Beim aktualiseren des Musikindexes ist ein Fehler aufgetreten."); }
        });
    }
}
//Zeigt Songsdetails an aus dem Currenttrack, die in metaUse definiert wurden.
function ShowCurrentSongMeta() {
    SonosWindows(SoDo.currentMeta);
    if (SoDo.currentMeta.is(":hidden")) {
        return;
    }
    if (SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Stream === true && SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.StreamContent !=="Apple") {
        SonosWindows(SoDo.currentMeta, true);
        return;
    }
    SoDo.currentMeta.empty();
    if (SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3 !== null) {
        var data = SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.MP3;
        var prop = Object.getOwnPropertyNames(data);
        for (var i = 0; i < prop.length; i++) {
            var k = prop[i];
            if (SoVa.metaUse.indexOf(k) !== -1) {
                if (data[k] !== "" && data[k] !== null && data[k] !== "leer" && data[k] !== 0) {
                    $("<div><b>" + k + "</b>: " + data[k] + "</div>").appendTo(SoDo.currentMeta);
                }
            }
        }
    } else {
        SonosAjax("GetSongMeta", { '': SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Uri }).success(function (datanull) {
            var propnull = Object.getOwnPropertyNames(datanull);
            for (var y = 0; y < propnull.length; y++) {
                var kp = propnull[y];
                if (SoVa.metaUse.indexOf(kp) !== -1) {
                    if (datanull[kp] !== null && datanull[kp] !== "") {
                        $("<div><b>" + kp + "</b>: " + datanull[kp] + "</div>").appendTo(SoDo.currentMeta);
                    }
                }
            }
        });
    }
}
//Ermittelt das aktuelle Verhalten des Gerätes und Songs
//Prüft, ob der Musikindex gerade  aktualisiert wird,
function GetMusicIndexInProgress() {
    SonosLog("GetMusicIndexInProgress");
    var request = SonosAjax("GetUpdateIndexInProgress");
    request.success(function (data) {
        SonosLog("GetMusicIndexInProgress:" + data);
        if (data === true) {
            window.setTimeout("GetMusicIndexInProgress()", 1000);
        } else {
            SoDo.musikIndexLoader.hide();
            SoDo.musikIndexCheck.show().hide(2000,function() {
                window.setTimeout("GetPlaylists()", 2000);
                SonosZones.Refreshstop = false;
            });

            
        }
        SonosLog("GetMusicIndexInProgress END");
    });
    request.fail(function (jqXHR) {
        if (jqXHR.statusText === "Internal Server Error" || jqXHR.statusText === "error") {
            ReloadSite("GetMusicIndexInProgress");
        } else { alert("Beim aktualiseren des Musikindexes ist ein Fehler aufgetreten."); }
        SoDo.musikIndexLoader.hide();
    });
}
//Läd alle vorhandenen Playlisten.
function GetPlaylists() {
    SonosLog("GetPlaylists ALL");
    if (SoVa.GlobalPlaylistLoaded === true) {
        return; //Playlisten müssen nicht neu geladen werden.
    }
    SoDo.globalPlaylistLoader.slideDown();
    $(".playlist").remove();
    var request = SonosAjax("GetPlaylists");
    request.success(function (data) {
        if (data === null || typeof data === "undefined" || data.length === 0) {
            SonosLog("GetPlaylists ALL Leer nochmal in einer Sekunde laden");
            window.setTimeout("GetPlaylists()", 1000);
            return;
        }
        //alle Wiedergabelisten leeren. Wird für die Autovervollständigung genutzt
        SoVa.allplaylist = [];
        $.each(data, function (i, item) {
            var playlisttype;
            if (item.Description === "M3U") {
                playlisttype = "m3u";
            } else {
                playlisttype = "sonos";
            }
            //Wiedergabeliste befüllen
            SoVa.allplaylist.push(item.Title);
            $('<div id="Playlist_' + i + '" class="playlist ' + playlisttype + '" data-containerid="' + item.ContainerID + '"><div onclick="ReplacePlaylist(this);">' + item.Title + '</DIV></div>').appendTo(SoDo.playlistwrapper);
        });
        SoVa.allplaylist.sort();
        SoVa.GlobalPlaylistLoaded = true;
        SoDo.globalPlaylistLoader.slideUp();
        SonosLog("GetPlaylists ALL END");
    });
    request.fail(function (jqXHR) {
        if (jqXHR.statusText === "Internal Server Error") {
            ReloadSite("GetPlaylists");
        } else { alert("Beim laden von GetPlaylist ist ein Fehler aufgetreten."); }
    });
}
//Funktion zum Absenden ohne Rückmeldung
function doit(d) {
    SonosLog("doit:" + d);
    var request = SonosAjax(d);
    request.success(function (data) {
        if (data === "Fehler") {
            alert("Beim laden der Aktion:" + d + " wurde ein Fehler gemeldet.");
        } else {
            SonosLog("doit:" + d + " Done");
        }
    });
    request.fail(function (jqXHR) {
        if (jqXHR.statusText === "Internal Server Error") {
            ReloadSite("doit:" + d);
        } else { alert("Beim laden der doit Aktion:" + d + " ist ein Fehler aufgetreten."); }
    });
} //Ende von DO
//Funktion zum Absenden ohne Rückmeldung mit Wertübergabe
function doitValue(d, v) {
    SonosLog("doitValue:" + d + " Value:" + v);
    var request = SonosAjax(d, { '': v });
    request.success(function () {
        SonosLog("doitValue:" + d + " Value:" + v + " Done");
    });
    request.fail(function (jqXHR) {
        if (jqXHR.statusText === "Internal Server Error") {
            ReloadSite("doitvalue:" + d + " value:" + v);
        } else { alert("Beim laden der Aktion:" + d + " ist ein Fehler aufgetreten."); }
    });
} //Ende von DO2
//Beim DEbug In die Console loggen
function SonosLog(v) {
    if (debug === false && showerrors === false) {
        return;
    }
    if (debug === true) {
        console.log(v);
    }
    if (showerrors === true) {
        SoDo.errorloggingwrapper.prepend("<br />" + v);
    }
}
/*Folgender aufrufe als erklärung
SonosWindows(ratinglist,false,{overlay:true,selecteddivs:[$("#Next"),$("#Play")]});

param 1 = Object, was angezeigt wird DomElement oder der Text "overlay" bei Overlay wurde auf das Overlay geklickt und nun soll es geschlossen werden.
param 2 = Optional; soll das element auf jedenfall geschlossen werden?
param 3 = Object mit Parametern
Object Param 1 = overlay = Boolean = Zeitg, ob ein Overlay angezeigt werden soll, bei dem das entsprechende Fenster von param 1 drüber liegt.
Object Param 2 = selecteddivs = Array oder einzel Jquery Object = Objecte, die auch über dem Overlay angezeigt werden sollen. 
*/
function SonosWindows(sobj, remove, setobj) {
    if (sobj === "overlay") {
        sobj = SoVa.overlayDVIObject; //Vorhandene elemente schließen. 
    }
    var objectindex = SoVa.swindowlist.indexOf(sobj);
    var rem = "notset";
    if (typeof remove !== "undefined") {
        rem = remove;
    }
    var overlay = false;
    var settingsobject = setobj || false;
    //Hier dann die settings für das Div hinterlegen. 
    if (settingsobject !== false) {
        overlay = setobj.overlay || false;
        var tempselecteddivs = setobj.selecteddivs || false;
        if (tempselecteddivs !== false) {
            if ($.isArray(tempselecteddivs)) {
                SoVa.selectetdivs = tempselecteddivs;
            } else {
                SoVa.selectetdivs.push(tempselecteddivs);
            }
        }
    }
    var i;
    if (objectindex === -1 && rem !== true) {
        SoVa.swindowlist.push(sobj);
        if (overlay === true) {
            SoDo.overlay.show().css("z-index", SoVa.szindex);
            SoVa.overlayDVIObject = sobj;
            SoVa.szindex++;
            if (SoVa.selectetdivs.length > 0) {
                for (i = 0; i < SoVa.selectetdivs.length; i++) {
                    SoVa.selectetdivs[i].css("z-index", SoVa.szindex);
                }
                SoVa.szindex++;
            }
        }
        sobj.show().css("z-index", SoVa.szindex);
        SoVa.szindex++;
    } else {
        if (rem !== false) {
            if (objectindex !== -1) {
                SoVa.swindowlist.splice(objectindex, 1);
            }
            if (sobj === SoVa.overlayDVIObject) {
                if (SoDo.overlay.is(":visible")) {
                    SoDo.overlay.hide();
                }
                SoVa.overlayDVIObject = "";
                if (SoVa.selectetdivs.length > 0) {
                    for (i = 0; i < SoVa.selectetdivs.length; i++) {
                        SoVa.selectetdivs[i].css("z-index", 100);
                    }
                    SoVa.selectetdivs = [];
                }
            }
            sobj.hide();
        }
    }
    if (SoVa.swindowlist.length === 0) {
        SoVa.szindex = 100;
    }


}
function ShowSleepMode() {
    SonosWindows(SoDo.sleepMode);
}
function SetSleepModeState() {
    SonosZones[SonosZones.ActiveZoneUUID].SleepMode = SoDo.sleepModeSelection.val();
    //clearTimeout(RefreshSleepModeStateID);//Timer löschen, wird durch Eveting wieder aktiviert bei Bedarf.
    var request = SonosAjax("SetSleepTimer",{ '': SonosZones[SonosZones.ActiveZoneUUID].SleepMode });
    request.fail(function (jqXHR) {
        if (jqXHR.statusText === "Internal Server Error") {
            ReloadSite("SetSleepModeState");
        } else { alert("Beim laden der Aktion:GetSleepModeState ist ein Fehler aufgetreten."); }
    });

}
//} Settings und Global Functions

function DevTest(da) {
    
    var request = $.post(SoVa.apiPlayerURL + "DevTestPost/" + SonosZones.ActiveZoneUUID, { '': da });
    request.success(function () {});
}

//{ In Work

function LoadApp(t) {
    var tdata = $(t).data("appname");
    
    switch (tdata) {
        case 'Clock':
            $("#AppIframe").html("<iframe src='/apps/clock.html'></iframe>");
            break;
        case 'Dash':
            $("#AppIframe").html("<iframe src='/apps/dash.html'></iframe>");
            break;
        case 'Aurora':
            $("#AppIframe").html("<iframe src='/apps/aurora.html' scrolling='no'></iframe>");
            break;
    }
    $("#AppIframeWrapper").slideDown(100);
}
//Eventing?
/*
function Eventing() {
    var source = new window.EventSource("/sonos/Event");
    source.onopen = function (event) {
        //   document.getElementById('test').innerHTML += 'Connection Opened.';  
    };
    source.onerror = function (event) {
        if (event.eventPhase == window.EventSource.CLOSED) {
            document.getElementById('test').innerHTML += 'Connection Closed.';
        }
    };
    source.onmessage = function (event) {
        //	   document.getElementById('test').innerHTML += event.data;  
        var PlayerEventData = JSON.parse(event.data);

        var k = "h";

    };
}
/*
//} In Work



//{ Hour

/*
Klasse: Hour()

Eine Klasse um mit Stunden zu rechnen.

Version:

1.0 - 11.7.2010

new Hour(String)

String hat das Format hh:mm:ss wobei jeder Teil optional ist, der String wird gegebenfalls von rechts aufgefüllt.
Beispiel:
var t1 = new Hour('1:45:0');

alert(t1); // gibt: 01:45:00
alert(t1*1); // gibt: 10.75

Funktionen:

add(Hour)   addiert zwei Hour Objekte. Rückgabe: Hour Objekt
sub(Hour)   subtrahiert zwei Hour Objekte. Rückgabe: Hour Objekt
hour()      gibt die Stunden als Dezimalzahl zurück.

set(number) rechnet eine Sekundenzahl in eine Stundenangabe um und setzt das Objekt auf diesen Wert.

toString() gibt den formatieren String aus
valueOf() gibt die Sekunden zurück

*/
function Hour(wert) {
    // private functions
    function Hourfrmt(n) { return n < 10 ? '0' + n : n; }
    function Hourseconds2string(n) {
        var sign = n < 0 ? '-' : '';
        n = Math.abs(n);
        var h = parseInt(n / 3600);
        var m = parseInt((n / 60) % 60);
        var sec = parseInt(n % 60);
        return sign + Hourfrmt(h) + ':' + Hourfrmt(m) + ':' + Hourfrmt(sec);
    }
    function Hourstring2seconds() {
        var tmp = wert.split(':');
        if (!tmp.length) tmp[0] = 0;
        if (tmp.length < 2) tmp[1] = 0;
        if (tmp.length < 3) tmp[2] = 0;
        while (tmp[2] > 59) {
            tmp[2] -= 60;
            ++tmp[1];
        }
        while (tmp[1] > 59) {
            tmp[1] -= 60;
            ++tmp[0];
        }

        return tmp[0] * 3600 + tmp[1] * 60 + tmp[2] * 1;
    }
    this.set = function (s) {
        wert = Hourseconds2string(s);
        return this;
    };
    //Format hh:mm:ss
    this.setTimeString = function (s) {
        wert = s;
        return this;
    };
    // ist der Kontext ein String, dann ist das Objekt ein formatierter String
    this.toString = function () { return Hourseconds2string(Hourstring2seconds()); };

    // ist der Kontext kein String, dann ist das Objekt eine Zahl
    this.valueOf = function () { return Hourstring2seconds(); };

}
//} Hour