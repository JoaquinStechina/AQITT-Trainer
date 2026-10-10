# All Quiet in the Trenches – Trainer (solo un jugador)

Trainer externo **sin BepInEx** y sin modificar ningún archivo del juego. `AQTrainer.exe` inyecta en
memoria un pequeño agente (`AQTAgent.dll`) dentro del juego que ya está abierto; el agente usa la propia
API del juego (`GameValueManager`, `SavedCharacterGameValue`, `AttributeManager`, …), así que no hay
direcciones de memoria que se rompan con cada parche. Al cerrar el juego, todo desaparece.

## Uso
1. Abre el juego (con llegar al menú principal alcanza).
2. Ejecuta `AQTrainer.exe` (doble clic). Debe decir **"Inyectado OK"**.
3. Vuelve a la ventana del juego (clic o Alt+Tab). Aparece el menú del trainer.
4. **F10** o **Insert**: mostrar/ocultar el menú. **Alt+1..9**: activar/desactivar tus trucos favoritos.

## Funciones
Pestaña **Trucos** (se aplican cada 0,3 s a *tus* soldados y a los de tu mismo bando, nunca al enemigo). Cada casilla trae
debajo una línea que explica qué toca exactamente. "Misión" = el juego lo reinicia en cada misión; "campaña" = se arrastra entre misiones.

**Equipo** (lo que lleva cada soldado)
- Munición, vendas, granadas de mano y granadas de humo infinitas. *Vendas infinitas* es solo el stock; que la cura salga bien
  depende de "Acciones siempre exitosas".

**Cuerpo**
- **Sin hambre** (campaña): pone a cada personaje la ración `CharacterRations_High` (el grupo de raciones reemplaza a Pitiful/Meager/Standard),
  quita las raciones de hambre de trinchera (`CharacterTrenchProvision_None*`) y las raciones reducidas (`IndividualRations_Less`),
  llena las provisiones de la compañía (`Provision`) y del campamento de trinchera (`TrenchProvision`) y deja en 0 los contadores
  de turnos con raciones malas (`TurnCounter_Rations_None/Low`). Ojo: esto también llena el stock de comida de la compañía.
- **Buena salud (cura)**: `HealthImpairment`, `BleedingProcess` y `BleedingIntensityValue` en 0; quita las heridas (todas las `Injury_*`),
  el sangrado, las enfermedades (disentería, gripe, fiebre de trinchera, gas, tuberculosis), **todas las complicaciones `Complication_*`**
  (erupción de piel / Skin rash, tos, náuseas, dolor, insomnio, fiebre, falta de aire, inflamación, gangrena, neumonía…) y deja el estado
  de salud en "Sano". También baja al mínimo la probabilidad de disentería, pie de trinchera y complicaciones de hospital. No toca a los muertos.
  Cura lo que *ya* tienen; no evita heridas nuevas.
- **Soldados invulnerables (previene)**: usa el atributo `InvincibleWound` que el propio juego trae; evita heridas *nuevas*, no cura las que ya hay.
- **Sin cansancio**: `PhysicalExertion` (misión) y `Exhaustion` (campaña) siempre en 0. En el juego el agotamiento sube en cada acción
  según el estrés y el esfuerzo, y con esfuerzo 60 o más el soldado queda sin aliento (`Breathless`).
- **Buena condición física**: `Fitness` al máximo (20). Más forma física = menos esfuerzo físico y menos sangrado; el juego
  muestra "Excelente/Buena condición física" a partir de ese valor. Con *Sin cansancio* y *Buena salud* activos casi no suma nada.

**Mente**
- **Felices (moral y optimismo)**: `Optimism` al máximo (+4) y moral (`Morale`) al máximo (tope del propio valor; 20 si no tuviera).
- **Sin estrés**: `Stress` en 0 (misión). El estrés alimenta el agotamiento y también entra en el cálculo de la munición que gasta cada acción.
- **Exaltados**: `FervorValue` al máximo (100) y el estado **Fervor** puesto (misión). Normalmente el fervor sube con los aciertos en
  combate; con el estado puesto cada acción suma 20 puntos menos de esfuerzo físico.
- **Soldados que nunca se rinden**: atributo `InvincibleSurrender` del juego.

**Circunstancias y relaciones**
- **Circunstancias buenas**: quita las malas de la categoría *Circumstances* (duelo `Grief_*`, eventos negativos, malas noticias,
  humor malo —Dejected, Upset, Shaken, Resentful, Nauseated, Disappointed—, "Daunted", "Work-crazed", piojos) y pone las buenas
  (buen clima, eventos positivos, humor Determined/Grateful/Relieved, "Motivated", buenas noticias). Son etiquetas de la ficha del soldado
  que el juego también maneja por su cuenta, así que si alguna vuelve a aparecer el trainer la corrige en el siguiente ciclo (0,3 s).
  No toca los números de moral: eso es *Felices*.
- **Confían en vos**: `Trust` (confianza) al máximo y `Fear` (miedo al comandante) en 0, y el estado *Relation: Comradely*.
  Es el único lugar donde se toca `Fear`.
