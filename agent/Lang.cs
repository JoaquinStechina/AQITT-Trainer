namespace AQT
{
    // Trainer language (menu, descriptions and messages). Skill / status names of the characters
    // come from the game itself and follow the language chosen in the game.
    // L.T(es, en): the Spanish and English text live in the code; other languages are looked up in Translations by the English text.
    internal static class L
    {
        public const int Es = 0;
        public const int En = 1;
        public const int Fr = 2;
        public const int Pt = 3;
        public const int Ru = 4;
        public const int De = 5;
        public const int Count = 6;

        // codes stored in settings.ini and the native name of each language
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
