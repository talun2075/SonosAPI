"use strict";
//Container etc hinterlegen.
var alarmClockDOM; //Button zum Klicken um den Wecker anzuzeigen
var alarmClockDIV; //Wecker DIV in dem alles hinein gerendert wird
var alarmClockWrapper; //Wrapper, damit die Scrollleiste funktioniert
var alarmClockDIVLoader; //Loader für den Wecker
var rooms; // Alle Räume mit Namen und UUID
var roomsAlarmTime = new Array(); //Pro Raum alle Zeiten definieren. 
$(document).ready(function(){
	SonosLog("Alarm Clock Initial");
	$('<div id="AlarmClockOpen" class="mediabuttonring"></div>').appendTo(SoDo.bodydiv);
	$('<div id="AlarmClock" class="alarmclockarrow"></div>').appendTo(SoDo.bodydiv);
	alarmClockDIV = $("#AlarmClock");
	alarmClockDOM = $("#AlarmClockOpen");
	$('<div id="AlarmClockLoader"><img alt="Device Loader"id="AlarmClockLoaderIMG" src="/Images/ajax-loader.gif" /></div>').appendTo(alarmClockDIV);
	alarmClockDIVLoader = $("#AlarmClockLoader");
	alarmClockDIVLoader.hide();
	$('<div id="AlarmClockWrapper"></div>').appendTo(alarmClockDIV);
	alarmClockWrapper=$("#AlarmClockWrapper");
	alarmClockDOM.on("click", function(){
		LoadPlayers();
		alarmClockDOM.toggleClass("aktiv");
	});
	SonosLog("Alarm Clock Initinal End");
	});