- **Hospital, intendente y aldea**: `HospitalTrust`, `QuartermasterTrust` y `VillageTrust` al máximo (99).
  Ojo: son valores de toda la compañía, no de un soldado.
- **Rivales pasan a ser amigos**: las rivalidades entre tus soldados se convierten en amistades (la relación es simétrica).

**Acciones** → **Acciones siempre exitosas** (Trucos): el juego no usa una tirada global; cada acción de misión compara un porcentaje con
`Random.Range(0,100)`, y ese porcentaje sale de un valor de "eficiencia" de cada soldado. El truco suma una cantidad fija a esas
eficiencias **solo en tus soldados** (el enemigo queda igual) y la resta al desactivar:
disparo (base/cobertura/flanqueo/vigilancia/supresión), granadas (impacto directo), cuerpo a cuerpo, alambre de púas y
arreglar el fusil trabado (comparten `DexterityBasedEfficiency`), primeros auxilios y saqueo (más cantidad).
Lo que **no** cubre: eventos/diálogos de la historia con tiradas propias, el chequeo de "saqueo de cadáveres" (depende de la
personalidad *Indifferent*) y el movimiento (la eficiencia de movimiento también cambia la velocidad, así que no se toca).


Pestaña **Valores**: valores globales de la partida (recursos, confianza, probabilidades, contadores…) con **nombres legibles**
(el juego solo tiene el nombre interno; pasá el mouse por un nombre para verlo), agrupados en secciones plegables y con buscador. Los editables
tienen botones -10 / -1 / +1 / +10, un campo para escribir un número exacto (**Enter** o "Fijar") y **Mín / Máx** (si el valor tiene tope).
Los valores calculados (solo lectura, son la mayoría) están ocultos; se muestran con la casilla de arriba. Cada fila tiene **Fav**.

Pestaña **Personajes**: elegí un soldado de tu tropa (p. ej. Elmar Framm, Georg Ackermann, Carr Mencke, Peter Linnecke)
y **agregá o quitá** sus atributos de las categorías **Habilidades** (Skills: Veteran, Sniper, Medic, Strong, Weak…), **Estado** (Status: heridas,
complicaciones, estrés, agotamiento, forma física…), **Circunstancias** (clima, humor, duelo, noticias…) y **Relación** (con vos, hospital, intendente, aldea),
con el **ícono del propio juego** de cada atributo. Arriba, la línea **"Tiene:"** lista todo lo que tiene el soldado elegido. Buscador, filtro "solo los que tiene",
**"Aplicar a TODA la tropa"** (agregar/quitar a todos a la vez), **Deshacer** (hasta 30 cambios; vuelve a poner también los opuestos que se habían quitado)
y una pestaña **Favoritos** con los atributos que marcaste con "Fav".
Notas: Strong/Weak, Quick/Slow, Dextrous/Clumsy y KeenPerception/PoorPerception son opuestos y se reemplazan entre sí.
Los marcados "(auto)" (estrés, agotamiento, forma física…) son niveles que el juego recalcula solo cuando cambia el valor
numérico, así que pueden volver a cambiar; quitar una herida no cambia por sí solo el estado de salud (herido/sano) del soldado.

Pestaña **Ajustes**: idioma del trainer (**Español, English, Français, Português, Русский, Deutsch**), transparencia, tamaño de letra y las opciones de más abajo.

"Descargar trainer" quita el agente del juego y revierte los atributos de invulnerabilidad.

## Qué cambió en la v6 (interfaz)
- **Idioma**: Español, English, Français, Português, Русский y Deutsch, en la pestaña Ajustes (se recuerda; el texto en inglés es la clave de las traducciones en `agent/Translations.cs`, y si falta una se muestra en inglés). Los nombres de habilidades y estados vienen del juego y siguen el idioma del juego.
- **Perfiles**: *Supervivencia*, *Cómodo*, *Todo*, *Ninguno* + **3 ranuras propias** (Guardar / Cargar). El trainer **recuerda los trucos activos** y los
  vuelve a activar al inyectarse de nuevo (se puede apagar en Ajustes).
- **Trucos**: secciones plegables (con "N/M activos"), buscador, contador "N activos de M", **Plegar/Desplegar todo**, casillas en verde cuando están
  activas, los de "solo misión" en gris fuera de una misión, sección **Favoritos** arriba y teclas rápidas **Alt+1..9** para los favoritos.
  Con el menú oculto aparece un indicador chico "AQT: N activos [F10]" (se puede apagar).
- **Contexto**: sobre la lista siempre se ve el alcance ("N soldados", "misión en curso" / "fuera de misión") y al activar un truco el mensaje de abajo dice a cuántos soldados llega.
- **Ventana**: se redimensiona arrastrando la esquina inferior derecha (`::`), recuerda posición y tamaño, y tiene sliders de transparencia y tamaño de letra.
- Los ajustes se guardan en `%APPDATA%\AQTrainer\settings.ini` (texto plano; se puede editar con el juego cerrado).

