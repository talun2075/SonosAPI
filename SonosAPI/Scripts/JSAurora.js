"use strict";
function NanoleafAurora(option) {
    var BasePath = '/sonos/Nanoleaf/';
    var internalaurora = this;
    var _data;
    var _PowerDom = $("#" + option.PowerDom);
    var _ScenariosDOM = option.ScenariosDOM;
    var _SelectedScenarioClass = option.SelectedScenarioClass;
    var _BrightnessDOMString = option.BrightnessDOM;
    var _BrightnessDOM;
    var _BrightnessDOMValueDOM = $("#"+option.BrightnessDOMValueDOM);
    var _opjectname = option.Name || "aurora";
    var Timer = 0;
    var CallServer = function (url) {
        return $.ajax({
            type: "GET",
            url: BasePath + url,
            dataType: "json"
        });
    }
    this.SetPowerState = function (v) {
        if (typeof (v) === "boolean" && v !== _data.state.on.value) {
            CallServer("SetPowerState/" + v);
            _data.state.on.value = v;
            this.RenderAurora();
        }
    }
    this.SetSelectedScenario = function (v) {
        _data.effects.select = v;
        if (_data.state.on.value !== true) {
            _data.state.on.value = true;
        }
        CallServer("SetSelectedScenario/" + v);
        this.RenderAurora();
        return;
    }
    this.RenderAurora = function () {
        if (typeof _data === "undefined") {
            alert("Aurora ist nicht initialisiert");
            return false;
        }
        //Power
        if (_PowerDom.prop("checked") !== !_data.state.on.value) {
            _PowerDom.prop("checked", !_data.state.on.value);
        }
        //Scenarios
        if (_data.effects.list.length === 0) {
            alert("Keine Scenarien geliefert");
            return false;
        }
        var sd = $("#" + _ScenariosDOM);
        sd.empty();
        $.each(_data.effects.list, function(index, item) {
            var newdiv;
            if (item === _data.effects.select) {
                newdiv = $("<div class=" + _SelectedScenarioClass + ">" + item + "</div>");
            } else {
                newdiv = $("<div>" + item + "</div>");
            }
            newdiv.appendTo(sd);
            newdiv.on("click", function() {
                internalaurora.SetSelectedScenario($(this).html());
            });
        });
        //Brightness
        if (typeof _BrightnessDOM === "undefined") {
            _BrightnessDOMValueDOM.html(_data.state.brightness.value);
            _BrightnessDOM =$("#"+_BrightnessDOMString) .slider({
                orientation: "vertical",
                range: "min",
                min: _data.state.brightness.min,
                max: _data.state.brightness.max,
                value: _data.state.brightness.value,
                stop: function(event, ui) {
                    _data.state.brightness.value = ui.value;
                    CallServer("SetBrightness/" + ui.value);
                    if (_data.state.on.value !== true) {
                        _data.state.on.value = true;
                    }
                    return true;
                },
                slide: function(event, ui) {
                    _BrightnessDOMValueDOM.html(ui.value);
                }
            });
        } else {
            if (_BrightnessDOM.slider("option", "value") !== _data.state.brightness.value) {
                _BrightnessDOM.slider({ value: _data.state.brightness.value });
                _BrightnessDOMValueDOM.html(_data.state.brightness.value);
            }
        }
    return true;
    }
    this.Init = function() {
        this.UpdateData();
        _PowerDom.on("click", function () {
            var t = _PowerDom.prop("checked");
            internalaurora.SetPowerState(!t);
        });
    }
    this.UpdateData = function () {
        clearTimeout(Timer);
        //Init Nanaoleaf && Get Server Data
        var request = CallServer("");
        request.success(function (data) {
            if (typeof data.name !== "undefined") {
                if (data.name === "ERROR") {
                    alert("Fehler beim Initialisieren am Server");
                    return false;
                }
                _data = data;
                internalaurora.RenderAurora();
            } else {
                alert("Fehler beim Initialisieren: Object enthält kein Namen");
            }
            return true;
        }).fail(function () {
            alert("Initialisierung fehlgeschlagen.");
        });
        window.setTimeout(_opjectname + ".UpdateData()", 30000);
    }


}