using System;
using System.Collections.Generic;

namespace AQT
{
    // Traducciones al frances, portugues, ruso y aleman. La clave es el texto en INGLES que se pasa a L.T(es, en);
    // si falta una clave se muestra el ingles. Orden de los valores: fr, pt, ru, de.
    internal static class Translations
    {
        private static readonly Dictionary<string, string[]> Map = new Dictionary<string, string[]>();

        // lang: L.Fr .. L.De
        public static string Get(string en, int lang)
        {
            string[] v;
            if (Map.TryGetValue(en, out v))
            {
                int i = lang - L.Fr;
                if (i >= 0 && i < v.Length && v[i] != null) return v[i];
            }
            return en;
        }

        private static void A(string en, string fr, string pt, string ru, string de)
        {
            Map[en] = new string[] { fr, pt, ru, de };
        }

        static Translations()
        {
            A("Skills",
              "Compétences", "Habilidades",
              "Навыки", "Fähigkeiten");
            A("Status",
              "État", "Estado",
              "Состояние", "Zustand");
            A("Circumst.",
              "Circonst.", "Circunst.",
              "Обстоят.", "Umstände");
            A("Relation",
              "Relation", "Relação",
              "Отношения", "Beziehung");
            A("Favorites",
              "Favoris", "Favoritos",
              "Избранное", "Favoriten");
            A("No player characters loaded yet (open or load a game).",
              "Aucun personnage du joueur n'est encore chargé (ouvrez ou chargez une partie).", "Ainda não há personagens do jogador carregados (abra ou carregue um jogo).",
              "Персонажи игрока ещё не загружены (откройте или загрузите игру).", "Noch keine Spielerfiguren geladen (Spiel starten oder laden).");
            A("Character:",
              "Personnage :", "Personagem:",
              "Персонаж:", "Figur:");
            A("Only the ones it has",
              "Seulement ceux qu'il possède", "Só os que ele tem",
              "Только имеющиеся", "Nur vorhandene");
            A("Apply to the WHOLE troop (",
              "Appliquer à TOUTE la troupe (", "Aplicar a TODA a tropa (",
              "Применить ко ВСЕМУ отряду (", "Auf die GANZE Truppe anwenden (");
            A("Search:",
              "Recherche :", "Buscar:",
              "Поиск:", "Suche:");
            A("Has: ",
              "Possède : ", "Tem: ",
              "Есть: ", "Hat: ");
            A("Undo: ",
              "Annuler : ", "Desfazer: ",
              "Отменить: ", "Rückgängig: ");
            A("Undo (nothing to undo)",
              "Annuler (rien à annuler)", "Desfazer (nada para desfazer)",
              "Отменить (нечего отменять)", "Rückgängig (nichts zum Rückgängigmachen)");
            A("Note: items marked (auto) are levels the game recalculates by itself; to really change them use the Cheats / Values tab.",
              "Remarque : les éléments marqués (auto) sont des niveaux que le jeu recalcule lui-même ; pour les modifier vraiment, utilisez l'onglet Cheats / Valeurs.", "Obs.: os itens marcados (auto) são níveis que o jogo recalcula sozinho; para mudá-los de verdade use a aba Truques / Valores.",
              "Примечание: элементы с пометкой (auto) — это уровни, которые игра пересчитывает сама; чтобы изменить их по-настоящему, используйте вкладку Читы / Значения.", "Hinweis: Einträge mit (auto) sind Stufen, die das Spiel selbst neu berechnet; um sie wirklich zu ändern, nutze den Reiter Cheats / Werte.");
            A("No favorites yet: press \"Fav\" on any attribute.",
              "Pas encore de favoris : appuyez sur « Fav » sur un attribut.", "Ainda sem favoritos: toque em \"Fav\" em qualquer atributo.",
              "Избранного пока нет: нажмите «Fav» у любого атрибута.", "Noch keine Favoriten: „Fav“ bei einem Attribut drücken.");
            A("Add",
              "Ajouter", "Adicionar",
              "Добавить", "Hinzufügen");
            A("Remove",
              "Retirer", "Remover",
              "Убрать", "Entfernen");
            A("added",
              "ajouté", "adicionado",
              "добавлено", "hinzugefügt");
            A("removed",
              "retiré", "removido",
              "убрано", "entfernt");
            A(" already has '",
              " a déjà '", " já tem '",
              " уже имеет '", " hat bereits '");
            A(" does not have '",
              " n'a pas '", " não tem '",
              " не имеет '", " hat nicht '");
            A(" on ",
              " sur ", " em ",
              " у ", " bei ");
            A(" soldiers",
              " soldats", " soldados",
              " солдат", " Soldaten");
            A("Undone: ",
              "Annulé : ", "Desfeito: ",
              "Отменено: ", "Rückgängig gemacht: ");
            A("Equipment (what each soldier carries)",
              "Équipement (ce que porte chaque soldat)", "Equipamento (o que cada soldado carrega)",
              "Снаряжение (что несёт каждый солдат)", "Ausrüstung (was jeder Soldat trägt)");
            A("Body",
              "Corps", "Corpo",
              "Тело", "Körper");
            A("Mind",
              "Esprit", "Mente",
              "Разум", "Geist");
            A("Circumstances and relationships",
              "Circonstances et relations", "Circunstâncias e relações",
              "Обстоятельства и отношения", "Umstände und Beziehungen");
            A("Actions",
              "Actions", "Ações",
              "Действия", "Aktionen");
            A("Infinite ammo",
              "Munitions infinies", "Munição infinita",
              "Бесконечные патроны", "Unendlich Munition");
            A("Refills the ammunition of every soldier on your side.",
              "Recharge les munitions de chaque soldat de votre camp.", "Recarrega a munição de cada soldado do seu lado.",
              "Пополняет боезапас каждого солдата вашей стороны.", "Füllt die Munition jedes Soldaten auf deiner Seite auf.");
            A("Infinite bandages",
              "Bandages infinis", "Bandagens infinitas",
              "Бесконечные бинты", "Unendlich Verbände");
            A("Keeps every soldier's bandage stock full. Whether the treatment succeeds is a different thing: see \"Actions always succeed\".",
              "Garde plein le stock de bandages de chaque soldat. Que le soin réussisse est autre chose : voir « Actions toujours réussies ».", "Mantém cheio o estoque de bandagens de cada soldado. Se o tratamento dá certo é outra coisa: veja \"Ações sempre bem-sucedidas\".",
              "Держит запас бинтов каждого солдата полным. Удастся ли перевязка — другой вопрос: см. «Действия всегда успешны».", "Hält den Verbandsvorrat jedes Soldaten voll. Ob die Behandlung gelingt, ist etwas anderes: siehe „Aktionen gelingen immer“.");
            A("Infinite hand grenades",
              "Grenades à main infinies", "Granadas de mão infinitas",
              "Бесконечные ручные гранаты", "Unendlich Handgranaten");
            A("Infinite smoke grenades",
              "Grenades fumigènes infinies", "Granadas de fumaça infinitas",
              "Бесконечные дымовые гранаты", "Unendlich Rauchgranaten");
            A("No hunger",
              "Pas de faim", "Sem fome",
              "Без голода", "Kein Hunger");
            A("Each soldier's rations at maximum and the company and trench provisions full (campaign).",
              "Rations de chaque soldat au maximum et provisions de la compagnie et de la tranchée pleines (campagne).", "Rações de cada soldado no máximo e provisões da companhia e da trincheira cheias (campanha).",
              "Рационы каждого солдата на максимуме, запасы роты и окопа полные (кампания).", "Rationen jedes Soldaten auf Maximum, Vorräte von Kompanie und Graben voll (Kampagne).");
            A("Good health (heals)",
              "Bonne santé (soigne)", "Boa saúde (cura)",
              "Хорошее здоровье (лечит)", "Gute Gesundheit (heilt)");
            A("Heals what they already have: removes wounds, bleeding, diseases and complications (skin rash, cough, nausea, pain...), sets them to \"healthy\" and lowers the chance of falling ill. It does not prevent new wounds: that is what invulnerable is for.",
              "Soigne ce qu'ils ont déjà : retire blessures, saignements, maladies et complications (éruption cutanée, toux, nausées, douleur...), les met en « sain » et réduit le risque de tomber malade. N'empêche pas de nouvelles blessures : c'est le rôle d'« invulnérables ».", "Cura o que já têm: remove ferimentos, sangramento, doenças e complicações (erupção na pele, tosse, náusea, dor...), deixa-os \"saudáveis\" e reduz a chance de adoecer. Não evita novos ferimentos: para isso servem os invulneráveis.",
              "Лечит то, что уже есть: убирает раны, кровотечение, болезни и осложнения (сыпь, кашель, тошноту, боль...), ставит состояние «здоров» и снижает шанс заболеть. Новые ранения не предотвращает — для этого есть «неуязвимые».", "Heilt, was sie schon haben: entfernt Wunden, Blutungen, Krankheiten und Komplikationen (Hautausschlag, Husten, Übelkeit, Schmerzen ...), setzt sie auf „gesund“ und senkt das Erkrankungsrisiko. Verhindert keine neuen Wunden – dafür gibt es „Unverwundbar“.");
            A("Invulnerable soldiers (prevents)",
              "Soldats invulnérables (prévient)", "Soldados invulneráveis (previne)",
              "Неуязвимые солдаты (предотвращает)", "Unverwundbare Soldaten (verhindert)");
            A("A game attribute that prevents new wounds. It does not heal existing ones: that is what \"Good health\" does.",
              "Un attribut du jeu qui empêche les nouvelles blessures. Ne soigne pas les existantes : c'est le rôle de « Bonne santé ».", "Um atributo do jogo que impede novos ferimentos. Não cura os que já existem: isso é a \"Boa saúde\".",
              "Атрибут игры, не дающий получать новые ранения. Не лечит уже имеющиеся — для этого «Хорошее здоровье».", "Ein Spielattribut, das neue Wunden verhindert. Heilt keine bestehenden – das macht „Gute Gesundheit“.");
            A("No fatigue",
              "Pas de fatigue", "Sem cansaço",
              "Без усталости", "Keine Müdigkeit");
            A("Physical exertion (mission) and exhaustion (campaign) always at 0. Exhaustion grows with stress and exertion, and at exertion 60 or more the soldier gets breathless.",
              "Effort physique (mission) et épuisement (campagne) toujours à 0. L'épuisement monte avec le stress et l'effort, et à partir de 60 d'effort le soldat est essoufflé.", "Esforço físico (missão) e exaustão (campanha) sempre em 0. A exaustão sobe com o estresse e o esforço, e com esforço 60 ou mais o soldado fica sem fôlego.",
              "Физическая нагрузка (миссия) и истощение (кампания) всегда 0. Истощение растёт от стресса и нагрузки, а при нагрузке 60 и выше солдат задыхается.", "Körperliche Anstrengung (Mission) und Erschöpfung (Kampagne) immer 0. Erschöpfung steigt mit Stress und Anstrengung; ab Anstrengung 60 ist der Soldat außer Atem.");
            A("Good physical condition",
              "Bonne condition physique", "Boa condição física",
              "Хорошая физическая форма", "Gute körperliche Verfassung");
            A("Fitness at maximum: they tire and bleed less. With \"No fatigue\" and \"Good health\" active it adds almost nothing.",
              "Forme physique au maximum : ils se fatiguent et saignent moins. Avec « Pas de fatigue » et « Bonne santé » actifs, cela n'apporte presque rien.", "Forma física no máximo: cansam e sangram menos. Com \"Sem cansaço\" e \"Boa saúde\" ativos quase não acrescenta nada.",
              "Физическая форма на максимуме: меньше устают и меньше кровоточат. При включённых «Без усталости» и «Хорошее здоровье» почти ничего не добавляет.", "Fitness auf Maximum: Sie ermüden und bluten weniger. Mit „Keine Müdigkeit“ und „Gute Gesundheit“ bringt es kaum noch etwas.");
            A("Happy (morale and optimism)",
              "Heureux (moral et optimisme)", "Felizes (moral e otimismo)",
              "Довольные (мораль и оптимизм)", "Glücklich (Moral und Optimismus)");
            A("Morale and optimism at maximum. Fear of the commander is handled by \"They trust you\".",
              "Moral et optimisme au maximum. La peur du commandant relève de « Ils vous font confiance ».", "Moral e otimismo no máximo. O medo do comandante fica em \"Eles confiam em você\".",
              "Мораль и оптимизм на максимуме. Страх перед командиром — в пункте «Они вам доверяют».", "Moral und Optimismus auf Maximum. Die Angst vor dem Kommandanten regelt „Sie vertrauen dir“.");
            A("No stress",
              "Pas de stress", "Sem estresse",
              "Без стресса", "Kein Stress");
            A("Stress always at 0 (mission). Stress feeds exhaustion and is part of the ammunition each action uses.",
              "Stress toujours à 0 (mission). Le stress alimente l'épuisement et entre dans le calcul des munitions que consomme chaque action.", "Estresse sempre em 0 (missão). O estresse alimenta a exaustão e entra na munição que cada ação gasta.",
              "Стресс всегда 0 (миссия). Стресс питает истощение и входит в расход патронов на каждое действие.", "Stress immer 0 (Mission). Stress treibt die Erschöpfung an und fließt in die Munition ein, die jede Aktion verbraucht.");
            A("Fervent",
              "Exaltés", "Exaltados",
              "Воодушевлённые", "Begeistert");
            A("Fervor at maximum and the \"Fervor\" state applied (mission). It normally rises with successes in combat; with the state applied every action is less tiring.",
              "Ferveur au maximum et état « Ferveur » appliqué (mission). Elle monte normalement avec les succès au combat ; avec l'état appliqué, chaque action fatigue moins.", "Fervor no máximo e estado \"Fervor\" aplicado (missão). Normalmente sobe com os acertos em combate; com o estado aplicado cada ação cansa menos.",
              "Рвение на максимуме и применено состояние «Рвение» (миссия). Обычно оно растёт от успехов в бою; с этим состоянием каждое действие меньше утомляет.", "Eifer auf Maximum und Zustand „Eifer“ gesetzt (Mission). Er steigt normalerweise mit Kampferfolgen; mit dem Zustand ermüdet jede Aktion weniger.");
            A("Soldiers never surrender",
              "Soldats qui ne se rendent jamais", "Soldados que nunca se rendem",
              "Солдаты никогда не сдаются", "Soldaten ergeben sich nie");
            A("A game attribute: they do not surrender in combat.",
              "Un attribut du jeu : ils ne se rendent pas au combat.", "Um atributo do jogo: não se rendem em combate.",
              "Атрибут игры: в бою они не сдаются.", "Ein Spielattribut: Sie ergeben sich im Kampf nicht.");
            A("Good circumstances",
              "Bonnes circonstances", "Boas circunstâncias",
              "Хорошие обстоятельства", "Gute Umstände");
            A("Tags on the soldier's sheet: sets good weather and good mood (determined, grateful, relieved, motivated, good news) and removes grief, bad news, bad mood and lice. It does not touch the morale numbers: that is \"Happy\".",
              "Étiquettes de la fiche du soldat : met du beau temps et une bonne humeur (déterminé, reconnaissant, soulagé, motivé, bonnes nouvelles) et retire deuil, mauvaises nouvelles, mauvaise humeur et poux. Ne touche pas aux chiffres de moral : c'est « Heureux ».", "Etiquetas da ficha do soldado: coloca bom clima e bom humor (determinado, grato, aliviado, motivado, boas notícias) e remove luto, más notícias, mau humor e piolhos. Não mexe nos números de moral: isso é \"Felizes\".",
              "Метки в карточке солдата: ставит хорошую погоду и настроение (решительный, благодарный, с облегчением, мотивированный, хорошие новости) и убирает скорбь, плохие новости, дурное настроение и вшей. Числа морали не трогает — это «Довольные».", "Markierungen im Soldatenbogen: setzt gutes Wetter und gute Stimmung (entschlossen, dankbar, erleichtert, motiviert, gute Nachrichten) und entfernt Trauer, schlechte Nachrichten, schlechte Laune und Läuse. Die Moralwerte bleiben unberührt – das ist „Glücklich“.");
            A("They trust you",
              "Ils vous font confiance", "Eles confiam em você",
              "Они вам доверяют", "Sie vertrauen dir");
            A("Trust in you at maximum, fear of the commander at 0 and a comradely standing (soldiers of your troop).",
              "Confiance en vous au maximum, peur du commandant à 0 et relation de camaraderie (soldats de votre troupe).", "Confiança em você no máximo, medo do comandante em 0 e relação de camaradagem (soldados da sua tropa).",
              "Доверие к вам на максимуме, страх перед командиром 0 и товарищеские отношения (солдаты вашего отряда).", "Vertrauen in dich auf Maximum, Angst vor dem Kommandanten 0 und kameradschaftliches Verhältnis (Soldaten deiner Truppe).");
            A("Hospital, quartermaster and village",
              "Hôpital, intendant et village", "Hospital, intendente e aldeia",
              "Госпиталь, интендант и деревня", "Lazarett, Quartiermeister und Dorf");
            A("Global trust with all three at maximum (campaign).",
              "Confiance globale avec les trois au maximum (campagne).", "Confiança global com os três no máximo (campanha).",
              "Общее доверие ко всем трём на максимуме (кампания).", "Globales Vertrauen zu allen dreien auf Maximum (Kampagne).");
            A("Rivals become friends",
              "Les rivaux deviennent amis", "Rivais viram amigos",
              "Соперники становятся друзьями", "Rivalen werden Freunde");
            A("Among your own soldiers: rivalries turn into friendships.",
              "Parmi vos propres soldats : les rivalités deviennent des amitiés.", "Entre seus próprios soldados: as rivalidades viram amizades.",
              "Среди ваших солдат: соперничество превращается в дружбу.", "Unter deinen eigenen Soldaten: Rivalitäten werden zu Freundschaften.");
            A("Actions always succeed",
              "Actions toujours réussies", "Ações sempre bem-sucedidas",
              "Действия всегда успешны", "Aktionen gelingen immer");
            A("Raises the efficiency of your soldiers only (shooting, grenades, melee, barbed wire, jammed rifle, first aid, looting) so the rolls pass. Everything is given back when you turn it off.",
              "Augmente l'efficacité de vos soldats uniquement (tir, grenades, corps à corps, barbelés, fusil enrayé, premiers soins, pillage) pour que les jets réussissent. Tout est rendu en désactivant.", "Aumenta a eficiência apenas dos seus soldados (tiro, granadas, corpo a corpo, arame farpado, fuzil emperrado, primeiros socorros, saque) para que os testes passem. Tudo é devolvido ao desativar.",
              "Повышает эффективность только ваших солдат (стрельба, гранаты, рукопашная, колючая проволока, заклинившая винтовка, первая помощь, мародёрство), чтобы проверки проходили. При выключении всё возвращается.", "Erhöht nur die Effizienz deiner Soldaten (Schießen, Granaten, Nahkampf, Stacheldraht, Ladehemmung, Erste Hilfe, Plündern), damit die Würfe gelingen. Beim Ausschalten wird alles zurückgegeben.");
            A("Survival",
              "Survie", "Sobrevivência",
              "Выживание", "Überleben");
            A("Comfortable",
              "Confortable", "Confortável",
              "Комфортный", "Komfortabel");
            A("Everything",
              "Tout", "Tudo",
              "Всё", "Alles");
            A("None",
              "Aucun", "Nenhum",
              "Ничего", "Keine");
            A("Ready. Open a game and turn on what you want.",
              "Prêt. Ouvrez une partie et activez ce que vous voulez.", "Pronto. Abra um jogo e ative o que quiser.",
              "Готово. Откройте игру и включите нужное.", "Bereit. Spiel öffnen und aktivieren, was du willst.");
            A("Re-enabled ",
              "Réactivation de ", "Reativados ",
              "Читов из прошлой сессии включено заново: ", "Wieder aktiviert: ");
            A(" cheats from the last session.",
              " cheats de la dernière session.", " truques da sessão anterior.",
              ".", " Cheats aus der letzten Sitzung.");
            A(": on",
              " : activé", ": ativado",
              ": включено", ": an");
            A(": off",
              " : désactivé", ": desativado",
              ": выключено", ": aus");
            A("  (no mission yet: it acts once one starts)",
              "  (pas encore de mission : agit dès qu'une commence)", "  (ainda sem missão: age quando uma começar)",
              "  (миссии пока нет: сработает, когда она начнётся)", "  (noch keine Mission: wirkt, sobald eine beginnt)");
            A("  - scope: ",
              "  - portée : ", "  - alcance: ",
              "  - охват: ", "  - Umfang: ");
            A("Profile \"",
              "Profil « ", "Perfil \"",
              "Профиль «", "Profil „");
            A("\" applied: ",
              " » appliqué : ", "\" aplicado: ",
              "» применён. Активных читов: ", "“ angewendet: ");
            A(" cheats active.",
              " cheats actifs.", " truques ativos.",
              ".", " Cheats aktiv.");
            A("Profile ",
              "Profil ", "Perfil ",
              "Профиль ", "Profil ");
            A(" saved (",
              " enregistré (", " salvo (",
              " сохранён (читов: ", " gespeichert (");
            A(" cheats).",
              " cheats).", " truques).",
              ").", " Cheats).");
            A(" loaded: ",
              " chargé : ", " carregado: ",
              " загружен. Активных читов: ", " geladen: ");
            A(" active",
              " actifs", " ativos",
              " активно", " aktiv");
            A("Cheats",
              "Cheats", "Truques",
              "Читы", "Cheats");
            A("Values",
              "Valeurs", "Valores",
              "Значения", "Werte");
            A("Characters",
              "Personnages", "Personagens",
              "Персонажи", "Figuren");
            A("Settings",
              "Réglages", "Ajustes",
              "Настройки", "Einstellungen");
            A("Scope: ",
              "Portée : ", "Alcance: ",
              "Охват: ", "Umfang: ");
            A("mission in progress",
              "mission en cours", "missão em andamento",
              "миссия идёт", "Mission läuft");
            A("no mission (campaign)",
              "hors mission (campagne)", "fora de missão (campanha)",
              "вне миссии (кампания)", "keine Mission (Kampagne)");
            A("Hide (F10)",
              "Masquer (F10)", "Ocultar (F10)",
              "Скрыть (F10)", "Ausblenden (F10)");
            A("Unload trainer",
              "Décharger le trainer", "Descarregar trainer",
              "Выгрузить трейнер", "Trainer entladen");
            A("They apply to your soldiers (your troop and your side in a mission), never to the enemy. \"Mission\" = the game resets it every mission; \"campaign\" = it carries over between missions. Items tagged \"mission\" only take effect inside a mission.",
              "S'appliquent à vos soldats (votre troupe et votre camp en mission), jamais à l'ennemi. « Mission » = le jeu le réinitialise à chaque mission ; « campagne » = il se conserve entre les missions. Les éléments marqués « mission » n'ont d'effet que pendant une mission.", "Aplicam-se aos seus soldados (sua tropa e o seu lado em missão), nunca ao inimigo. \"Missão\" = o jogo reinicia a cada missão; \"campanha\" = se mantém entre missões. Os itens marcados \"missão\" só fazem efeito dentro de uma missão.",
              "Действуют на ваших солдат (ваш отряд и вашу сторону в миссии), но не на врага. «Миссия» — игра сбрасывает это в каждой миссии; «кампания» — сохраняется между миссиями. Пункты с пометкой «миссия» работают только внутри миссии.", "Wirken auf deine Soldaten (deine Truppe und deine Seite in einer Mission), nie auf den Feind. „Mission“ = das Spiel setzt es jede Mission zurück; „Kampagne“ = bleibt zwischen Missionen erhalten. Mit „Mission“ markierte Einträge wirken nur innerhalb einer Mission.");
            A("Quick profiles:",
              "Profils rapides :", "Perfis rápidos:",
              "Быстрые профили:", "Schnellprofile:");
            A("My profiles:",
              "Mes profils :", "Meus perfis:",
              "Мои профили:", "Meine Profile:");
            A("Load ",
              "Charger ", "Carregar ",
              "Загрузить ", "Laden ");
            A("Save the active ones to:",
              "Enregistrer les actifs dans :", "Salvar os ativos em:",
              "Сохранить активные в:", "Aktive speichern in:");
            A("Slot ",
              "Emplacement ", "Slot ",
              "Слот ", "Slot ");
            A(" active of ",
              " actifs sur ", " ativos de ",
              " активно из ", " aktiv von ");
            A("Collapse all",
              "Tout replier", "Recolher tudo",
              "Свернуть всё", "Alle einklappen");
            A("Expand all",
              "Tout déplier", "Expandir tudo",
              "Развернуть всё", "Alle ausklappen");
            A("No cheat matches the search.",
              "Aucun cheat ne correspond à la recherche.", "Nenhum truque corresponde à busca.",
              "Ни один чит не подходит под поиск.", "Kein Cheat passt zur Suche.");
            A("Tip: press \"Fav\" on a cheat to pin it on top and toggle it with Alt+1..9.",
              "Astuce : appuyez sur « Fav » sur un cheat pour l'épingler en haut et le basculer avec Alt+1..9.", "Dica: toque em \"Fav\" num truque para fixá-lo no topo e ativá-lo com Alt+1..9.",
              "Совет: нажмите «Fav» у чита, чтобы закрепить его сверху и переключать клавишами Alt+1..9.", "Tipp: „Fav“ bei einem Cheat drücken, um ihn oben anzuheften und mit Alt+1..9 umzuschalten.");
            A("mission",
              "mission", "missão",
              "миссия", "Mission");
            A("Trainer language",
              "Langue du trainer", "Idioma do trainer",
              "Язык трейнера", "Sprache des Trainers");
            A("Skill and status names come from the game and follow the language you picked in the game.",
              "Les noms des compétences et des états viennent du jeu et suivent la langue choisie dans le jeu.", "Os nomes de habilidades e estados vêm do jogo e seguem o idioma escolhido no jogo.",
              "Названия навыков и состояний берутся из игры и соответствуют языку, выбранному в игре.", "Die Namen von Fähigkeiten und Zuständen stammen aus dem Spiel und folgen der im Spiel gewählten Sprache.");
            A("Window",
              "Fenêtre", "Janela",
              "Окно", "Fenster");
            A("Opacity: ",
              "Opacité : ", "Transparência: ",
              "Прозрачность: ", "Deckkraft: ");
            A("Font size: ",
              "Taille du texte : ", "Tamanho da letra: ",
              "Размер шрифта: ", "Schriftgröße: ");
            A("You can resize the window by dragging the bottom-right corner (::) and move it by its title bar. Both are remembered.",
              "Vous pouvez redimensionner la fenêtre en tirant le coin inférieur droit (::) et la déplacer par sa barre de titre. Les deux sont mémorisés.", "Você pode redimensionar a janela arrastando o canto inferior direito (::) e movê-la pela barra de título. Ambos são lembrados.",
              "Размер окна меняется перетаскиванием правого нижнего угла (::), а двигать его можно за заголовок. Оба значения запоминаются.", "Du kannst die Fenstergröße über die untere rechte Ecke (::) ändern und das Fenster an der Titelleiste verschieben. Beides wird gemerkt.");
            A("Reset window position and size",
              "Réinitialiser la position et la taille de la fenêtre", "Restaurar posição e tamanho da janela",
              "Сбросить положение и размер окна", "Fensterposition und -größe zurücksetzen");
            A("Options",
              "Options", "Opções",
              "Параметры", "Optionen");
            A("Remember the active cheats when the trainer is injected again",
              "Mémoriser les cheats actifs quand le trainer est réinjecté", "Lembrar os truques ativos quando o trainer for injetado de novo",
              "Запоминать активные читы при повторной инжекции трейнера", "Aktive Cheats merken, wenn der Trainer erneut injiziert wird");
            A("Show a small indicator (\"AQT: N active\") while the menu is hidden",
              "Afficher un petit indicateur (« AQT : N actifs ») quand le menu est masqué", "Mostrar um pequeno indicador (\"AQT: N ativos\") com o menu oculto",
              "Показывать маленький индикатор («AQT: N активно») при скрытом меню", "Kleine Anzeige („AQT: N aktiv“) zeigen, solange das Menü verborgen ist");
            A("Alt+1..9 hotkeys for favorite cheats",
              "Raccourcis Alt+1..9 pour les cheats favoris", "Atalhos Alt+1..9 para os truques favoritos",
              "Горячие клавиши Alt+1..9 для избранных читов", "Alt+1..9-Tastenkürzel für Favoriten-Cheats");
            A("Show attribute icons in Characters",
              "Afficher les icônes des attributs dans Personnages", "Mostrar os ícones dos atributos em Personagens",
              "Показывать значки атрибутов во вкладке «Персонажи»", "Attributsymbole bei Figuren anzeigen");
            A("Hotkeys",
              "Raccourcis clavier", "Teclas de atalho",
              "Горячие клавиши", "Tastenkürzel");
            A("show or hide the menu",
              "afficher ou masquer le menu", "mostrar ou ocultar o menu",
              "показать или скрыть меню", "Menü ein- oder ausblenden");
            A("Alt+1..9: mark cheats with \"Fav\" in the Cheats tab to assign them a key.",
              "Alt+1..9 : marquez des cheats avec « Fav » dans l'onglet Cheats pour leur attribuer une touche.", "Alt+1..9: marque truques com \"Fav\" na aba Truques para dar-lhes uma tecla.",
              "Alt+1..9: отметьте читы кнопкой «Fav» во вкладке «Читы», чтобы назначить им клавишу.", "Alt+1..9: Cheats im Reiter Cheats mit „Fav“ markieren, um ihnen eine Taste zuzuweisen.");
            A("Files",
              "Fichiers", "Arquivos",
              "Файлы", "Dateien");
            A("Settings: ",
              "Réglages : ", "Ajustes: ",
              "Настройки: ", "Einstellungen: ");
            A("Error log: ",
              "Journal d'erreurs : ", "Registro de erros: ",
              "Журнал ошибок: ", "Fehlerprotokoll: ");
            A("Resources and supplies",
              "Ressources et fournitures", "Recursos e suprimentos",
              "Ресурсы и припасы", "Ressourcen und Vorräte");
            A("Trust",
              "Confiance", "Confiança",
              "Доверие", "Vertrauen");
            A("Chances",
              "Probabilités", "Probabilidades",
              "Вероятности", "Wahrscheinlichkeiten");
            A("Counters and turns",
              "Compteurs et tours", "Contadores e turnos",
              "Счётчики и ходы", "Zähler und Runden");
            A("Mission, trench and weather",
              "Mission, tranchée et météo", "Missão, trincheira e clima",
              "Миссия, окоп и погода", "Mission, Graben und Wetter");
            A("Other",
              "Autres", "Outros",
              "Прочее", "Sonstiges");
            A("Company provisions",
              "Provisions de la compagnie", "Provisões da companhia",
              "Запасы роты", "Kompanievorräte");
            A("Trench provisions",
              "Provisions de la tranchée", "Provisões da trincheira",
              "Запасы окопа", "Grabenvorräte");
            A("Quartermaster supplies",
              "Fournitures de l'intendant", "Suprimentos do intendente",
              "Припасы интенданта", "Vorräte des Quartiermeisters");
            A("Village food",
              "Nourriture du village", "Comida da aldeia",
              "Еда деревни", "Dorfnahrung");
            A("Prestige",
              "Prestige", "Prestígio",
              "Престиж", "Prestige");
            A("Hospital capacity",
              "Capacité de l'hôpital", "Capacidade do hospital",
              "Вместимость госпиталя", "Lazarettkapazität");
            A("Available ammunition (reserve)",
              "Munitions disponibles (réserve)", "Munição disponível (reserva)",
              "Доступные боеприпасы (резерв)", "Verfügbare Munition (Reserve)");
            A("Available bandages (reserve)",
              "Bandages disponibles (réserve)", "Bandagens disponíveis (reserva)",
              "Доступные бинты (резерв)", "Verfügbare Verbände (Reserve)");
            A("Available hand grenades",
              "Grenades à main disponibles", "Granadas de mão disponíveis",
              "Доступные ручные гранаты", "Verfügbare Handgranaten");
            A("Available smoke grenades",
              "Grenades fumigènes disponibles", "Granadas de fumaça disponíveis",
              "Доступные дымовые гранаты", "Verfügbare Rauchgranaten");
            A("Collected dog tags",
              "Plaques d'identité récupérées", "Plaquetas de identificação coletadas",
              "Собранные жетоны", "Gesammelte Erkennungsmarken");
            A("Panic",
              "Panique", "Pânico",
              "Паника", "Panik");
            A("Panic reduction",
              "Réduction de la panique", "Redução do pânico",
              "Снижение паники", "Panikreduzierung");
            A("Trust: hospital",
              "Confiance : hôpital", "Confiança: hospital",
              "Доверие: госпиталь", "Vertrauen: Lazarett");
            A("Trust: quartermaster",
              "Confiance : intendant", "Confiança: intendente",
              "Доверие: интендант", "Vertrauen: Quartiermeister");
            A("Trust: village",
              "Confiance : village", "Confiança: aldeia",
              "Доверие: деревня", "Vertrauen: Dorf");
            A("Dysentery chance",
              "Risque de dysenterie", "Chance de disenteria",
              "Шанс дизентерии", "Ruhr-Wahrscheinlichkeit");
            A("Trench foot chance",
              "Risque de pied de tranchée", "Chance de pé de trincheira",
              "Шанс окопной стопы", "Schützengrabenfuß-Wahrscheinlichkeit");
            A("Reinforcement chance",
              "Probabilité de renforts", "Chance de reforços",
              "Шанс подкрепления", "Verstärkungs-Wahrscheinlichkeit");
            A("Attack mission chance",
              "Probabilité de mission d'attaque", "Chance de missão de ataque",
              "Шанс миссии атаки", "Angriffsmission-Wahrscheinlichkeit");
            A("Defense mission chance",
              "Probabilité de mission de défense", "Chance de missão de defesa",
              "Шанс миссии обороны", "Verteidigungsmission-Wahrscheinlichkeit");
            A("Mutiny counter",
              "Compteur de mutinerie", "Contador de motim",
              "Счётчик мятежа", "Meuterei-Zähler");
            A("Turns until home leave",
              "Tours avant la permission", "Turnos até a licença",
              "Ходов до отпуска", "Runden bis zum Heimaturlaub");
            A("Turns until next resupply",
              "Tours avant le prochain ravitaillement", "Turnos até o próximo reabastecimento",
              "Ходов до следующего снабжения", "Runden bis zur nächsten Versorgung");
            A("Turns in a row without rations",
              "Tours consécutifs sans rations", "Turnos seguidos sem rações",
              "Ходов подряд без рационов", "Runden in Folge ohne Rationen");
            A("Turns in a row on low rations",
              "Tours consécutifs à rations réduites", "Turnos seguidos com rações baixas",
              "Ходов подряд с малыми рационами", "Runden in Folge mit knappen Rationen");
            A("Turns in a row on standard rations",
              "Tours consécutifs à rations normales", "Turnos seguidos com rações normais",
              "Ходов подряд со стандартными рационами", "Runden in Folge mit Standardrationen");
            A("Turns in a row on high rations",
              "Tours consécutifs à rations élevées", "Turnos seguidos com rações altas",
              "Ходов подряд с повышенными рационами", "Runden in Folge mit hohen Rationen");
            A("Wind strength",
              "Force du vent", "Força do vento",
              "Сила ветра", "Windstärke");
            A("Wind direction",
              "Direction du vent", "Direção do vento",
              "Направление ветра", "Windrichtung");
            A("Vision distance (mission)",
              "Distance de vision (mission)", "Distância de visão (missão)",
              "Дальность обзора (миссия)", "Sichtweite (Mission)");
            A("Global game values (resources, trust, counters...). Editable ones have buttons; hover a name to see its internal name.",
              "Valeurs globales de la partie (ressources, confiance, compteurs...). Les modifiables ont des boutons ; survolez un nom pour voir son nom interne.", "Valores globais do jogo (recursos, confiança, contadores...). Os editáveis têm botões; passe o mouse sobre um nome para ver o nome interno.",
              "Глобальные значения игры (ресурсы, доверие, счётчики...). У изменяемых есть кнопки; наведите курсор на название, чтобы увидеть внутреннее имя.", "Globale Spielwerte (Ressourcen, Vertrauen, Zähler ...). Bearbeitbare haben Schaltflächen; Mauszeiger auf einen Namen zeigt den internen Namen.");
            A("Also show calculated values (read-only)",
              "Afficher aussi les valeurs calculées (lecture seule)", "Mostrar também os valores calculados (somente leitura)",
              "Показывать также вычисляемые значения (только чтение)", "Auch berechnete Werte zeigen (nur lesen)");
            A("No values loaded yet (open or load a game).",
              "Aucune valeur n'est encore chargée (ouvrez ou chargez une partie).", "Ainda não há valores carregados (abra ou carregue um jogo).",
              "Значения ещё не загружены (откройте или загрузите игру).", "Noch keine Werte geladen (Spiel starten oder laden).");
            A("No value matches the search.",
              "Aucune valeur ne correspond à la recherche.", "Nenhum valor corresponde à busca.",
              "Ни одно значение не подходит под поиск.", "Kein Wert passt zur Suche.");
            A("Set",
              "Fixer", "Fixar",
              "Задать", "Setzen");
            A("Invalid number: ",
              "Nombre invalide : ", "Número inválido: ",
              "Неверное число: ", "Ungültige Zahl: ");
            A("Min",
              "Min", "Mín",
              "Мин", "Min");
            A("Max",
              "Max", "Máx",
              "Макс", "Max");
            A("(calculated)",
              "(calculé)", "(calculado)",
              "(вычисляемое)", "(berechnet)");
            A("Error: ",
              "Erreur : ", "Erro: ",
              "Ошибка: ", "Fehler: ");
            A("Language: ",
              "Langue : ", "Idioma: ",
              "Язык: ", "Sprache: ");
        }
    }
}