## Qué cambió en la v5 (limpieza de trucos repetidos)
Revisé cada casilla contra el código del juego y estas se pisaban o eran confusas:
- **"Moral al máximo" y "Felices"** escribían el mismo valor (`Morale`: una a 15, la otra a 20). Se fusionaron: *Felices (moral y optimismo)*.
- **"Felices" y "Relación con vos"** ponían ambas `Fear` en 0. Ahora solo lo hace *Confían en vos* (`Fear` es el miedo al comandante, no "felicidad").
- **"Sin esfuerzo físico" y "Sin agotamiento"** eran dos casillas de lo mismo: el agotamiento se calcula a partir del estrés y el esfuerzo en cada
  acción. Ahora es *Sin cansancio*. *Sin estrés* sigue aparte porque el estrés también entra en la munición que gasta cada acción.
- **Vendas**: se renombró a *Vendas infinitas* (es el stock), para no confundirlo con el éxito de la cura (en *Acciones siempre exitosas*).
- **Invulnerables vs. Buena salud**: se aclaró que uno *previene* heridas nuevas y el otro *cura* las existentes. Siguen siendo dos casillas.
- Las secciones antes eran "Estado de los soldados / Bienestar / Supervivencia" (nombres que se pisaban); ahora son Equipo, Cuerpo, Mente,
  Circunstancias y relaciones, y Acciones.
- Se corrigió la descripción de *Exaltados*: lo que el código del juego hace con el estado Fervor es restar esfuerzo físico, no estrés.
- **Corrección en *Buena salud***: no quitaba las complicaciones salvo fiebre y falta de aire (el juego tiene 15 `Complication_*`, entre ellas *Skin rash*). Ahora las quita todas.
- **Corrección del error "Getting control N's position in a group with only M controls when doing Repaint"** al abrir la pestaña Personajes (popup "Error / Oops" del juego). Era un fallo del menú del trainer: IMGUI exige dibujar los mismos controles en cada pasada, y la lista de Personajes cambiaba su cantidad de filas a mitad de pasada (categoría, buscador, "solo los que tiene"), mostraba el tooltip solo con el mouse encima y la pestaña activa cambiaba en el mismo evento. Ahora todo eso se congela en el evento Layout y la descripción del atributo se muestra en una etiqueta fija debajo de la lista. El buscador de la pestaña Valores tenía el mismo problema y se corrigió igual.
Siguen solapadas a propósito (se dejan por separado porque son lo que pediste): *Buena condición física* con *Sin cansancio*/*Buena salud*,
y *Exaltados* con *Sin cansancio*.

## Importante / honestidad técnica
- **No pude ejecutar el juego** en mi entorno: todo está compilado contra tus DLL reales (las firmas de la API
  están verificadas por el compilador) pero la inyección y el menú **no están probados en tu máquina**.
  Si algo falla, mira el log: `%TEMP%\AQTrainer.log` y avísame con lo que diga.
- Los trucos de estado (moral, estrés, cansancio y los de bienestar) se basan en leer cómo usa el juego esos valores
  y atributos (nombres exactos tomados de los datos del juego); si alguno se comporta raro, desactívalo y dime cuál.
  Si algún nombre no existe en tu versión del juego, el trainer lo anota en el log (`Bienestar: no se encontro ...`)
  y simplemente se salta esa parte.
- Haz una copia de tus partidas guardadas antes de probar. Si guardas con "invulnerables" activo, el atributo
  queda en esa partida hasta que lo desactives. Lo mismo vale para "Acciones siempre exitosas" (la mejora queda
  sumada en los soldados hasta que lo desactives o descargues el trainer).
- Windows Defender / SmartScreen puede marcar `AQTrainer.exe` como sospechoso: es un falso positivo típico de
  cualquier inyector (usa `CreateRemoteThread`/`WriteProcessMemory`). El código fuente completo está en este
  paquete para que lo revises o lo recompiles.
- Pensado solo para la campaña de un jugador. No usar en partidas con otras personas.

## Si el juego se actualiza o quieres recompilar
`build.bat` recompila ambos binarios con el compilador de .NET Framework que ya trae Windows
(`build.bat "H:\SteamLibrary\steamapps\common\All Quiet in the Trenches"`).

## Archivos
| Archivo | Qué es |
|---|---|
| `AQTrainer.exe` | Inyector (consola). SHA-256: `c123cec0710a57c33a133d36681069676f0b4d70ba7ac38b8d462c778b26d87f` |
| `AQTAgent.dll` | Agente que corre dentro del juego. SHA-256: `68403ac64ceeb247efbd5f229fc0c65bb172c6aaa4e8297779c7ccb634c0062c` |
| `agent/*.cs` | Código del agente (menú, ajustes, trucos, bienestar, personajes, valores) |
| `injector/*.cs` | Código del inyector (adaptado de SharpMonoInjector, licencia MIT en `LICENSE-SharpMonoInjector.txt`) |
