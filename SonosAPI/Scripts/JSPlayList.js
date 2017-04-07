"use strict";
function SonosPlaylist() {
    this.Playlist = [];
    this.IsEmpty = false;
    this.RenderPlaylist = function (stream) {
        if (this.Playlist.length === 0 || !SonosZones.CheckActiveZone()) {
            return;
        }
        if (typeof stream === "undefined") {
            stream = false;
        }

        SonosLog("RenderPlaylist");
        if (SoDo.playListLoader.is(":hidden")) {
            SoDo.playListLoader.slideDown();
        }
        if ($(".currentplaylist").length > 0) {
            $(".currentplaylist").remove();
        }

        if (stream === true || this.IsEmpty === true) {
            SoDo.playListLoader.slideUp();
        } else {
            for (var i = 0; i < this.Playlist.length; i++) {
                var songcover = '';
                var item = this.Playlist[i];
                if (item.AlbumArtURI != null && item.AlbumArtURI !== '' && item.AlbumArtURI !== "leer") {
                    songcover = 'http://' + SonosZones[SonosZones.ActiveZoneUUID].BaseURL + item.AlbumArtURI;
                }
                $('<div id="Currentplaylist_' + (i) + '" class="currentplaylist"><DIV class="currentrackinplaylist" onclick="ShowSongInfos(this)">' + item.Title + '</div><DIV class="curpopdown"><DIV class="playlistcover" data-geladen="NichtGeladen" data-url="' + songcover + '" data-uri="' + item.Uri + '"></DIV><DIV class="playlistplaysmall" onclick="PlayPressSmall(this)"></DIV><DIV class="mediabuttonsmal" onclick="RemoveFromPlaylist(this);return false;"><img src="Images/erase_red.png"></DIV><div class="bomb" onclick="ShowPlaylistRating(this)"><img src="/Images/bombe.png" alt="playlistbomb"/></DIV><DIV onclick="ShowPlaylistRating(this)" class="rating_bar" style="margin-top: 14px;" Style="float:left;"><DIV style="width:0%;"></DIV></DIV><div OnMouseOver="MakeCurrentPlaylistSortable()" OnTouchStart="MakeCurrentPlaylistSortable()" OnTouchEnd="ResortPlaylistDisable()" OnMouseOut="ResortPlaylistDisable()" class="moveCurrentPlaylistTrack"></div><DIV class ="addFavItemPlaylist" onclick="AddFavItem(this,\'playlist\');"></DIV></DIV></div>').appendTo(SoDo.currentplaylistwrapper);
            }
            SoDo.playListLoader.slideUp();
            SoVa.currentplaylistScrolled = false;
            SetCurrentPlaylistSong(SonosZones[SonosZones.ActiveZoneUUID].CurrentTrackNumber, "RenderPlaylist");
        }
        SonosLog("RenderPlaylist Ende");
    }
    this.ReorderPlaylist = function (oldValue, newValue) {
        this.Playlist.splice(newValue, 0, this.Playlist.splice(oldValue, 1)[0]);
    }
    this.RemoveFromPlaylist = function (rem) {
        if (this.Playlist.length === 0 || !SonosZones.CheckActiveZone()) {
            return;
        }
        this.Playlist.splice(rem, 1);
        //numberof Tracks setzen.
        SonosZones[SonosZones.ActiveZoneUUID].SetNumberOfTracks(this.Playlist.length);
        this.RenderPlaylist();
    }
    this.CheckToRender = function (pl) {
        var _playlist;
        if (typeof pl === "undefined") {
            _playlist = this.Playlist;
        } else {
            _playlist = pl;
        }
        if (_playlist === null || typeof _playlist === "undefined") {
            _playlist = [];
        }
        var c =$(".currentplaylist");
        var clength = c.length;
        //Playliste IsEmpty Prüfen.
        if (clength > 0 && this.IsEmpty === true) {
            this.IsEmpty = false;
        }
        var internalmax = (clength - 1);
        if (clength === _playlist.length && clength > 0) {
            //wenn alles gleich, dann ersten und letzten Eintrag testen, evtl. auch noch zwei drei aus der mitte.
            if (c[0].firstChild.innerHTML === _playlist[0].Title && c[internalmax].firstChild.innerHTML === _playlist[internalmax].Title) {
                //hier ist der erste und letzte gleich nun noch ein in der mitte nehmen
                var tei = Math.floor(internalmax / 2);
                var tei3 = Math.floor(internalmax / 3);
                if (c[tei].firstChild.innerHTML === _playlist[tei].Title && c[tei3].firstChild.innerHTML === _playlist[tei3].Title) {
                    return false;
                } else {
                    return true;
                }

            } else {
                return true;
            }
        } else {
            //hier ist nichts gleich, daher neu rendern
            return true;
        }
    }

}