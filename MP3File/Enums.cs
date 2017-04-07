namespace MP3File
{
    public static class Enums
    {
        /// <summary>
        /// Mp3 Rating Stimmung
        /// </summary>
        public enum Gelegenheit
        {
            None,
            Party,
            Hintergrund,
            Romantisch,
            Saisonal,
            unset
        }
        /// <summary>
        /// MP3 Rating Geschwindigkeiten
        /// </summary>
        public enum Geschwindigkeit
        {
            None,
            Sehr_Langsam,
            Langsam,
            Moderat,
            Schnell,
            Sehr_Schnell,
            unset
        }
        /// <summary>
        /// MP3 Rating Gelegenheiten
        /// </summary>
        public enum Stimmung
        {
            None,
            Wild,
            Fröhlich,
            Entspannt,
            Düster,
            Einschläfernd,
            unset
        }
    }
}