//Läd und füllt den Kalender
function ACShow(rel){
	SonosLog("AlarmClock Show");
	if(rel !=="reload"){
		SonosWindows(alarmClockDIV);
	}
	if(alarmClockDIV.is(":hidden")){
		return;
	}
	alarmClockDIVLoader.show();
	alarmClockWrapper.empty();
	var request = SonosAjax("GetAlarms");
    request.success(function (data) {
        SonosLog("AlarmClock data loaded");
		$.each(data, function(i, item){
			var check = "";
			if(item.Enabled){
				check ="checked";
			}
			if (typeof roomsAlarmTime[item.RoomUUID] === "undefined") {
			    roomsAlarmTime[item.RoomUUID] = new Array();
			    rooms[item.RoomUUID] = "Player Offline";
			}
			roomsAlarmTime[item.RoomUUID].push(item.StartTime);

			var datatag = 'data-id="'+item.ID+'" data-starttime="'+item.StartTime+'" data-duration="'+item.Duration+'" data-recurrence="'+item.Recurrence+'" data-roomuuid="'+item.RoomUUID+'" data-enabled="'+item.Enabled+'" data-programuri="'+item.ProgramURI+'" data-playmode="'+item.PlayMode+'" data-volume="'+item.Volume+'" data-includelinkedzones="'+item.IncludeLinkedZones+'"';
			$('<div id="Alarm_'+item.ID+'" class="alarm" '+datatag+'><div class="displayflex" OnClick="EditAlarm('+item.ID+')"><div class="displayflex"><div class="roomname">'+rooms[item.RoomUUID]+'</div><div class="starttime">'+item.StartTime+'</div><div class="startdate">'+StartDates(item.Recurrence)+'</div></DIV><div class="alarmonoffswitch"><input type="checkbox" OnClick="AlarmEnabled(this)" name="alarmonoffswitch" class="alarmonoffswitch-checkbox" id="myonoffswitch'+item.ID+'" '+check+'/><label class="alarmonoffswitch-label" for="myonoffswitch'+item.ID+'"><div class="alarmonoffswitch-inner"></div><div class="alarmonoffswitch-switch"></div></label></div></div></div>').appendTo(alarmClockWrapper);
		});
		$('<div id="Neu" class="alarm"><div class="displayflex" OnClick="EditAlarm(\'Neu\')"><div class="displayflex"><div class="roomname">Neu</div><div class="starttime"></div><div class="startdate">Never</div></DIV><div class="alarmonoffswitch"></div>').appendTo(alarmClockWrapper);
		
		alarmClockDIVLoader.hide();
		SonosLog("AlarmClock each Alarm Loaded");
    });
	request.fail(function (jqXHR) { if(jqXHR.statusText==="Internal Server Error"){ReloadSite("ACShow");
	}else{alert("Beim laden von ACShow ist ein Fehler aufgetreten.");} });
}	
var editAlarmDIV; //Editalarm DIV
var editAlarmDIVWrapper; //Entsprechender Wrapper
var editalarminitianal = true; //Erstaufruf EditAlarm
var editAlarmEnabledDIV; //Enthält die Inputs für Enabled
var editAlarmEnabledTrue; //Enabled true Input
var editAlarmEnabledFalse; //enabled False input
var editAlarmStartTimeDIV; //Div für die Start Uhrzeit
var editAlarmStartTimeHour; //Input für Starttime Stunden
var editAlarmRoomSelection; //Selector für die Räume
var editAlarmRoomSelectionWrapper; //Warpper für Raum Selector
var editAlarmDaysSelectionWrapper; //Wrapper für die Tage
var editAlarmDaysSelection; //Enthält alle Checkboxen für die Tage
var editAlarmDaysSelectionCLASS; //Klasse mit allen Checkboxen für die Tage
var editAlarmDaysSelectionOnce; //Für einmalig auswählen eines Tages
var editAlarmDaysSelectionDaily; //Für alle Tage
var editAlarmDaysSelectionWeekDays;// Wochentage
var editAlarmDaysSelectionWeekEnd;//Wochenende
var editAlarmVolumeWrapper;//Lautstärken Wrapper
var editAlarmVolumeSlider; //Lautstärken Slider
var editAlarmVolumeInput; //Lautstärken Input
var editAlarmDurationWrapper; //Wrapper für die Dauer des Weckers
var editAlarmDurationSelection; //Selection des Weckers
var editAlarmIncludeRoomsWrapper;//Wrapper included Rooms
var editAlarmIncludeRoomsChecker;//Checkbox included Rooms
var editAlarmPlaylistWrapper; //Wiedergabe Wrapper
var editAlarmPlaylist; //Wiedergabeliste input
var selectAlarmPlaylistDIV; //Auswahl einer Playlist für Input
var selectAlarmPlaylistWrapper;//Wraper für die Auswahl der Playlisten
var editAlarmRandomWrapper;//Wrapper für die zufällige wiedergabe
var editAlarmRandomChecker; //Checkbox zufällige wiedergabe
var editAlarmSave; //Der Speicherbutton um die Änderungen an den Server zu senden.
var destroyAlarm; //Der Button zum zerstören des Weckers
var editalarmid; //Hält die ID, des Alarms, welcher gerade geändert wird
var editalarmidStartTime; //Hält zur Kontrolle die Starttime beim Beginn des editierens, da nur Pro Zone eine Zeit genommen werden darf und dies beim Speichern getestet werden muss.
function EditAlarm(t){
	SonosLog("EditAlarm");
	if(editalarminitianal === true){
		SonosLog("EditAlarm Initial");	
		editAlarmDIV = $('<DIV id="EditAlarm"></DIV>');
		editAlarmDIV.appendTo(SoDo.bodydiv);	
		editAlarmDIVWrapper = $('<DIV id="EditAlarmWrapper"></DIV>');
		editAlarmDIVWrapper.appendTo(editAlarmDIV);
		editAlarmEnabledDIV = $('<DIV id="EditAlarmEnabledWrapper" class="displayflex"><DIV class="editalarmenabled"><input type="radio" id="Editalarenabledtrue" name="enabled" value="true"> An</DIV><DIV class="editalarmenabled"><input type="radio" id="Editalarenabledfalse" name="enabled" value="false"> Aus</DIV></DIV>');
		editAlarmEnabledDIV.appendTo(editAlarmDIVWrapper);
		editAlarmEnabledTrue = $("#Editalarenabledtrue");
		editAlarmEnabledFalse = $("#Editalarenabledfalse");
		editAlarmStartTimeDIV = $('<DIV id="EditAlarmStartTimeWrapper" class="displayflex">Startzeit:<DIV id="EditAlarmStartTimeDIV"><input type="time" id="EditAlarmStartTimeHour" maxlength="5"></DIV></DIV>');
		editAlarmStartTimeDIV.appendTo(editAlarmDIVWrapper);
		editAlarmStartTimeHour = $("#EditAlarmStartTimeHour");
		editAlarmStartTimeHour.on( "change", function(){
			//eingabe am Browser prüfen. Ipad und co werden eine Drobdown anzeigen.
			var tval = $(this).val();
			var indp = tval.indexOf(":");
			if(tval.length < 4){alert("Die Eingegebene Startzeit kann nicht interpretiert werden. Bitte anpassen");}
			if(tval.length === 4){
				if(indp !== -1){
					alert("Die Eingegebene Startzeit kann nicht interpretiert werden. Bitte anpassen");
				}else{
					//var stunden = tval.substr(0,2);
					var modifiedtime = tval.substr(0,2)+":"+tval.substr(2,2);
					$(this).val(modifiedtime);
				}
			}
			if(tval.length === 5){
					if(indp !==2){
						alert("Die Eingegebene Startzeit kann nicht interpretiert werden. Bitte anpassen");
					}
			}
		});
		editAlarmRoomSelectionWrapper =$('<DIV id="EditAlarmRoomSelectionWrapper" class="displayflex">Raum:</DIV>');
		editAlarmRoomSelection.appendTo(editAlarmRoomSelectionWrapper);
		editAlarmRoomSelectionWrapper.appendTo(editAlarmDIVWrapper);
		editAlarmDaysSelectionWrapper = $('<DIV id="EditAlarmDaysSelectionWrapper"><DIV id="EditAlarmDaysSelectionWrapperText">Tage:</DIV></DIV>');
		editAlarmDaysSelectionWrapper.appendTo(editAlarmDIVWrapper);
		editAlarmDaysSelection = $('<DIV><input type="Checkbox" id="EditAlarmDaysSelectionOnce" class="editAlarmDaysSelection">Einmalig</DIV><DIV><input type="Checkbox" id="EditAlarmDaysSelectionDaily" class="editAlarmDaysSelection">Täglich</DIV></br><DIV><input type="Checkbox" id="editAlarmDaysSelection1" class="editAlarmDaysSelection editAlarmDaysSelectionWeek">Montag</DIV><DIV><input type="Checkbox" id="editAlarmDaysSelection2" class="editAlarmDaysSelection editAlarmDaysSelectionWeek">Dienstag</DIV><DIV><input type="Checkbox" id="editAlarmDaysSelection3" class="editAlarmDaysSelection editAlarmDaysSelectionWeek">Mittwoch</DIV><DIV><input type="Checkbox" id="editAlarmDaysSelection4" class="editAlarmDaysSelection editAlarmDaysSelectionWeek">Donnerstag</DIV><DIV><input type="Checkbox" id="editAlarmDaysSelection5" class="editAlarmDaysSelection editAlarmDaysSelectionWeek">Freitag</DIV><DIV><input type="Checkbox" id="editAlarmDaysSelection6" class="editAlarmDaysSelection editAlarmDaysSelectionWeekEnd">Samstag</DIV><DIV><input type="Checkbox" id="editAlarmDaysSelection0" class="editAlarmDaysSelection editAlarmDaysSelectionWeekEnd">Sonntag</DIV>');
		editAlarmDaysSelection.appendTo(editAlarmDaysSelectionWrapper);
		editAlarmDaysSelectionCLASS=$('.editAlarmDaysSelection');
		editAlarmDaysSelectionCLASS.on("change",function(item){CheckDays(item);});
		editAlarmDaysSelectionOnce = $('#EditAlarmDaysSelectionOnce');
		editAlarmDaysSelectionDaily = $('#EditAlarmDaysSelectionDaily');
		editAlarmDaysSelectionWeekDays = $('.editAlarmDaysSelectionWeek');
		editAlarmDaysSelectionWeekEnd = $('.editAlarmDaysSelectionWeekEnd');
		editAlarmVolumeWrapper = $('<DIV id="EditAlarmVolumeWrapper"></DIV>');
		editAlarmVolumeWrapper.appendTo(editAlarmDIVWrapper);
		editAlarmVolumeSlider = $('<DIV id="EditAlarmVolumeSlider"></DIV>');
		editAlarmVolumeSlider.appendTo(editAlarmVolumeWrapper);
		editAlarmVolumeSlider.slider({
		    orientation: "horizontal",
		    range: "min",
		    min: 1,
		    max: 100,
		    value: 1,
		    Stop: function (event, ui) {
		        //On Stop
		        editAlarmVolumeInput.val(ui.value);
		    },
		    slide: function (event, ui) {
		       //Slide
			   editAlarmVolumeInput.val(ui.value);
		    }
		});
		editAlarmVolumeInput = $('<input type="number" maxlength="3" min="1" max="100" step=1 id="EditAlarmVolumeInput">');
		editAlarmVolumeInput.appendTo(editAlarmVolumeWrapper);
		editAlarmVolumeInput.on("change",function(){
			var num = parseInt(editAlarmVolumeInput.val())||1;
			if(num >100){
				num = 100;
				
			}
			editAlarmVolumeInput.val(num);
			editAlarmVolumeSlider.slider({value:num});
		});
		editAlarmVolumeInput.on("click",function(){
			editAlarmVolumeInput.select();
		});
		editAlarmDurationWrapper = $('<DIV id="EditAlarmDurationWrapper">Dauer:</DIV>');
		editAlarmDurationWrapper.appendTo(editAlarmDIVWrapper);
		editAlarmDurationSelection = $('<select id="EditAlarmDurationSelection"><option value="00:30:00">30 Minuten</option><option value="00:45:00">45 Minuten</option><option value="01:00:00">1 Stunde</option><option value="02:00:00">2 Stunden</option><option value="03:00:00">3 Stunden</option><option value="">Unbegrenzt</option></select>');
		editAlarmDurationSelection.appendTo(editAlarmDurationWrapper);
		editAlarmPlaylistWrapper = $('<DIV id="EditAlarmPlaylistWrapper">Wiedergabeliste: <input type="text" id="EditAlarmPlaylist"><DIV id="EditAlarmPlaylistSet" OnClick="SelectAlarmPlaylist()">Set</DIV></DIV>');
		editAlarmPlaylistWrapper.appendTo(editAlarmDIVWrapper);
		editAlarmPlaylist =$('#EditAlarmPlaylist');
		selectAlarmPlaylistDIV = $('<DIV id="SelectAlarmPlaylist"><DIV id="SelectAlarmPlaylistWrapper"></DIV></DIV>');
		selectAlarmPlaylistDIV.appendTo(SoDo.bodydiv);
		selectAlarmPlaylistWrapper =$('#SelectAlarmPlaylistWrapper');
		editAlarmIncludeRoomsWrapper = $('<DIV id="EditAlarmIncludeRoomsWrapper"><input type="Checkbox" id="EditAlarmIncludeRoomsChecker">Inkl. gruppierte Räume</DIV>');
		editAlarmIncludeRoomsWrapper.appendTo(editAlarmDIVWrapper);
		editAlarmIncludeRoomsChecker = $('#EditAlarmIncludeRoomsChecker');
		editAlarmRandomWrapper = $('<DIV id="EditAlarmRandomWrapper"><input type="Checkbox" id="EditAlarmRandomChecker">Zufällige Wiedergabe</DIV>');
		editAlarmRandomWrapper.appendTo(editAlarmDIVWrapper);
		editAlarmRandomChecker = $('#EditAlarmRandomChecker');
		editAlarmSave = $('<DIV id="EditAlarmSave" OnClick="SaveAlarmChanges()">Save</DIV>');
		editAlarmSave.appendTo(editAlarmDIVWrapper);
		destroyAlarm = $('<DIV id="EditAlarmDestroy" OnClick="DestroyAlarm()">Löschen</DIV>');
		destroyAlarm.appendTo(editAlarmDIVWrapper);
		editalarminitianal = false;
	}
	if(editAlarmDIV.is(":visible")){
		SonosWindows(editAlarmDIV);
		SonosLog("EditAlarm Hide");
		return;
	}
	editalarmid = t;
	if(t==="Neu"){
	    editAlarmEnabledTrue.prop("checked", false);
	    editAlarmEnabledFalse.prop("checked", false);
	    editAlarmStartTimeHour.val("09:00");
	    editAlarmVolumeSlider.slider({ value: 10 });
	    editAlarmVolumeInput.val(10);
	    editAlarmRandomChecker.prop("checked", false);
	}else{
			var dataitem = $("#Alarm_"+t); //Dataholder
			//Enabled setzen
			editAlarmEnabledTrue.prop("checked", false);
			editAlarmEnabledFalse.prop("checked", false);
			var ena = dataitem.attr("data-enabled");
			if(ena==="true"){
				editAlarmEnabledTrue.prop("checked", true);
			}else{
				editAlarmEnabledFalse.prop("checked", true);
			}
			//Startzeit setzen
			var starttimetemp = dataitem.attr("data-starttime");
			editalarmidStartTime = starttimetemp;
			var splittime = starttimetemp.split(':');
			editAlarmStartTimeHour.val(splittime[0]+":"+splittime[1]);
			//Raum auswählen
			var roomuuidtemp = dataitem.attr("data-roomuuid");
			editAlarmRoomSelection.val(roomuuidtemp);
			//Playlist auswählen
			var programuri = dataitem.attr("data-programuri");
			var lastslash = programuri.lastIndexOf("/");
			editAlarmPlaylist.val(programuri.substr(lastslash+1));
			editAlarmPlaylist.attr("data-containerid","Unchanged");
			//Tage
			var days = dataitem.attr("data-recurrence");
			editAlarmDaysSelectionCLASS.prop("checked", false);	
			switch(days){
				case 'ONCE':
					editAlarmDaysSelectionOnce.prop("checked", true);	
				break;
				case 'WEEKDAYS':
					editAlarmDaysSelectionWeekDays.prop("checked", true);
				break;
				case 'WEEKENDS':
					editAlarmDaysSelectionWeekEnd.prop("checked", true);
				break;
				case 'DAILY':
					editAlarmDaysSelectionDaily.prop("checked", true);
				break;
				default:
				//einzeltage verarbeiten
				var tagestring = days.substr(3);
				var tage = tagestring.split("");
				for(var i=0;i < tage.length;i++){
					$("#editAlarmDaysSelection"+tage[i]).prop("checked", true);
			
				}
			}
			//Lautstärke
			var volume = dataitem.attr("data-volume");
			editAlarmVolumeSlider.slider({value:volume});
			editAlarmVolumeInput.val(volume);
			//Dauer
			var duration = dataitem.attr("data-duration");
			editAlarmDurationSelection.val(duration);
			//IncludeLinkedZones
			var includelinkedzones = dataitem.attr("data-includelinkedzones");
			if(includelinkedzones ==="true"){
				editAlarmIncludeRoomsChecker.prop("checked",true);
			}else{
				editAlarmIncludeRoomsChecker.prop("checked",false);
			}
			//Random
			var random = dataitem.attr("data-playmode");
			if(random ==="NORMAL"){
				editAlarmRandomChecker.prop("checked",false);
			}else{
				editAlarmRandomChecker.prop("checked",true);
			}
	}
	SonosLog("EditAlarm Data Loaded");
	SonosWindows(editAlarmDIV,false,{overlay:true});
}
//Als erstes die Player laden
function LoadPlayers(){
		var uuids = Object.getOwnPropertyNames(SonosZones.PlayersUUIDToNames);
		rooms = SonosZones.PlayersUUIDToNames;
		editAlarmRoomSelection = $('<select id="AlarmSonosPlayerSelector" name="AlarmSonosPlayerSelector"></select>');
		for(var i=0;i<uuids.length;i++){
			var uuid = uuids[i];
			$('<option value="'+uuid+'">'+rooms[uuid]+'</option>').appendTo(editAlarmRoomSelection);
			roomsAlarmTime[uuid] = new Array();
		}
		ACShow(); //Alarme laden
}	
//Prüft, bei EditAlarms die Tage und reagiert auf bestimmte Änderungen.
function CheckDays(item){
	var id = $(item.target).attr("id");
	switch(id){
		case 'EditAlarmDaysSelectionOnce':
			editAlarmDaysSelectionCLASS.prop("checked", false);
			editAlarmDaysSelectionOnce.prop("checked", true);
		break;
		case 'EditAlarmDaysSelectionDaily':
			editAlarmDaysSelectionCLASS.prop("checked", false);
			editAlarmDaysSelectionWeekDays.prop("checked", true);
			editAlarmDaysSelectionWeekEnd.prop("checked", true);
			editAlarmDaysSelectionDaily.prop("checked", true);
		break;
		default:
			editAlarmDaysSelectionOnce.prop("checked", false);
			editAlarmDaysSelectionDaily.prop("checked", false);
			var daycounter = $('.editAlarmDaysSelection:checked').length;
			if(daycounter === 7){
				editAlarmDaysSelectionDaily.prop("checked", true);
			}
		break;
	}

}
//Gibt die Tage zurück, wann der Wecker losläuft.
function StartDates(recurrence){
	
	SonosLog("StartDates Insert:"+recurrence);
	var res;
	switch(recurrence){
		case 'ONCE':
			res = "Einmalig";
		break;
		case 'WEEKDAYS':
			res = "Mo-Fr";
		break;
		case 'WEEKENDS':
		case 'ON_06':
			res = "Sa und So";
		break;
		case 'DAILY':
			res = "Täglich";
		break;
		default:
		//einzeltage verarbeiten
		var tagestring = recurrence.substr(3);
		var tage = tagestring.split("");
		var inrow = true;
		//Prüfen, ob ide Tage aufeinander folgend sind.
		    var i;
		    for(i = 0;i < tage.length;i++){
			var next = parseInt(tage[i+1]);
			var now = parseInt(tage[i]);
			if(!isNaN(next)){
				if(next-now !==1){
					inrow=false;
					i=tage.length;
				}
			}
		}
		if(tage.length ===1){
			inrow=false;
		}
		//String für die Tage bauen
		if(inrow === true){
			res = GetDays(tage[0])+" - "+GetDays(tage[tage.length-1]);
		}else{
			res ="";
			for(i = 0;i < tage.length;i++){
				res += GetDays(tage[i])+",";
			}
		}		

	}
	//Letztes Komma entfernen
	if(res.lastIndexOf(",") === (res.length-1)){
		res = res.substr(0, res.length-1);
	}
	SonosLog("StartDates Return:"+res);
	return res;
}
//Gibt für die Übergebene Zahl einen Tag zurück
function GetDays(v){
	SonosLog("GetDays Insert:"+v);
	var d="KeinTagGewählt";
	switch(v){
		case '0':
			d="So";
		break;
		case '1':
			d="Mo";
		break;
		case '2':
			d="Di";
		break;
		case '3':
			d="Mi";
		break;
		case '4':
			d="Do";
		break;
		case '5':
			d="Fr";
		break;
		case '6':
			d="Sa";
		break;
	
	}
	SonosLog("GetDays Return:"+d);
	return d;
}
//Alarm aktivieren, deaktivieren im Backend
function AlarmEnabled(t){
	SonosLog("AlarmEnabled");
	var ena = $(t).prop("checked");
	var dataholder = $(t).parent().parent().parent();
	var id = dataholder.attr("data-id");
	dataholder.attr("data-enabled", ena);
    SonosAjax("AlarmEnable", "", id, ena);
	SonosLog("AlarmEnabled ID:"+id+" Status:"+ena);
}
//Auswahl der Playlist für den Wecker
function SelectAlarmPlaylist(){
	SonosLog(SelectAlarmPlaylist);
	selectAlarmPlaylistWrapper.empty();
	var request =SonosAjax("Browsing",{ '': "A:PLAYLISTS" });
    request.success(function (alarmplaylistdata) {
		$.each(alarmplaylistdata, function(i, item){
			$('<DIV OnClick="SetAlarmPlaylist(this)" data-title="' + item.Title + '" data-containerid="' + item.ContainerID + '">'+item.Title+'</DIV>').appendTo(selectAlarmPlaylistWrapper);
		});
		SonosWindows(selectAlarmPlaylistDIV);
	});
}
//Playlist laden und zur Auswahl bereit machen.
function SetAlarmPlaylist(t){
	var item = $(t);
	var titel = item.attr("data-title");
	var containerid = item.attr("data-containerid");
	editAlarmPlaylist.val(titel);
	editAlarmPlaylist.attr("data-containerid",containerid);
	SonosWindows(selectAlarmPlaylistDIV,true);
}
function DestroyAlarm() {
    var retval = confirm("Soll der Alarm wirklich gelöscht werden?");
    if (retval) {
        var request = SonosAjax("DestroyAlarm", { '': editalarmid });
        request.success(function() {
            SonosWindows(editAlarmDIV);
            ACShow("reload");
        });
        request.fail(function() {
            SonosWindows(editAlarmDIV);
            ACShow("reload");
        });
    }

}
function SaveAlarmChanges(){
	var saveRoomuuid = editAlarmRoomSelection.val();
	//enabled prüfen
	var saveenable = false;
	if(editAlarmEnabledTrue.prop("checked")){
		saveenable = true;
	}
	//StartTime
	var temptime = editAlarmStartTimeHour.val();
	if(temptime.length !== 5){
		alert("Die Eingegebene Startzeit kann nicht interpretiert werden. Bitte anpassen");
		return;
	}
	var savestarttime =	temptime+":00";
	//Prüfen, ob Startzeit für diesen Raum schon vorhanden ist
	if(roomsAlarmTime[saveRoomuuid].indexOf(savestarttime) > -1){
		if(savestarttime === editalarmidStartTime){
			//Die Zeit, die gefunden wurde, ist die Zeit des geöffneten Weckers.
		}else{
			alert("Es gibt schon einen Wecker für diese Zone mit der Startzeit "+temptime+". Bitte wähle eine andere Uhrzeit");
			return;
		}
	}
	//Playlist
	if(typeof editAlarmPlaylist.attr("data-containerid") == "undefined"){
		alert("Es fehlt die Wiedergabeliste, bitte eine auswählen.");
		return;
	}
	
	
	//Tage
	var tage = "";
	if(editAlarmDaysSelectionDaily.prop("checked")){
		tage = "DAILY";
	}else if(editAlarmDaysSelectionOnce.prop("checked")){
		tage = "ONCE";
	}else{
		var tagearray = new Array();
	    var i;
	    for(i = 0;i < 7;i++){
			if($('#editAlarmDaysSelection'+i).prop("checked")){
				tagearray.push(i);
			}
		}
		if(tagearray.length > 1){
			tage = "ON_";
			for(i = 0;i < tagearray.length;i++){
				tage += tagearray[i];
			}
		
		}else{
			if(typeof tagearray[0] == "undefined"){
				alert("Es wurde nicht ausgewählt, wann der Wecker losgehen soll.");
				return;
			}
			tage = "ON_"+tagearray[0];
		}
	}
	if(editAlarmVolumeInput.val() ===""){
		editAlarmVolumeInput.val("0");
	}
	var sendvalue = editalarmid+"#"+editAlarmEnabledTrue.prop("checked")+"#"+savestarttime+"#"+saveRoomuuid+"#"+tage+"#"+editAlarmVolumeInput.val()+"#"+editAlarmDurationSelection.val()+"#"+editAlarmPlaylist.attr("data-containerid")+"#"+editAlarmIncludeRoomsChecker.prop("checked")+"#"+editAlarmRandomChecker.prop("checked")+"#"+editAlarmPlaylist.val();
	var request = SonosAjax("SetAlarm", { '': sendvalue });
	request.success(function () { ACShow("reload"); });
    request.fail(function () { alert("Beim laden der Aktion:SaveAlarmChanges für Wecker "+editalarmid+" ist ein Fehler aufgetreten."); });
	
	SonosWindows(editAlarmDIV);
	
	
}



