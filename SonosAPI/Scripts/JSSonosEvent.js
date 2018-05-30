/*
Alle Functions, die regelmäßig über SetTimeout aufgerufen werden
*/
//Prüft, ob sich an den Zonen etwas geändert hat und dann wird entsprechend GetZones aufgerufen
var rinconpropcounter = 0;
var rinconpropcounterReset = 0;
function GetTopologieChange() {
    clearTimeout(SoVa.TopologieChangeID);
    try {
        if (SonosZones.Refreshstop === false) {
            rinconpropcounterReset++;
            var request = SonosAjax("GetTopologieChanged");
            request.success(function(data) {
                if (data === null) {
                    SonosLog("GetTopologieChange Data Error");
                    ReloadSite("GetTopologieChange:Data:Null");
                    return;
                }
                //Prüfen was sich geändert hat.
                var rinconprop = Object.getOwnPropertyNames(data);
                if (rinconprop.length === 0) {
                    rinconpropcounter++;
                    if (rinconpropcounter === 3) {
                        rinconpropcounter = 0;
                        ReloadSite("GetTopologieChange:RinconProp");//Reload initialisiert einen neunen SetTimeout
                        return;
                    }
                    SoVa.TopologieChangeID = window.setTimeout("GetTopologieChange()", SoVa.TopologieChangeTime);
                    return;
                }
                if (rinconpropcounter >= 3) {
                    rinconpropcounter = 0;
                }
                //Reset der Counter, wenn der Fehler sehr selten Passiert.
                if (rinconpropcounterReset >= 5) {
                    rinconpropcounter = 0;
                    rinconpropcounterReset = 0;
                }
                for (var i = 0; i < rinconprop.length; i++) {
                    var propzone = rinconprop[i];
                    var proplastchange = data[propzone];
                    //TopologieChange
                    if (propzone === "SonosZones") {
                        if (SonosZones.GetTopologieChangeTime === "INITIAL") {
                            //Setzen des Default auf aktuellen Wert.
                            SonosZones.GetTopologieChangeTime = proplastchange;
                        }
                        if (SonosZones.GetTopologieChangeTime !== proplastchange && SonosZones.Refreshstop === false) {
                            SonosZones.GetTopologieChangeTime = proplastchange;
                            SonosLog("GetTopologieChange:ZonenÄnderung");
                            GetZones();
                            break;
                        }
                    } else {
                        //Player überprüfen
                        //Wenn der Player noch kein Change hat, dann Stop/Pause setzen, sorgt dafür, dass eine Änderung geladen wird.
                        if (typeof SonosZones[propzone] !== "undefined") {
                            if (SonosZones[propzone].LastChange !== proplastchange && SonosZones[propzone].GetPlayerChangeEventIsRunning === false) {
                                SonosZones[propzone].GetPlayerChangeEventIsRunning = true;
                                request =  SonosAjax("GetZonebyRincon","",propzone);
                                request.success(function(data2) {
                                    if (data2 !== null && typeof data2 !== "undefined") {
                                        SonosLog("GetTopologieChange:Geänderte Playerinfos geladen");
                                        try {
                                            SonosZones[data2.CoordinatorUUID].SetBySonosItem(data2);
                                        } catch (Ex) {
                                            alert("Es ist ein Fehler beim SetbySonosItem aufgetreten:" + Ex.Message);
                                        }
                                        SonosZones[data2.CoordinatorUUID].GetPlayerChangeEventIsRunning = false;
                                    } else {
                                        SonosLog("GetTopologieChange:Geänderte Player geladen Daten aber null:" + data2);
                                    }
                                });
                                request.fail(function() {
                                    SonosLog("GetTopologieChange:Konnte die Zone nicht direkt laden");
                                });

                            } else {
                                SonosZones[propzone].CheckPlayerChangeEvent();//Prüfen, ob sich beim Aktualisieren irgendwas verhaspelt hat.
                            }
                        } else {
                            var name = "Unbekannt";
                            if (typeof SonosZones.PlayersUUIDToNames[propzone] !== "undefined") {
                                name = SonosZones.PlayersUUIDToNames[propzone];
                            }
                            SonosZones[propzone] = new SonosZone(propzone, name);
                            SonosLog("GetTopologieChange:PropZone:Undefined");
                        }
                    }
                }
                SoVa.TopologieChangeID = window.setTimeout("GetTopologieChange()", SoVa.TopologieChangeTime);
            });
            request.fail(function(jqXHR) {
                EventErrorsCheck(jqXHR.status, "GetTopologieChange:RequestFail");
            });
        } else {
            //Keine Daten vom Serverladen bei Refreshstop
            if (debug === false) {
                SoVa.TopologieChangeID = window.setTimeout("GetTopologieChange()", SoVa.TopologieChangeTime);
            }
        }
    } catch (ex) {
        SoVa.TopologieChangeID = window.setTimeout("GetTopologieChange()", SoVa.TopologieChangeTime);
        alert("GetTopologieChange: "+ ex.message);
    }
}
function GetAktSongInfo() {
    clearTimeout(SoVa.GetAktSongInfoTimerID);
    try {
        //Bei Refreshstop sich selber aufrufen bis das wieder normal ist
        if (SonosZones.Refreshstop === false && SonosZones.CheckActiveZone() && (typeof SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Stream === "undefined" || SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Stream === false || SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.StreamContent === "Dienst" || SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.StreamContent === "Apple") && (SonosZones[SonosZones.ActiveZoneUUID].PlayState === "PLAYING" || SonosZones[SonosZones.ActiveZoneUUID].CheckCurrenTrackRefesh())) {
            //Wenn MP3 im CurrentTrack leer ist wird dieses auf jedenfall geladen. Außer beim Stream???//todo: 
            var getmp3 = SonosZones[SonosZones.ActiveZoneUUID].CheckCurrenTrackRefesh();

            var cturi = SonosZones[SonosZones.ActiveZoneUUID].CurrentTrack.Uri;
            var request_cur = SonosAjax("GetAktSongInfo", { '': cturi }, getmp3);
            request_cur.success(function(data) {
                //Wenn null Seite neu Laden.
                if (data === null && SonosZones.CheckActiveZone() && SonosZones[SonosZones.ActiveZoneUUID].NumberOfTracks > 0) {
                    SonosLog("GetAktSongInfo: Daten nicht ok aber NumberofTRacks vorhanden. Data");
                    ReloadSite("GetAktSongInfo:Data:null");
                }
                if (data === null || typeof data === "undefined" || data.CurrentTrack === null) {
                    SonosLog("GetAktSongInfo: server Daten NULL");
                    SoVa.GetAktSongInfoTimerID = window.setTimeout("GetAktSongInfo()", 10000);
                    return;
                } else {
                    if (SonosZones.CheckActiveZone()) {
                        SonosZones[SonosZones.ActiveZoneUUID].SetAktSongInfo(data, getmp3);
                    } else {
                        SonosLog("GetAktSongInfo: Aktive Zone nicht vorhanden.");
                    }

                } //Ende currenttrack null
                SoVa.GetAktSongInfoTimerID = window.setTimeout("GetAktSongInfo()", 1500);
            });
            request_cur.fail(function (jqXHR) {
                EventErrorsCheck(jqXHR.status, "GetAktSongInfo: RequestFail");
            });
        } else {
            SoVa.GetAktSongInfoTimerID = window.setTimeout("GetAktSongInfo()", 1000);
        }
    } catch (ex) {
        SoVa.GetAktSongInfoTimerID = window.setTimeout("GetAktSongInfo()", 3000);
        alert("GetAktSongInfo: "+ex.message);
    }
} //Ende CurrentState
function EventErrorsCheck(sourcejqXHR, Source) {
    clearTimeout(SoVa.eventErrorChangeID);
    if (typeof Source !== "undefined") {
        SoVa.eventErrorsSource = Source;
    }
    var request =  SonosAjax("GetTopologieChanged");
    request.success(function() {
        SonosWindows(SoDo.eventError, true);
        ReloadSite(SoVa.eventErrorsSource);
    });
    request.fail(function () {
        SoVa.eventErrorChangeID = window.setTimeout("EventErrorsCheck()", 50000);
        //Layout definieren.
        if (SoDo.eventError.is(":hidden")) {
            SonosWindows(SoDo.eventError, false, { overlay: true });
        }
        //Position der Fehlermeldung definieren und Animieren, damit kein Memoryeffekt eintritt.
        SoDo.eventError.animate({ left: Math.floor((Math.random() * 100) + 1)+'%', top: Math.floor((Math.random() * 100) + 1)+'%' }, 40000);
    });
}

