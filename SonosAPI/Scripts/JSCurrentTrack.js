"use strict";
function SonosCurrentTrack() {
    this.Album = "leer";
    this.AlbumArtURI = "leer";
    this.Artist = "leer";
    this.MP3 = new MP3();
    this.MetaData = "leer";
    this.Stream = false;
    this.Title = "leer";
    this.Uri = "leer";
    this.BaseURL = "leer";
    this.StreamContent = "leer";
    this.SetCurrentTrack = function(ct) {
        var haschanged = false;
        var mp3change = false;
        if (typeof ct === "undefined" || ct === null) {
            return haschanged;
        }
        if (this.Album !== ct.Album && ct.Album !== null) {
            SonosLog("CurrenTRack: Album vs Neues Album: " + this.Album + " vs " + ct.Album);
            this.Album = ct.Album;
            haschanged = true;
        }
        if (this.AlbumArtURI !== ct.AlbumArtURI && ct.AlbumArtURI !== null) {
            SonosLog("CurrenTRack: AlbumArtURI vs Neues AlbumArtURI: " + this.AlbumArtURI + " vs " + ct.AlbumArtURI);
            this.AlbumArtURI = ct.AlbumArtURI;
            haschanged = true;
        }
        if (this.Artist !== ct.Artist && ct.Artist !== null) {
            //hier nun prüfen, ob einer der Artisten mit the beginnt und beim vergleich das dann entsprechend entfernen.
            var artold = this.Artist;
            var artnew = ct.Artist;
            if (artold.substring(0, 4).toLowerCase() === "the ") {
                artold = artold.substring(4, artold.length);
            }
            if (artnew.substring(0, 4).toLowerCase() === "the ") {
                artnew = artnew.substring(4, artnew.length);
            }
            if (artold !== artnew) {
                SonosLog("CurrenTRack: Artist vs Neues Artist: " + this.Artist + " vs " + ct.Artist);
                this.Artist = ct.Artist;
                haschanged = true;
            }
        }
        if (this.MetaData !== ct.MetaData && ct.MetaData !== null) {
            this.MetaData = ct.MetaData;
            haschanged = true;
        }
        if (ct.MetaData === null && this.MetaData !== "leer") {
            this.MetaData = "leer";
            haschanged = true;
        }
        if (this.Stream !== ct.Stream && ct.Stream !== null) {
            this.Stream = ct.Stream;
            haschanged = true;
        }
        if (this.StreamContent !== ct.StreamContent && ct.StreamContent !== null) {
            this.StreamContent = ct.StreamContent;
        }
        if (this.Title !== ct.Title && ct.Title !== null) {
            SonosLog("CurrenTRack: Title vs Neues Title: " + this.Title + " vs " + ct.Title);
            this.Title = ct.Title;
            haschanged = true;
        }
        if (this.Uri !== ct.Uri && ct.Uri !== null) {
            this.Uri = ct.Uri;
            haschanged = true;
        }
        //MP3 Verarbeitung
        if (typeof ct.MP3 !== "undefined" && ct.MP3 !== null) {
            //Daten MP3 Anpassen
            if ((this.MP3.Album === "leer" || this.MP3.Album === null) && this.Album !== "leer") {
                this.MP3.Album = this.Album;
            }
            if ((this.MP3.Title === "leer" || this.MP3.Title === null) && this.Title !== "leer") {
                this.MP3.Title = this.Title;
            }
            if ((this.MP3.Artist === "leer" || this.MP3.Artist === null) && this.Artist !== "leer") {
                this.MP3.Artist = this.Artist;
            }
            //CurrentTrack durch MP3 Anpassen
            if (this.Artist === "leer" && ct.MP3.Artist !== null && ct.MP3.Artist !== "leer") {
                this.Artist = ct.MP3.Artist;
            }
            if (this.Album === "leer" && ct.MP3.Album !== null && ct.MP3.Album !== "leer") {
                this.Album = ct.MP3.Album;
            }
            if (this.Title === "leer" && ct.MP3.Titel !== null && ct.MP3.Titel !== "leer") {
                this.Title = ct.MP3.Titel;
            }
            //Daten an MP3 Uebergeben
            mp3change = this.MP3.SetMP3(ct.MP3);

        } else {
            //Daten für MP3 Sind null und nun wenn ein anderer Song diese Resetten.
            if (this.MP3.Title !== this.Title || this.MP3.Album !== this.Album) {
                this.MP3 = new MP3();
                SoDo.bewertungWidth.width(this.MP3.Bewertung + "%");
            }
        }
        if (mp3change === false && haschanged === false) {
            return false;
        } else {
            return true;
        }
    };
    this.RenderCurrentTrack = function(tracknumber) {
        //Wenn kein Stream MP3 Rendern ansonsten Stream
        if (typeof tracknumber === "undefined") {
            tracknumber = 0;
        }
        if (this.Stream === false) {
            SonosLog("SonosZone:CurrentTrack:RenderCurrentTRack: Kein Stream");
            this.MP3.RenderMP3(false);
            //In Playlist Daten hinterlegen
            if (tracknumber > 0) {
                var plid = $("#Currentplaylist_" + (tracknumber - 1));
                if (plid.length > 0 && !plid.hasClass("aktsonginplaylist")) {
                    SetCurrentPlaylistSong(tracknumber, "RenderCurrentTrack");
                }
            }
            //Rating list verarbeiten und evtl. Ausblenden
            if (SoVa.ratingonlycurrent === true && SoDo.ratingListBox.is(":visible") === true) {
                ShowCurrentRating("show");
            }
            if (SoVa.ratingonlycurrent === false && SoDo.ratingListBox.is(":visible") === true) {
                ShowCurrentRating("hide");
            }
            //Sonstige Elemente Einblenden
            if (SoDo.runtimeSlider.is(":hidden")) {
                SoDo.runtimeSlider.show();
            }
        } else {
            //Stream
            //Elemente ausblenden
            if (!this.StreamContent === "Apple") {
                if (SoDo.bewertungWidth.is(":visible")) {
                    SoDo.bewertungWidth.hide();
                }
            } else {
                if (SoDo.bewertungWidth.is(":hidden")) {
                    SoDo.bewertungWidth.show();
                }

            }
            if (!this.StreamContent === "Dienst" && !this.StreamContent === "Apple") {
                if (SoDo.runtimeSlider.is(":visible")) {
                    SoDo.runtimeSlider.hide();
                }
            } else {
                if (SoDo.runtimeSlider.is(":hidden")) {
                    SoDo.runtimeSlider.show();
                }
            }
            if (typeof this.StreamContent !== "undefined" && !this.StreamContent === "Dienst" && !this.StreamContent === "Apple") {
                if (SoDo.aktTitle.text() !== this.StreamContent) {
                    SoDo.aktTitle.text(this.StreamContent);
                }
                if (SoDo.aktArtist.text() !== "") {
                    SoDo.aktArtist.text("");
                }
            } else {
                if (this.Title !== "leer") {
                    if (SoDo.aktTitle.text() !== this.Title) {
                        SoDo.aktTitle.text(this.Title);
                    }
                } else {
                    if (SoDo.aktTitle.text() !== "") {
                        SoDo.aktTitle.text("");
                    }
                }
                if (this.Artist !== "leer") {
                    if (SoDo.aktArtist.text() !== this.Artist) {
                        SoDo.aktArtist.text(this.Artist);
                    }
                } else {
                    if (SoDo.aktArtist.text() !== "") {
                        SoDo.aktArtist.text("");
                    }
                }
                if (this.StreamContent === "Apple") {
                    this.MP3.RenderMP3(true);
                }
            }

        }
        //Unabhängig vom Stream
        //AlbumCover
        if (this.AlbumArtURI !== "leer") {
            if (SoDo.cover.attr("src") !== "http://" + this.BaseURL + this.AlbumArtURI) {
                SoDo.cover.attr("src", "http://" + this.BaseURL + this.AlbumArtURI);
                UpdateImageOnErrors();
            }
        } else {
            if (SoDo.cover.attr("src") !== SoVa.nocoverpfad) {
                SoDo.cover.attr("src", SoVa.nocoverpfad);
            }
        }
    };

}
function MP3() {
    this.Album = "leer";
    this.Artist = "leer";
    this.ArtistPlaylist = false;
    this.Aufwecken = false;
    this.Bewertung = 0;
    this.BewertungMine = 0;
    this.Gelegenheit = "None";
    this.Genre = "leer";
    this.Geschwindigkeit = "None";
    this.HatCover = false;
    this.Jahr = 0;
    this.Komponist = "leer";
    this.Kommentar = "leer";
    this.Laufzeit = "leer";
    this.Lyric = "leer";
    this.Pfad = "leer";
    this.Stimmung = "None";
    this.Title = "leer";
    this.Tracknumber = 0;
    this.Typ = "leer";
    this.VerarbeitungsFehler = false;
    this.Verlag = "leer";

    this.SetMP3 = function(mp3) {
        var haschanged = false; //Wenn Änderungen gemacht wurden true zurück, damit neu gerendert werden kann.
        if (typeof mp3 === "undefined" || mp3 === null) {
            return haschanged;
        }
        if (this.Album !== mp3.Album && mp3.Album !== null) {
            this.Album = mp3.Album;
            haschanged = true;
        }
        if (this.Artist !== mp3.Artist && mp3.Artist !== null) {
            this.Artist = mp3.Artist;
            haschanged = true;
        }
        if (this.ArtistPlaylist !== mp3.ArtistPlaylist && mp3.ArtistPlaylist !== null) {
            this.ArtistPlaylist = mp3.ArtistPlaylist;
            //haschanged = true;
        }
        if (this.Aufwecken !== mp3.Aufwecken && mp3.Aufwecken !== null) {
            this.Aufwecken = mp3.Aufwecken;
            //haschanged = true;
        }
        if (this.Bewertung !== parseInt(mp3.Bewertung) && mp3.Bewertung !== null) {
            this.Bewertung = parseInt(mp3.Bewertung);
            haschanged = true;
        }
        if (this.BewertungMine !== parseInt(mp3.BewertungMine) && mp3.BewertungMine !== null) {
            this.BewertungMine = parseInt(mp3.BewertungMine);
            haschanged = true;
        }
        if (this.Gelegenheit !== mp3.Gelegenheit && mp3.Gelegenheit !== null) {
            this.Gelegenheit = mp3.Gelegenheit;
            //haschanged = true;
        }
        if (this.Geschwindigkeit !== mp3.Geschwindigkeit && mp3.Geschwindigkeit !== null) {
            this.Geschwindigkeit = mp3.Geschwindigkeit;
            //haschanged = true;
        }
        if (this.Genre !== mp3.Genre && mp3.Genre !== null) {
            this.Genre = mp3.Genre;
            haschanged = true;
        }
        if (this.HatCover !== mp3.HatCover && mp3.HatCover !== null) {
            this.HatCover = mp3.HatCover;
            haschanged = true;
        }
        if (this.Jahr !== mp3.Jahr && mp3.Jahr !== null) {
            this.Jahr = mp3.Jahr;
            //haschanged = true;
        }
        if (this.Komponist !== mp3.Komponist && mp3.Komponist !== null) {
            this.Komponist = mp3.Komponist;
            //haschanged = true;
        }
        if (this.Kommentar !== mp3.Kommentar && mp3.Kommentar !== null) {
            this.Kommentar = mp3.Kommentar;
            //haschanged = true;
        }
        if (this.Laufzeit !== mp3.Laufzeit && mp3.Laufzeit !== null) {
            this.Laufzeit = mp3.Laufzeit;
            //haschanged = true;
        }
        if (this.Lyric !== mp3.Lyric && mp3.Lyric !== null) {
            this.Lyric = mp3.Lyric;
            haschanged = true;
        }
        if (this.Pfad !== mp3.Pfad && mp3.Pfad !== null) {
            this.Pfad = mp3.Pfad;
            //haschanged = true;
        }
        if (this.Stimmung !== mp3.Stimmung && mp3.Stimmung !== null) {
            this.Stimmung = mp3.Stimmung;
            //haschanged = true;
        }
        if (this.Title !== mp3.Titel && mp3.Titel !== null) {
            this.Title = mp3.Titel;
            haschanged = true;
        }
        if (this.Tracknumber !== parseInt(mp3.Tracknumber) && mp3.Tracknumber !== null) {
            this.Tracknumber = parseInt(mp3.Tracknumber);
            //haschanged = true;
        }
        if (this.Typ !== mp3.Typ && mp3.Typ !== null) {
            this.Typ = mp3.Typ;
            //haschanged = true;
        }
        if (this.VerarbeitungsFehler !== mp3.VerarbeitungsFehler && mp3.VerarbeitungsFehler !== null) {
            this.VerarbeitungsFehler = mp3.VerarbeitungsFehler;
            //haschanged = true;
        }
        if (this.Verlag !== mp3.Verlag && mp3.Verlag !== null) {
            this.Verlag = mp3.Verlag;
            //haschanged = true;
        }
        return haschanged;
    };
    this.RenderMP3 = function(changeafterrating) {
        if (typeof changeafterrating === "undefined") {
            changeafterrating = false;
        }
        //Nach einem Rating bestimmte Dinge nicht neu Rendern
        if (changeafterrating === false) {
            //Lyric Darstellen
            this.RenderLyric();
        }
        //Bewertung
        if (SoDo.bewertungWidth.is(":hidden")) {
            SoDo.bewertungWidth.show();
        }
        SoDo.bewertungWidth.width(this.Bewertung + "%");
        if (parseInt(this.Bewertung) === -1) {
            if (SoDo.currentBomb.is(":hidden")) {
                SoDo.currentBomb.show();
            }
        } else {
            if (SoDo.currentBomb.is(":visible")) {
                SoDo.currentBomb.hide();
            }
        }
        if (this.Title !== "leer") {
            if (SoDo.aktTitle.text() !== this.Title) {
                SoDo.aktTitle.text(this.Title);
            }
        } else {
            if (SoDo.aktTitle.text() !== "") {
                SoDo.aktTitle.text("");
            }
        }
        if (this.Artist !== "leer") {
            if (SoDo.aktArtist.text() !== this.Artist) {
                SoDo.aktArtist.text(this.Artist);
            }
        } else {
            if (SoDo.aktArtist.text() !== "") {
                SoDo.aktArtist.text("");
            }
        }
    };
    this.RenderLyric = function() {
        SoDo.lyricWrapper.children().remove();
        if (this.Lyric !== "leer") {
            $('<div>' + this.Lyric + '</div>').appendTo(SoDo.lyricWrapper);
        } else {
            $('<div>No Lyrics in Song</div>').appendTo(SoDo.lyricWrapper);
        }
    };
}
