using System;
using System.Collections.Generic;

namespace AQT
{
    // A cheat of the "Cheats" tab: stable identifier (stored in the settings), section, texts in both languages.
    internal sealed class Cheat
    {
        public string Id;
        public int Section;
        public string TitleEs, TitleEn, DescEs, DescEn;
        public bool MissionOnly;   // mostly useful inside a mission
        public bool On;

        public string Title { get { return L.T(TitleEs, TitleEn); } }
        public string Desc { get { return L.T(DescEs, DescEn); } }
    }

    internal sealed class Preset
    {
        public string NameEs, NameEn;
        public string[] Ids;       // null = all
        public string Name { get { return L.T(NameEs, NameEn); } }
    }

    // Cheat table. The ids are the ones TrainerBehaviour.Tick() uses to know what to apply.
    internal static class Cheats
    {
        public const int SectionCount = 5;

        public static readonly string[] SectionIds = new string[] { "equip", "body", "mind", "social", "actions" };

        public static string SectionTitle(int s)
        {
            switch (s)
            {
                case 0: return L.T("Equipo (lo que lleva cada soldado)", "Equipment (what each soldier carries)");
                case 1: return L.T("Cuerpo", "Body");
                case 2: return L.T("Mente", "Mind");
                case 3: return L.T("Circunstancias y relaciones", "Circumstances and relationships");
                default: return L.T("Acciones", "Actions");
            }
        }

        public static readonly List<Cheat> All = new List<Cheat>();
        private static readonly Dictionary<string, Cheat> ById = new Dictionary<string, Cheat>();

        public static readonly Preset[] Presets = new Preset[]
        {
            new Preset { NameEs = "Supervivencia", NameEn = "Survival",
                Ids = new string[] { "invWound", "invSurrender", "health", "noHunger", "noTired" } },
            new Preset { NameEs = "Cómodo", NameEn = "Comfortable",
                Ids = new string[] { "ammo", "firstAid", "health", "noHunger", "noTired", "noStress", "happy" } },
            new Preset { NameEs = "Todo", NameEn = "Everything", Ids = null },
            new Preset { NameEs = "Ninguno", NameEn = "None", Ids = new string[0] }
        };