function Eventing() {
    //todo: Eventing deaktivert
    if (typeof (window.EventSource) === "undefined") {
        SoVa.TopologieChangeID = window.setTimeout("GetTopologieChange()", 100);
        return;
    }
    SoVa.TopologieChangeTime = 600000;
    SoVa.TopologieChangeID = window.setTimeout("GetTopologieChange()", 2000);

    var source = new window.EventSource("/sonos/Event");
    source.onopen = function (event) {
        //console.log("Event:Connection Opened " + event.data);
    };
    source.onerror = function (event) {
        if (event.eventPhase === window.EventSource.CLOSED) {
            //console.log("Event:Connection Closed " + event.data);
        } else {
            //console.log("Event:Connection Closed Spezial " + event.data);
            if (confirm("Die SSE Verbindung war Fehlerhaft und es wird versucht auf den Fallback zu gehen!") === true) {
                SoVa.TopologieChangeID = window.setTimeout("GetTopologieChange()", 100);
                this.close();
            }
        }
    };
    source.onmessage = function (event) {
        // document.getElementById('test').innerHTML += event.data;  
        try {
            console.log("Event:Message " + event.data);
            if (typeof event.data === "undefined" || event.data === "") {
                return;
            }

            if (event.data === "ZoneChange") {
                    SonosLog("Eventing:ZonenÄnderung");
                    GetZones();
            } else {
                var PlayerEventData = JSON.parse(event.data);
                if (typeof SonosZones[PlayerEventData.UUID] === "undefined") {
                    GetZones(); //Dann müssen die Zonen neu sein.
                } else {
                    //prüfen, ob LastChange neu ist.
                    if (SonosZones[PlayerEventData.UUID].LastChange !== PlayerEventData.LastChange) {
                        var request = SonosAjax("GetZonebyRincon", "", PlayerEventData.UUID);
                        request.success(function (data2) {
                            if (data2 !== null && typeof data2 !== "undefined") {
                                SonosLog("Eventing:Geänderte Playerinfos geladen");
                                try {
                                    SonosZones[data2.CoordinatorUUID].SetBySonosItem(data2);
                                } catch (Ex) {
                                    alert("Es ist ein Fehler beim SetbySonosItem aufgetreten:" + Ex.Message);
                                }
                                SonosZones[data2.CoordinatorUUID].GetPlayerChangeEventIsRunning = false;
                            } else {
                                SonosLog("Eventing:Geänderte Player geladen Daten aber null:" + data2);
                            }
                        });
                        request.fail(function () {
                            SonosLog("Eventing:Konnte die Zone nicht direkt laden");
                        });










                    }
                }
            }
        } catch (ex) {
            alert(ex.message+"\n\n"+event.data+"\n");
            console.log("Fehlerhafte Event Daten:" + event.data);
        }

        /*
        
        */
        
    };
    console.log("SSE started");
}
