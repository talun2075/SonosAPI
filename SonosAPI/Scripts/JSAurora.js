"use strict";
function NanoleafAurora(option) {
    var BasePath = '/sonos/Nanoleaf/';
    var internalaurora = this;
    var _data;
    var _Wrapper = $("#" + option.Wrapper);
    var _PowerDom = [];
    var _SelectedScenarioClass = "ssc";
    var _BrightnessDOM = [];
    var _BrightnessSliderValue = [];
    var _opjectname = option.Name || "aurora";
    var Timer = 0;
    var _newData = true;
    var CallServer = function(url) {
        return $.ajax({
            type: "GET",
            url: BasePath + url,
            dataType: "json"
        });
    };
    this.SetGroupScenario = function(v) {
        var sgs =  CallServer("SetGroupScenario/" + v);
        sgs.success(function(data) {
            if (data !== "Done") {
                alert("Es ist ein Fehler aufgetreten:" + data);
            }
            internalaurora.UpdateData();
        });
        
    };
    function GetGroupScenarios() {
        var ggs = CallServer("GetGroupScenario/0");
        ggs.success(function(data) {
            var newdiv = $('<div class="groupcontainer scenarios" id="GroupPowerScenarios"></div>');
            newdiv.appendTo($("#Aurora_Group"));
            var gs = $("#GroupPowerScenarios");
            for (var i = 0; i < data.length; i++) {
                var newscendiv = $('<div class="groupscenario">'+data[i]+'</div>');
                newscendiv.appendTo(gs);
                newscendiv.on("click", function () {
                    internalaurora.SetGroupScenario($(this).html());
                });
            }
        });
    }
    this.GetAurora = function(serial) {
        for (var i = 0; i < _data.length; i++) {
            if (_data[i].NewAurora === true) continue;
            var s = _data[i].NLJ.serialNo;
            if (s === serial) {
                return _data[i].NLJ;
            }
        }
        return false;
    };
    this.SetPowerState = function (v, serial) {
        var au = this.GetAurora(serial);
        if (au === false) return;
        if (typeof v === "boolean" && v !== au.state.on.value) {
            CallServer("SetPowerState/"+serial+"/"+v);
            au.state.on.value = v;
            this.RenderAurora();
        }
    };
    this.SetPower = function(serial) {
        var t = _PowerDom[serial].prop("checked");
        internalaurora.SetPowerState(!t, serial);
    };
    this.SetSelectedScenario = function(v, serialNo) {
        var au = this.GetAurora(serialNo);
        if (au === false) return;
        au.effects.select = v;
        if (au.state.on.value !== true) {
            au.state.on.value = true;
        }
        CallServer("SetSelectedScenario/"+serialNo+"/"+ v);
        this.RenderAurora();
        return;
    };
    this.RenderAurora = function() {
        if (typeof _data === "undefined") {
            alert("Aurora ist nicht initialisiert");
            return false;
        }
        if (_newData ===true) {
            for (var x = 0; x < _data.length; x++) {
                var faid = "new";
                if (_data[x].NewAurora === false) {
                    faid = _data[x].NLJ.serialNo;
                }
                var newAurora = $('<div id="Aurora_' + faid + '" class="auroraContainer"><div class="auroraName">' + _data[x].Name + '</div><div class="container"><input type="checkbox" onClick="' + _opjectname + '.SetPower(\'' + faid + '\')" id="power_' + faid + '" class="powerCheck" name="power_' + faid + '" checked="checked"/><label for="power_' + faid + '" class="power"><span class="icon-off"></span><span class="light"></span></label></div><div class="brightnessSlider" id="BrightnessSlider_' + faid + '" data="' + faid + '"><div id="BrightnessSliderLabel_' + faid + '" class="brightnessSliderLabel">Helligkeit</div><div id="BrightnessSliderValue_' + faid + '" class="brightnessSliderValue">50</div></div><div id="Scenarios_' + faid + '" data="' + faid + '" class="scenarios"></div></div>');
                newAurora.appendTo(_Wrapper);
            }
            if (_data.length > 0) {
                var newAuroraG = $('<div id="Aurora_Group" class="auroraContainer"><div class="auroraName">Alle Aurora</div><div class="groupcontainer" id="GroupPowerOn">Power On</div><div class="groupcontainer" id="GroupPowerOff">Power Off</div></div>');
                newAuroraG.appendTo(_Wrapper);
                GetGroupScenarios();
                $("#GroupPowerOn").on("click", function() {
                    CallServer("SetGroupPowerState/true").success(function () {
                        internalaurora.UpdateData();
                    });
                    
                });
                $("#GroupPowerOff").on("click", function () {
                    CallServer("SetGroupPowerState/false").success(function () {
                        internalaurora.UpdateData();
                    });
                });

            }
            _newData = false;
        }
        for (var i = 0; i < _data.length; i++) {
            //Check new Aurora and continue
            if (_data[i].NewAurora === true) {
                continue;
            }
            var aid = _data[i].NLJ.serialNo;
            //Power
            _PowerDom[aid] = $("#power_" + aid);
            if (_PowerDom[aid].prop("checked") !== !_data[i].NLJ.state.on.value) {
                _PowerDom[aid].prop("checked", !_data[i].NLJ.state.on.value);
            }
            //Scenarios
            if (_data[i].NLJ.effects.effectsList.length === 0) {
                alert("Keine Scenarien geliefert Aurora Serial:" + _data[i].NLJ.serialNo);
                continue;
            }
            var sd = $("#Scenarios_" + aid);//todo: Prüfen, ob wirklich immer gelerrt werden muss evtl. childs zählen und wenn länge gleich nur noch die Klasse setzen
            sd.empty();
            var internalI = i;
            $.each(_data[i].NLJ.effects.effectsList, function(index, item) {
                var newdiv;
                if (item === _data[internalI].NLJ.effects.select) {
                    newdiv = $("<div class=" + _SelectedScenarioClass + ">" + item + "</div>");
                } else {
                    newdiv = $("<div>" + item + "</div>");
                }
                newdiv.appendTo(sd);
                newdiv.on("click", function () {
                    var serial = $(this).parent().attr("data");
                    internalaurora.SetSelectedScenario($(this).html(), serial);
                });
            });
            //Brightness
            if (typeof _BrightnessDOM[aid] === "undefined") {
                _BrightnessSliderValue[aid] = $("#BrightnessSliderValue_" + aid);
                _BrightnessSliderValue[aid].html(_data[i].NLJ.state.brightness.value);
                _BrightnessDOM[aid] = $("#BrightnessSlider_" + aid).slider({
                    orientation: "vertical",
                    range: "min",
                    min: _data[i].NLJ.state.brightness.min,
                    max: _data[i].NLJ.state.brightness.max,
                    value: _data[i].NLJ.state.brightness.value,
                    stop: function (event, ui) {
                        var serial = $(this).attr("data");
                        var au = internalaurora.GetAurora(serial);
                        if (au === false) return false;
                        au.state.brightness.value = ui.value;
                        CallServer("SetBrightness/"+serial+"/" + ui.value);
                        if (au.state.on.value !== true) {
                            au.state.on.value = true;
                            internalaurora.RenderAurora();
                        }
                        return true;
                    },
                    slide: function (event, ui) {
                        var serial = $(this).attr("data");
                        _BrightnessSliderValue[serial].html(ui.value);
                    }
                });
            } else {
                if (_BrightnessDOM[aid].slider("option", "value") !== _data[i].NLJ.state.brightness.value) {
                    _BrightnessDOM[aid].slider({ value: _data[i].NLJ.state.brightness.value });
                    _BrightnessSliderValue[aid].html(_data[i].NLJ.state.brightness.value);
                }
            }
        }

        return true;

    };
    this.Init = function() {
        this.UpdateData();
    };
    this.UpdateData = function() {
        clearTimeout(Timer);
        //Init Nanaoleaf && Get Server Data
        var request = CallServer("");
        request.success(function(data) {
            if (data.length === 0 || data[0].NewAurora === false && typeof data[0].NLJ.name === "undefined") {
                if (data.name === "ERROR") {
                    alert("Fehler beim Initialisieren am Server");
                    return false;
                }
                if (!_Wrapper.is(":empty")) {
                    _Wrapper.empty();
                }
                console.log("Fehler beim Initialisieren: Object enthält kein Namen");

            } else {
                var pl = $("#PreLoad");
                if (pl.is(":visible")) {
                    pl.hide();
                }
                //todo: Änderung der bekannten seriennummer bzw. länge
                if (typeof _data === "undefined" || _data.length !== data.length) {
                    _newData = true;
                    _Wrapper.empty();
                    _PowerDom = [];
                    _BrightnessDOM = [];
                } else {
                    _newData = false;
                }
                _data = data;
                internalaurora.RenderAurora();
            }
            return true;
        }).fail(function() {
            alert("Initialisierung fehlgeschlagen.");
        });
        window.setTimeout(_opjectname + ".UpdateData()", 30000);
    };


}