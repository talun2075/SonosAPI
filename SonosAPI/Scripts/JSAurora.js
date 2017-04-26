"use strict";

function NanoleafAurora(option) {
    var BasePath = '/sonos/Nanoleaf/';
    var _init = false;
    var _powerState = false;
    var _selectedScenario = "leer";
    var _scenarios = [];
    var internalaurora = this;
    var _PowerDom = $("#" + option.PowerDom);
    var _ScenariosDOM = option.ScenariosDOM;
    var _SelectedScenarioClass = option.SelectedScenarioClass;
    var _BrightnessDOM = $("#" + option.BrightnessDOM);
    var CallServer = function (url) {
        return $.ajax({
            type: "GET",
            url: BasePath + url,
            dataType: "json"
        });
    }
   
    this.SetScenarios = function (v)
    {
        if (Array.isArray(_scenarios) && _scenarios.length === 0) {
            _scenarios = v;
            var sd = $("#" + _ScenariosDOM);
                sd.empty();
            $.each(_scenarios, function(index,item) {
                var newdiv;
                if (item === _selectedScenario) {
                    newdiv = $("<div class="+_SelectedScenarioClass+">" + item + "</div>");
                } else {
                    newdiv = $("<div>" + item + "</div>");
                }
                newdiv.appendTo(sd);
                newdiv.on("click", function() {
                    internalaurora.SetSelectedScenario($(this).html());
                });
            });

        }
    }

    this.SetPowerState = function (v) {
        if (typeof (v) === "boolean") {
            if (_init === false && v ===true) {
                _PowerDom.prop("checked",!v);
            } else {
                if (v !== _powerState) {
                    CallServer("SetPowerState/" + v);
                }
            }
            _powerState = v;
        }
    }

    this.SetSelectedScenario = function (v) {
        if (_selectedScenario === "leer" || _scenarios.indexOf(v) !== -1) {
            if (_selectedScenario !== "leer") {
                //Check for PowerState
                if (_powerState === false) {
                    _powerState = true;
                    _PowerDom.prop("checked", false);
                }
                CallServer("SetSelectedScenario/" + v);
                $("#" + _ScenariosDOM + "> .ssc").removeClass();
                $.each($("#" + _ScenariosDOM + "> DIV"), function (index, item) {
                    if (item.innerText === v) {
                        $(item).addClass(_SelectedScenarioClass);
                    }
                });
            }
            _selectedScenario = v;
        }

    }
    this.Init = function() {
        //Init Nanaoleaf && Get Server Data
        var request = CallServer("");
        request.success(function(data) {
            if (typeof data.name !== "undefined") {
                if (data.name === "ERROR") {
                    alert("Fehler beim Initialisieren am Server");
                    return false;
                }
                internalaurora.SetSelectedScenario(data.effects.select);
                internalaurora.SetPowerState(data.state.on.value);
                internalaurora.SetScenarios(data.effects.list);
               
                _BrightnessDOM.slider({
                    orientation: "vertical",
                    range: "min",
                    min: data.state.brightness.min,
                    max: data.state.brightness.max,
                    value: data.state.brightness.value,
                    stop: function (event, ui) {
                        CallServer("SetBrightness/" + ui.value);
                        return true;
                    },
                    slide: function (event, ui) {
                        //todo: Slide definieren.
                    }
                });
                _init = true;
            } else {
                alert("Fehler beim Initialisieren: Object enthält kein Namen");
            }
            return true;
        }).fail(function() {
            alert("Initialisierung fehlgeschlagen.");
        });
        _PowerDom.on("click", function () {
            var t = _PowerDom.prop("checked");
            internalaurora.SetPowerState(!t);
        });
    }



}