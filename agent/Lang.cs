namespace AQT
{
    // Idioma del trainer (menu, descripciones y mensajes). Los nombres de habilidades / estados de los personajes
    // vienen del propio juego y siguen el idioma que tengas elegido en el juego.
    // L.T(es, en): el texto en espanol e ingles va en el codigo; los otros idiomas se buscan en Translations por el texto en ingles.
    internal static class L
    {
        public const int Es = 0;
        public const int En = 1;
        public const int Fr = 2;
        public const int Pt = 3;
        public const int Ru = 4;
        public const int De = 5;
        public const int Count = 6;

        // codigos que se guardan en settings.ini y nombres propios de cada idioma
        public static readonly string[] Codes = new string[] { "es", "en", "fr", "pt", "ru", "de" };
        public static readonly string[] Names = new string[] { "Español", "English", "Français", "Português", "Русский", "Deutsch" };

        public static int Lang;

        public static string T(string es, string en)
        {
            if (Lang == Es) return es;
            if (Lang == En || string.IsNullOrEmpty(en)) return en;
            return Translations.Get(en, Lang);
        }

        public static int FromCode(string code)
        {
            for (int i = 0; i < Codes.Length; i++)
                if (Codes[i] == code) return i;
            return Es;
        }
    }
}
