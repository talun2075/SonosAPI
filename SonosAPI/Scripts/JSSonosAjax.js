function SonosAjax(_url,  _data, _para1, _para2) {
    /*
	_url= URL als interner Interpreter
	_data= Daten, die an den Server sollen. Meherere als Objekt: {variable1: "value1", variable2:"value2"}
    _para1 und 2 Optionale Parameter, die nur verwendung finden bei speziellen Cases.
	*/
    if (typeof _url === "undefined" || _url === null || _url === "") {
        return false;
    }
    var url;
    var type;
    switch (_url) {
        case "BaseUrl":
            url = SoVa.apiPlayerURL + "/BaseURL/" + _data;
            break;
        case "LoadDevice":
            url = SoVa.apiDeviceURL;
            break;
        case "GetCoordinator":
            url = SoVa.apiDeviceURL + "GetCoordinator/0";
            break;
        case "SetGroups":
            url = SoVa.apiPlayerURL + "SetGroups/" + SoVa.masterPlayer;
            type = "POST";
            break;
        case "SetFadeMode":
            url = SoVa.apiPlayerURL + "SetFadeMode/" + SonosZones[SonosZones.ActiveZoneUUID].ZoneUUID + "/" + !SonosZones[SonosZones.ActiveZoneUUID].FadeMode;
            break;
        case "SetVolume":
            if (typeof _para1 === "undefined" || typeof _para2 === "undefined") {
                return false;
            }
            url = SoVa.apiPlayerURL + "SetVolume/" + _para1 + "/" + _para2;
            break;
        case "SetGroupVolume":
            if (typeof _para1 === "undefined" || typeof _para2 === "undefined") {
                return false;
            }
            url = SoVa.apiPlayerURL + "SetGroupVolume/" + _para1 + "/" + _para2;
            break;
        case "SaveQueue":
        case "ExportQueue":
        case "Enqueue":
        case "ReplacePlaylist":
        case "RemoveFavItem":
        case "AddFavItem":
        case "SetSongMeta":
        case "Browsing":
        case "SetSleepTimer":
        case "Seek":
        case "SetSongInPlaylist":
        case "SetFilterRating":
        case "DestroyAlarm":
        case "SetAlarm":
        case "SetRatingFilter":
            url = SoVa.apiPlayerURL + _url + "/" + SonosZones.ActiveZoneUUID;
            type = "POST";
            break;
        case "GetSongMeta":
            url = SoVa.apiPlayerURL + _url + "/0";
            type = "POST";
            break;
        case "GetPlaylists":
        case "GetTopologieChanged":
            url = SoVa.apiPlayerURL + _url + "/0";
            break;
        case "RemoveSongInPlaylist":
            url = SoVa.apiPlayerURL + "RemoveSongInPlaylist/" + SonosZones.ActiveZoneUUID + "/" + _para1;
            break;
        case "ReorderTracksinQueue":
            url = SoVa.apiPlayerURL + "ReorderTracksinQueue/" + SonosZones.ActiveZoneUUID + "/" + _para1 + "/" + _para2;
            break;
        case "SetAudioIn":
        case "GetErrorListCount":
        case "GetErrorList":
        case "SetUpdateMusicIndex":
        case "GetUpdateIndexInProgress":
        case "Next":
        case "SetMute":
        case "Previous":
        case "GetAlarms":
            url = SoVa.apiPlayerURL + _url+ "/" + SonosZones.ActiveZoneUUID;
            break;
        case "GetZonebyRincon":
        case "GetPlayerPlaylist":
        case "GetPlayState":
        case "Play":
        case "Pause":
            url = SoVa.apiPlayerURL + _url + "/" + _para1;
            break;
        case "GetAktSongInfo":
            url = SoVa.apiPlayerURL + "GetAktSongInfo/" + SonosZones.ActiveZoneUUID + "/" + _para1;
            type = "POST";
            break;
        case "AlarmEnable":
            url = SoVa.apiPlayerURL + _url + "/" + SonosZones.ActiveZoneUUID + "/" + _para1 + "/" + _para2;
            break;
        case "SetPlaymode":
            url = SoVa.apiPlayerURL + _url + "/" + SonosZones.ActiveZoneUUID + "/" + _para1;
            break;
        case "aagb":
            url = "";
            break;
        default:
            alert("Übergeben url ist nicht definiert und kann somit nicht interpretiert werden.");
            return false;
            
    }

    //Es werden aktuell nur Get und Post Supportet
    if (type !== "GET" && type !== "POST") {
        type = "GET";
    }
    //Wenn keine Daten definiert sind, dann leer überegeben
    if (typeof _data === "undefined") {
        _data = "";
    }
    return $.ajax({
        type: type,
        url: url,
        data: _data,
        dataType: "json"
    });
}