# All Quiet in the Trenches – Trainer (single player)

An external trainer for **All Quiet in the Trenches** that does **not** need BepInEx and does not modify any game file.
`AQTrainer.exe` injects a small agent (`AQTAgent.dll`) into the running game's memory. The agent uses the game's own API
(`GameValueManager`, `SavedCharacterGameValue`, `AttributeManager`, …), so there are no memory addresses that break with every patch.
Everything disappears when you close the game.

Built for Unity 2022.3 / Mono. Single-player only. Works on **Windows** and on the **native Linux** build of the game (see [Linux](#linux)).

## Usage (Windows)
1. Start the game (reaching the main menu is enough).
2. Run `AQTrainer.exe` (double click). It should print **"Injected OK"**.
3. Go back to the game window (click it or Alt+Tab). The trainer menu appears.
4. **F10** or **Insert** shows/hides the menu. **Alt+1..9** toggles your favorite cheats.

The menu is available in **English, Español, Français, Português, Русский and Deutsch** (Settings tab).

## Linux

The game has a native Linux build (Steam, Unity Mono), so the trainer ships a Linux loader that does the same job as the Windows injector:
it attaches a thread to the game's Mono runtime, loads `AQTAgent.dll` from memory and calls `AQT.Loader.Load()`. The menu and cheats are
exactly the same as on Windows. macOS is not supported.

Requirements: x86-64 Linux with glibc 2.34 or newer (Ubuntu 22.04+, Debian 12+, Fedora, Arch, SteamOS…). `AQTAgent.dll` must stay in the folder
above `linux/` (or next to `libaqtloader.so`). The prebuilt `linux/libaqtloader.so` and `linux/aqt-inject` can be rebuilt with `sh linux/build.sh` (needs `gcc`).

### Option A – Steam launch option (recommended, no root)
In Steam: right-click the game → *Properties* → *Launch Options*, and set:

```
LD_PRELOAD="/full/path/to/AQTrainer/linux/libaqtloader.so" %command%
```

Start the game normally. The loader waits (in a background thread) until the game has loaded its scripts and then injects the agent; the
menu appears a few seconds after the main menu. Steam may print harmless "cannot be preloaded (wrong ELF class)" warnings for its 32-bit helper
processes. Remove the launch option to play without the trainer.

### Option B – attach to a running game
```
sudo ./linux/aqt-inject            # finds the game by name; or: sudo ./linux/aqt-inject <pid>
```
`aqt-inject` uses `ptrace` to make the running game `dlopen()` the loader. This needs root, or `ptrace_scope` set to 0
(`echo 0 | sudo tee /proc/sys/kernel/yama/ptrace_scope`). If you cannot or do not want that, use Option A. It also assumes the game runs
on your system's libc (not inside a different container image).

### Linux files and logs
- Settings: `~/.config/AQTrainer/settings.ini` (or `$XDG_CONFIG_HOME/AQTrainer/settings.ini`).
- Log (loader and agent): `/tmp/AQTrainer.log` (or `$TMPDIR/AQTrainer.log`). If nothing appears in the game, read this file first.
- Optional environment variables for the loader: `AQT_AGENT` (full path to `AQTAgent.dll`), `AQT_WAIT_ASSEMBLY` (assembly to wait for, default
  `Assembly-CSharp`; `-` = inject immediately), `AQT_DELAY_MS` (extra delay after that, default 2000).
- The loader was tested against a stock Mono runtime with both options. It has **not** been tested with the real Linux build of the game.

## Features

### Cheats tab
Cheats are applied every 0.3 s to *your* soldiers (your troop plus your side during a mission), never to the enemy.
Each checkbox has a line below it explaining exactly what it touches. "Mission" = the game resets the value every mission;
"campaign" = it carries over between missions. Cheats tagged *mission* only take effect inside a mission and are greyed out otherwise.

**Equipment**
- Infinite ammo, bandages, hand grenades and smoke grenades. *Infinite bandages* only keeps the stock full; whether the treatment
  succeeds depends on *Actions always succeed*.

**Body**
- **No hunger** (campaign): gives every character the `CharacterRations_High` ration (the ration group replaces Pitiful/Meager/Standard),
  removes the trench-hunger rations (`CharacterTrenchProvision_None*`) and reduced rations (`IndividualRations_Less`), fills the company
  (`Provision`) and trench camp (`TrenchProvision`) provisions and resets the bad-rations turn counters (`TurnCounter_Rations_None/Low`).
  Note: this also fills the company's food stock.
- **Good health (heals)**: sets `HealthImpairment`, `BleedingProcess` and `BleedingIntensityValue` to 0; removes wounds (all `Injury_*`),
  bleeding, diseases (dysentery, flu, trench fever, gas, tuberculosis) and **all `Complication_*`** (skin rash, cough, nausea, pain, insomnia,
  fever, shortness of breath, swelling, gangrene, pneumonia…), and sets the health state to "Healthy". It also lowers the chance of dysentery,
  trench foot and hospital complications to the minimum. Dead characters are not touched. It heals what they *already* have; it does not prevent new wounds.
- **Invulnerable soldiers (prevents)**: uses the game's own `InvincibleWound` attribute; prevents *new* wounds, does not heal existing ones.
- **No fatigue**: `PhysicalExertion` (mission) and `Exhaustion` (campaign) always 0. In the game, exhaustion grows with every action depending on
  stress and exertion, and at exertion 60 or more the soldier becomes breathless (`Breathless`).
- **Good physical condition**: `Fitness` at maximum (20). More fitness means less exertion and less bleeding. Almost redundant with
  *No fatigue* and *Good health*.

**Mind**
- **Happy (morale and optimism)**: `Optimism` at maximum (+4) and `Morale` at its own cap (20 if it had none).
- **No stress**: `Stress` at 0 (mission). Stress feeds exhaustion and also enters the ammunition each action consumes.
- **Fervent**: `FervorValue` at maximum (100) and the **Fervor** state applied (mission). Fervor normally rises with combat successes;
  with the state applied every action costs 20 fewer points of physical exertion.
- **Soldiers never surrender**: the game's `InvincibleSurrender` attribute.

**Circumstances and relationships**
- **Good circumstances**: removes the bad ones from the *Circumstances* category (grief `Grief_*`, negative events, bad news, bad moods —
  Dejected, Upset, Shaken, Resentful, Nauseated, Disappointed—, "Daunted", "Work-crazed", lice) and applies the good ones (good weather,
  positive events, Determined/Grateful/Relieved moods, "Motivated", good news). These are tags on the soldier's sheet that the game also manages
  by itself, so if one comes back the trainer fixes it on the next cycle. It does not touch the morale numbers: that is *Happy*.
- **They trust you**: `Trust` at maximum, `Fear` (fear of the commander) at 0 and the *Relation: Comradely* state. This is the only place `Fear` is touched.
- **Hospital, quartermaster and village**: `HospitalTrust`, `QuartermasterTrust` and `VillageTrust` at maximum (99).
  These are company-wide values, not per-soldier.
- **Rivals become friends**: rivalries between your soldiers turn into friendships (the relationship is symmetric).

**Actions → Actions always succeed.** The game has no global dice roll: every mission action compares a percentage against
`Random.Range(0,100)`, and that percentage comes from a per-soldier "efficiency" value. This cheat adds a fixed amount to those efficiencies
**for your soldiers only** (the enemy is unchanged) and subtracts it when turned off: shooting (base/cover/flanking/overwatch/suppression),
grenades (direct hit), melee, barbed wire and clearing a jammed rifle (both use `DexterityBasedEfficiency`), first aid and looting (larger amount).
Not covered: story events/dialogues with their own rolls, the "loot corpses" check (depends on the *Indifferent* personality) and movement
(movement efficiency also changes speed, so it is left alone).

**Quality of life**
- Quick profiles (*Survival*, *Comfortable*, *Everything*, *None*) and **3 custom slots** (save/load).
- Collapsible sections with "N/M active", search box, "N active of M" counter, *Collapse/Expand all*.
- **Favorites** section on top and **Alt+1..9** hotkeys for favorite cheats.
- Active cheats are remembered and re-enabled the next time the trainer is injected (can be turned off).
- Small "AQT: N active [F10]" indicator while the menu is hidden (can be turned off).
- A scope line ("N soldiers", "mission in progress" / "no mission") and a status message saying how many soldiers each cheat reaches.

### Values tab
Global game values (resources, trust, chances, counters…) with **readable names** (the game only has internal names; hover a name to see it),
grouped into collapsible sections, with a search box. Editable values have -10 / -1 / +1 / +10 buttons, a field for an exact number
(**Enter** or *Set*) and **Min / Max** buttons (when the value has a cap). Calculated (read-only) values — most of them — are hidden;
a checkbox shows them. Every row has a **Fav** toggle.

### Characters tab
Pick a soldier from your troop (e.g. Elmar Framm, Georg Ackermann, Carr Mencke, Peter Linnecke) and **add or remove** attributes from the
**Skills** (Veteran, Sniper, Medic, Strong, Weak…), **Status** (wounds, complications, stress, exhaustion, fitness…), **Circumstances**
(weather, mood, grief, news…) and **Relation** (to you, hospital, quartermaster, village) categories, with the game's own icon for each attribute.
- A **"Has:"** line lists everything the selected soldier has.
- Search box and an "only the ones it has" filter.
- **"Apply to the WHOLE troop"** adds/removes the attribute for every soldier at once.
- **Undo** (up to 30 changes; it also restores the opposite attributes that were removed).
- A **Favorites** category with the attributes you marked with *Fav*.

Notes: Strong/Weak, Quick/Slow, Dextrous/Clumsy and KeenPerception/PoorPerception are opposites and replace each other.
Items marked "(auto)" (stress, exhaustion, fitness…) are levels the game recalculates by itself when the underlying number changes, so they can
change back; removing a wound does not by itself change the soldier's health state (wounded/healthy).

### Settings tab
Trainer language, window opacity, font size, and options (remember active cheats, HUD indicator, hotkeys, attribute icons).
The window can be resized by dragging its bottom-right corner (`::`); position and size are remembered.
Settings are stored in `%APPDATA%\AQTrainer\settings.ini` on Windows and `~/.config/AQTrainer/settings.ini` on Linux (plain text; safe to edit with the game closed).

**Unload trainer** removes the agent from the game and reverts the invulnerability attributes.

## Changelog highlights
- **Linux support** – native Linux loader (`LD_PRELOAD` launch option or `ptrace` attach); same agent and menu.
- **v6** – new interface: profiles, favorites, search, collapsible sections, hotkeys, HUD indicator, scope/status messages, resizable window,
  opacity and font-size sliders, friendly value names, "apply to whole troop", undo, attribute icons, persistent settings, and six languages.
- **v5.2** – fixed the IMGUI error *"Getting control N's position in a group with only M controls when doing Repaint"* (the layout changed
  its number of controls mid-pass; everything that affects the control count is now frozen on the Layout event).
- **v5.1** – *Good health* now removes **all** `Complication_*` (before it missed everything except fever and shortness of breath, e.g. *Skin rash*).
- **v5** – removed duplicated/overlapping cheats: *Max morale* + *Happy* merged; *Happy* no longer sets `Fear` (only *They trust you* does);
  *No physical exertion* + *No exhaustion* merged into *No fatigue*; clearer names and descriptions; corrected *Fervent* description.

## Honest technical notes
- The game could not be run in the development environment: everything is compiled against the real game DLLs (the compiler verifies the
  API signatures), but injection and the menu are **not fully tested**. If something fails, check the log (`%TEMP%\AQTrainer.log` on Windows, `/tmp/AQTrainer.log` on Linux) and open an issue with its content.
- The state cheats (morale, stress, fatigue, wellbeing) are based on reading how the game uses those values and attributes (exact names taken
  from the game data). If one behaves oddly, turn it off. If a name does not exist in your game version, the trainer logs it
  (`Wellbeing: could not find ...`) and skips that part.
- Back up your saves before trying it. If you save with *Invulnerable* active, the attribute stays in that save until you turn it off.
  The same applies to *Actions always succeed* (the boost stays added to the soldiers until you turn it off or unload the trainer).
- Windows only: Windows Defender / SmartScreen may flag `AQTrainer.exe`: this is a typical false positive for any injector (it uses
  `CreateRemoteThread`/`WriteProcessMemory`). The full source is in this repository so you can review or rebuild it.
- Intended for single-player campaigns only.

## Building
On Linux, `sh linux/build.sh` rebuilds the loader (needs `gcc`). On Windows, `build.bat` rebuilds both binaries with the .NET Framework compiler that ships with Windows (C# 5):

```
build.bat "H:\SteamLibrary\steamapps\common\All Quiet in the Trenches"
```

## Files
| File | What it is |
|---|---|
| `AQTrainer.exe` | Injector (console). SHA-256: `ec1954fd243c43a7eb6daab7f09acfc9c95065e25225c87e52df495c66f0e75b` |
| `AQTAgent.dll` | Agent that runs inside the game. SHA-256: `8d6aa2c141fe68386ddcd82f7384a341cfe856d1cbac72079b31d53ec0889382` |
| `agent/*.cs` | Agent source (menu, settings, cheats, wellbeing, characters, values, translations) |
| `linux/libaqtloader.so` | Linux loader (loaded with `LD_PRELOAD` or by `aqt-inject`); source in `linux/aqt_loader.c` |
| `linux/aqt-inject` | Linux attach tool (ptrace); source in `linux/aqt_inject.c`; `linux/build.sh` rebuilds both |
| `injector/*.cs` | Injector source (adapted from SharpMonoInjector, MIT license in `LICENSE-SharpMonoInjector.txt`) |