        static Cheats()
        {
            Add("ammo", 0, true,
                "Munición infinita", "Infinite ammo",
                "Recarga la munición de cada soldado de tu bando.",
                "Refills the ammunition of every soldier on your side.");
            Add("firstAid", 0, true,
                "Vendas infinitas", "Infinite bandages",
                "Mantiene lleno el stock de vendas de cada soldado. Que la cura salga bien es otra cosa: ver \"Acciones siempre exitosas\".",
                "Keeps every soldier's bandage stock full. Whether the treatment succeeds is a different thing: see \"Actions always succeed\".");
            Add("handGrenades", 0, true,
                "Granadas de mano infinitas", "Infinite hand grenades", "", "");
            Add("smokeGrenades", 0, true,
                "Granadas de humo infinitas", "Infinite smoke grenades", "", "");

            Add("noHunger", 1, false,
                "Sin hambre", "No hunger",
                "Raciones de cada soldado al máximo y provisiones de la compañía y de la trinchera llenas (campaña).",
                "Each soldier's rations at maximum and the company and trench provisions full (campaign).");
            Add("health", 1, false,
                "Buena salud (cura)", "Good health (heals)",
                "Cura lo que ya tienen: quita heridas, sangrado, enfermedades y complicaciones (erupción de piel, tos, náuseas, dolor...), los deja \"sanos\" y baja las chances de enfermar. " +
                "No evita heridas nuevas: para eso están los invulnerables.",
                "Heals what they already have: removes wounds, bleeding, diseases and complications (skin rash, cough, nausea, pain...), sets them to \"healthy\" and lowers the chance of falling ill. " +
                "It does not prevent new wounds: that is what invulnerable is for.");
            Add("invWound", 1, true,
                "Soldados invulnerables (previene)", "Invulnerable soldiers (prevents)",
                "Atributo del juego que impide recibir heridas nuevas. No cura las que ya tienen: para eso está \"Buena salud\".",
                "A game attribute that prevents new wounds. It does not heal existing ones: that is what \"Good health\" does.");
            Add("noTired", 1, false,
                "Sin cansancio", "No fatigue",
                "Esfuerzo físico (misión) y agotamiento (campaña) siempre en 0. El agotamiento sube con el estrés y el esfuerzo, y con esfuerzo 60 o más el soldado queda sin aliento.",
                "Physical exertion (mission) and exhaustion (campaign) always at 0. Exhaustion grows with stress and exertion, and at exertion 60 or more the soldier gets breathless.");
            Add("fitness", 1, false,
                "Buena condición física", "Good physical condition",
                "Forma física al máximo: se cansan y sangran menos. Con \"Sin cansancio\" y \"Buena salud\" activos casi no suma nada.",
                "Fitness at maximum: they tire and bleed less. With \"No fatigue\" and \"Good health\" active it adds almost nothing.");

            Add("happy", 2, false,
                "Felices (moral y optimismo)", "Happy (morale and optimism)",
                "Moral al máximo y optimismo al máximo. El miedo al comandante está en \"Confían en vos\".",
                "Morale and optimism at maximum. Fear of the commander is handled by \"They trust you\".");
            Add("noStress", 2, true,
                "Sin estrés", "No stress",
                "Estrés siempre en 0 (misión). El estrés alimenta el agotamiento y entra en la munición que gasta cada acción.",
                "Stress always at 0 (mission). Stress feeds exhaustion and is part of the ammunition each action uses.");
            Add("fervor", 2, true,
                "Exaltados", "Fervent",
                "Fervor al máximo y estado \"Fervor\" puesto (misión). Normalmente sube con los aciertos en combate; con el estado puesto cada acción cansa menos.",
                "Fervor at maximum and the \"Fervor\" state applied (mission). It normally rises with successes in combat; with the state applied every action is less tiring.");
            Add("invSurrender", 2, true,
                "Soldados que nunca se rinden", "Soldiers never surrender",
                "Atributo del juego: no se rinden en combate.",
                "A game attribute: they do not surrender in combat.");

            Add("circ", 3, false,
                "Circunstancias buenas", "Good circumstances",
                "Etiquetas de la ficha del soldado: pone buen clima y buen humor (determinado, agradecido, aliviado, motivado, buenas noticias) y quita duelo, malas noticias, mal humor y piojos. " +
                "No toca los números de moral: eso es \"Felices\".",
                "Tags on the soldier's sheet: sets good weather and good mood (determined, grateful, relieved, motivated, good news) and removes grief, bad news, bad mood and lice. " +
                "It does not touch the morale numbers: that is \"Happy\".");
            Add("relPlayer", 3, false,
                "Confían en vos", "They trust you",
                "Confianza en vos al máximo, miedo al comandante en 0 y trato de camaradería (soldados de tu tropa).",
                "Trust in you at maximum, fear of the commander at 0 and a comradely standing (soldiers of your troop).");
            Add("relGroups", 3, false,
                "Hospital, intendente y aldea", "Hospital, quartermaster and village",
                "Confianza global con los tres al máximo (campaña).",
                "Global trust with all three at maximum (campaign).");
            Add("relFriends", 3, false,
                "Rivales pasan a ser amigos", "Rivals become friends",
                "Entre tus propios soldados: las rivalidades se convierten en amistad.",
                "Among your own soldiers: rivalries turn into friendships.");

            Add("success", 4, true,
                "Acciones siempre exitosas", "Actions always succeed",
                "Sube la eficiencia solo de tus soldados (disparos, granadas, cuerpo a cuerpo, alambre de púas, fusil trabado, primeros auxilios, saqueo) para que las tiradas pasen. Al desactivarlo se devuelve todo.",
                "Raises the efficiency of your soldiers only (shooting, grenades, melee, barbed wire, jammed rifle, first aid, looting) so the rolls pass. Everything is given back when you turn it off.");
        }

        private static void Add(string id, int section, bool missionOnly, string titleEs, string titleEn, string descEs, string descEn)
        {
            Cheat c = new Cheat();
            c.Id = id;
            c.Section = section;
            c.MissionOnly = missionOnly;
            c.TitleEs = titleEs;
            c.TitleEn = titleEn;
            c.DescEs = descEs;
            c.DescEn = descEn;
            All.Add(c);
            ById[id] = c;
        }

        public static Cheat Get(string id)
        {
            Cheat c;
            return ById.TryGetValue(id, out c) ? c : null;
        }

        public static int ActiveCount()
        {
            int n = 0;
            for (int i = 0; i < All.Count; i++) if (All[i].On) n++;
            return n;
        }

        public static List<string> ActiveIds()
        {
            List<string> r = new List<string>();
            for (int i = 0; i < All.Count; i++) if (All[i].On) r.Add(All[i].Id);
            return r;
        }

        // Leaves exactly the cheats in the list active (null = all).
        public static void SetActive(IEnumerable<string> ids)
        {
            HashSet<string> set = ids == null ? null : new HashSet<string>(ids);
            for (int i = 0; i < All.Count; i++)
                All[i].On = set == null || set.Contains(All[i].Id);
        }

        public static bool Matches(Cheat c, string lowerQuery)
        {
            if (lowerQuery.Length == 0) return true;
            string hay = (c.TitleEs + " " + c.TitleEn + " " + c.DescEs + " " + c.DescEn + " " + c.Title + " " + c.Desc).ToLowerInvariant();
            return hay.IndexOf(lowerQuery, StringComparison.Ordinal) >= 0;
        }
    }
}
