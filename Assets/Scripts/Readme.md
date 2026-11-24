# Resort Empire — Game State & Save System

## Implemented systems

- `GameState.cs` — Stores coins, energy, resort value, buildings and worker count.
- `BuildingState.cs` — Stores each building's type, level and active flag.
- `GameManager.cs` — Initializes a new game or loads an existing save.
- `GameConfig.cs` — Configures starting coins and energy through a ScriptableObject asset (BRYN-9).
- `SaveSystem.cs` — Saves and loads game data using JSON.
- `GameStateTester.cs` — Temporary script for testing game-state functionality.

## Debug controls

These controls work while the game is running in Unity Play mode, provided the `GameStateTester` component is attached to an active GameObject.

| Key | Action                                            |
| --- | ------------------------------------------------- |
| C   | Add 100 coins                                     |
| E   | Remove 10 energy                                  |
| S   | Save the current game state                       |
| N   | Add one completed wave (temporary, BRYN-26)       |
| P   | Print the current game state in the Unity Console |

Click inside the Game view before using the controls.

## Saving and loading

- On first launch, coins and energy come from the `GameConfig` assigned to `GameManager`; resort value comes from the `GameManager` Inspector. If no config is assigned, coins and energy default to 1000 and 100 with a Console warning.
- The game state is saved to `resort_save.json` in `Application.persistentDataPath`.
- When the game starts again, the existing save is loaded automatically.
- The current implementation also **attempts to save when the application is paused or closed**.
- Press `S` during Play mode to save manually.

## Starting resort (BRYN-11 and BRYN-12)

- A new game has exactly three active buildings: Restaurant, BeachBar and Reception (BRYN-11). Each starts at level 1, consistent with BRYN-10.
- A new game has two available workers, stored as `workerCount` (BRYN-12).
- These defaults are created with each new `GameState`; separate games do not share building instances.
- Loading an older save that only contains coins, energy and resort value adds the starting buildings and workers while retaining those resource values. The next save includes the new fields.
- Saved building levels, active flags and worker counts are retained on later loads. Zero workers and an explicitly empty building list are not reset to the starting defaults.
- As with the existing resource stories, this implementation is game-state data with Console inspection. Building placement, worker characters and task assignment are not implemented.

## Top bar HUD (BRYN-14)

- `Assets/Prefabs/UI/TopBarHud.prefab` shows coins, energy and resort value in three counters at the top left (background `top_bar_counter`, icon, TextMeshPro value). Stars are not part of it.
- `TopBarHud.cs` reads `GameManager.Instance.CurrentState` in `Update` and redraws a text only when its value differs from the last displayed one. There are no change events yet, so this is polling. While `GameManager.Instance` or its state does not exist yet, it waits without errors.
- Numbers use a space as thousands separator (`1250` -> `1 250`), see `TopBarHud.Format`.
- `SafeAreaFitter.cs` fits the prefab root to `Screen.safeArea`, so the counters keep their margin inside notches and rounded corners.
- Add the prefab as a child of `UIRoot` (Canvas). It does not block clicks (`raycastTarget` is off).
- To test it, add `GameManager` and `GameStateTester` to the scene and press `C` / `E` (see Debug controls).

### Wave forecast (BRYN-16)

- `WaveForecast` in `TopBarHud.prefab` sits under the counters (same `top_bar_counter` background, 300x80, TMP text, `raycastTarget` off). It shows `Vlna: N hostí`, where N = `WaveManager.Instance.GuestsPerWave`. Guest needs are drawn on arrival, so they are not part of the forecast. The label is short because `Ďalšia vlna: N hostí` did not fit the sprite legibly (it has no 9-slice border, so the background is not widened).
- It is visible while `WaveManager.IsWaveRunning` is false (before the first wave and after a wave) and hidden during a wave. It is also hidden when there is no `WaveManager` in the scene.
- `TopBarHud.UpdateForecast` polls in `Update` and touches the UI only when the visibility or `GuestsPerWave` changes (no per-frame allocations). The fields are `forecastRoot` and `forecastText`.
- `IsWaveRunning` turns false in `WaveManager.FinishWave`, so the forecast reappears when the result screen opens (behind its overlay), not after `Pokračovať`.
- Neither `StartWaveButton` nor `WaveManager` was changed.

## Start Wave button (BRYN-48)

- `Assets/Prefabs/UI/StartWaveButton.prefab` is a full-screen container with `SafeAreaFitter`; the button (`btn_start_wave`) sits bottom right. The sprite has no text, so the "Start Wave" label is a TMP child.
- `StartWaveButton.cs` (on the Button): a click sets `interactable = false` and calls `WaveEvents.RaiseStartWaveRequested()`. Nothing else runs in the handler (no SaveGame, Instantiate or Resources.Load), because BRYN-94 needs the wave animation to start within 100 ms of the click.
- `WaveEvents.cs`: static event `StartWaveRequested`. The wave logic (BRYN-49) subscribes to it.
- `StartWaveButton.SetInteractable(bool)` is for the wave logic to unlock the button again after the wave. Until BRYN-49 does that, the button stays locked after one click.
- In `SampleScene`, `UIRoot` contains `TopBarHud` and `StartWaveButton`; the EventSystem comes from the `UIRoot` prefab.

## Wave start: boat (BRYN-49, BRYN-94)

- `BoatController` (BRYN-20) now waits off-screen for `WaveEvents.StartWaveRequested` and runs one cycle (arrive, guests get off, guests board, leave). It subscribes in `OnEnable` and unsubscribes in `OnDisable` (the event is static). After the cycle the boat stays off-screen.
- `startOnWaveRequest` (default `true`): when `false`, the boat behaves as before (starts by itself and repeats).
- Dock and off-screen points are computed once in `Start()`, not after the click, so they are not recomputed if the camera size changes at runtime. The click handler only starts a coroutine; the first boat movement happens in the same frame (no Instantiate, Resources.Load or SaveGame, no wait).
- `FrameRateSetup.cs` sets `Application.targetFrameRate = 60` before the first scene loads.
- The Start Wave button stays locked after the click. Unlocking at the end of the wave is a later task (no `WaveFinished` event yet).
- Measured in the Editor (`onClick.Invoke()` from a script, timestamp at the click vs. the first change of the boat position): about 3 ms. It does not include touch input latency; verify on a real device.
- No PlayMode test: it would need an `.asmdef`, and a test assembly cannot reference `Assembly-CSharp` where this code lives.

## Wave: even guest arrival (BRYN-50, BRYN-92)

- `WaveManager.cs` (on the `WaveManager` object in `SampleScene`) defines the wave: `Guests Per Wave` (default 10) and `Arrival Spread` (default 60 s).
- Guests do not arrive at once: the boat sends them one by one with the same gap, `ArrivalInterval = Arrival Spread / (Guests Per Wave - 1)` (10 guests in 60 s = one every 6.7 s).
- `BoatController` takes the guest count and the gap from `WaveManager.Instance` during a wave. Without a `WaveManager` in the scene it behaves as before (2 guests).
- BRYN-92: the arrival of all guests (Start Wave click until the last guest reaches the island) must take at most 90 s. `WaveManager` measures it and logs `príchod trval XX s`; above 90 s it logs a warning. The remaining time up to 90 s covers the boat trip and the walk along the pier, so `Arrival Spread` is limited to 75 s.

## Wave end (BRYN-52)

- The wave ends after all guests have arrived and their queued or ongoing service has finished.
- `GuestManager` registers with `WaveManager.RegisterQueue` and waits until all its visits finish, including guests already inside buildings. This keeps the result screen from appearing before the final service completes. The queue counter itself counts only waiting guests.
- At the end `WaveManager` raises `WaveEvents.WaveFinished(WaveResult)` with guests arrived, coins earned during the wave and duration, and unlocks the Start Wave button.
- `WaveManager.IsWaveRunning` and `WaveManager.ArrivalFinished` can be read by other systems (e.g. the panel in BRYN-61).
- If the player starts the next wave while the boat is still taking departing guests away, the boat runs the new wave right after it returns instead of ignoring the click.

## Wave result screen (BRYN-54, BRYN-93)

- `ResultScreen.cs` is on `UIRoot`. It builds the result screen in code in `Awake` (dark overlay, panel, texts, `Pokračovať` button) and keeps it hidden. `Panel Sprite` and `Button Sprite` are optional; without them plain colors are used.
- On `WaveEvents.WaveFinished` it shows: guests, coins earned and duration of the wave.
- BRYN-93: the screen must appear within 100 ms after the wave ends. It is only switched on in the same frame (nothing is loaded or instantiated). At the end of that frame it logs `Výsledok vlny zobrazený za X ms`; above 100 ms it logs a warning.
- `Pokračovať` hides the screen and raises `WaveEvents.ResultScreenClosed`; `WaveManager` then unlocks the Start Wave button. Without a `ResultScreen` in the scene the button is unlocked right at the wave end.

## Guest assignment and queues (BRYN-64, BRYN-65, BRYN-67)

- `BuildingGuestQueue` owns a FIFO line and reserved places for one building. Food goes to Restaurant, Drink to BeachBar, and Room to Reception. Inactive buildings are excluded.
- At the end of the pier, `GuestManager` reserves a free place or adds the guest to the matching building's line. A reservation counts as occupied while the guest walks to the entrance, preventing overbooking. Only overflow guests count toward `GuestsInQueue` (BRYN-65).
- Guests visibly walk to the entrance or their queue position. At the entrance they disappear inside and service begins. Service progresses using game time; after completion, the served counter increments, the place is released, and the oldest waiter walks inside (BRYN-64). The remaining line moves forward.
- `IslandView` building slots configure `Service Seconds` (currently 10), `Guest Entrance Offset`, and `Guest Queue Step`. Positions use world units relative to the building anchor. `Assets/Prefabs/World/Island.prefab` contains the layouts for all three starting buildings.
- `BoatController` waits only for guests to finish the pier journey, so building queues do not hold the boat at the dock. Assignment happens before the arrival event, including for the final guest of a wave.
- Destroying a guest releases its reservation or removes it from the queue without counting it as served. Disabling `GuestManager` clears its own visits and reservations.

## Building info panel (BRYN-112)

- `BuildingUpgradePanel` po ťuknutí na budovu ukazuje jej level, kapacitu a personál ("obsluhovaná" / "bez obsluhy" podľa `GameState.IsStaffed`), aj na maximálnom leveli.

## Odomknutie budov (BRYN-26, BRYN-27)

- Pool a Bungalows sú na začiatku stavenisko (`isActive = false`). Míľnik je počet dokončených vĺn: `GameState.wavesCompleted` (ukladá sa do `resort_save.json`, starý save ho má 0).
- Vlny aj ceny sú na jednom mieste, v `BuildingState.Unlocks`: Pool po 3. vlne za 500 mincí, Bungalows po 6. vlne za 1 000 mincí (ceny sú dočasné). Čítajú sa cez `BuildingState.UnlockWave(type)` a `UnlockCost(type)`, ktoré vrátia `-1` pre budovy, ktoré sa neodomykajú.
- `WaveProgressTracker` sa vytvorí sám pri štarte hry (`RuntimeInitializeOnLoadMethod`, `DontDestroyOnLoad`), do scény ho netreba dávať. Pri `WaveEvents.WaveFinished` zvýši `wavesCompleted` a hru uloží až o jeden frame neskôr, aby zápis súboru nezdržal obrazovku s výsledkom (BRYN-93). `WaveManager` sa nemení.
- Ťuknutie na stavenisko otvorí `BuildingUnlockPanel` (vytvorí sa sám pri prvom použití, UI v kóde, sortingOrder 150: nad upgrade panelom 100, pod Nastaveniami 200). Pred míľnikom ukáže `Odomkne sa po N. vlne (dokončené X/N)`. Po míľniku je tlačidlo `Odomknúť` s cenou; nejde, keď beží vlna alebo je málo mincí (s dôvodom).
- Odomknutie odpočíta mince, nastaví `isActive = true` a uloží hru. Budovu zobrazí `BuildingView` sám. Sprity panelu sa preberajú z `BuildingUpgradePanel.Instance` (4 verejné gettery), bez neho sa použijú farby.
- Odomknuté budovy zatiaľ nemajú potreby hostí (mimo tohto tasku).
- Testovanie: kláves `N` pridá dokončenú vlnu (pozri Debug controls).

## Settings (BRYN-18)

- Tlačidlo `Nastavenia` (`SettingsButton`) je v `TopBarHud.prefab` vpravo hore, mimo `Bars`. Ikona zatiaľ nie je, preto je tam sprite `btn_secondary` s TMP textom. Výmena za ikonu = zmeniť sprite v `Image` a vypnúť/zmeniť text `Label`.
- `Assets/Prefabs/UI/SettingsPanel.prefab` má vlastný Canvas (Overlay, `sortingOrder` 200, nad `BuildingUpgradePanel` so 100). `SettingsButton` ho vytvorí z prefabu (serialized `Panel Prefab`, bez `Resources.Load`) pri prvom otvorení, potom ho len zapína a vypína. Stmavené pozadie zachytáva kliky (nedá sa ťuknúť na budovy) a ťuknutím naň sa panel zavrie. Hra pod panelom beží ďalej.
- Panel má dva nezávislé prepínače: `Zvuky` a `Hudba` (zapnuté = `state_ok`, vypnuté = stlmený obrázok a text `Vyp`).
- `GameAudioSettings` (statická trieda) drží `SoundMuted` a `MusicMuted` a udalosť `Changed`. Ukladá sa cez `PlayerPrefs` (`settings.soundMuted`, `settings.musicMuted`) pri každej zmene, nie do `GameState` ani `resort_save.json`. Názov nie je `AudioSettings`, lebo ten patrí Unity.
- Projekt zatiaľ neobsahuje žiadny zvuk ani audio súbory. Pridané je len nastavenie a napojenie.

### Ako napojiť vlastný zvuk
1. Pridaj `AudioSource` s tvojím zvukom alebo hudbou.
2. Pridaj na ten istý objekt komponent `AudioChannel` a vyber `Channel`: `Sound` (efekty) alebo `Music` (hudba).
3. Hotovo: `AudioChannel` nastaví `AudioSource.mute` pri zapnutí objektu aj pri zmene nastavenia za behu. Na tom istom `AudioSource` nenastavuj `mute` sám, prepísalo by sa.
4. Ak zvuk púšťaš cez `AudioSource.PlayClipAtPoint` alebo vytváraš `AudioSource` v kóde, pridaj `AudioChannel` aj tam (`gameObject.AddComponent<AudioChannel>().Kind = AudioChannel.Channel.Sound`), alebo si pozri `GameAudioSettings.SoundMuted` pred prehraním.

## Hodnotenie vlny (BRYN-80, BRYN-81, BRYN-83, BRYN-84)

- BRYN-81: spokojnosť hosťa sa počíta z čakania od príchodu na ostrov (koniec móla) po začiatok obsluhy v budove. Čas obsluhy sa nepočíta. Čakanie do `Satisfied Wait Seconds` (8 s) = 100 %, od `Unhappy Wait Seconds` (40 s) = 0 %, medzi tým rovnomerne. Chôdza k budove a státie v rade sa počítajú do čakania.
- `GuestManager` vyvolá statickú udalosť `GuestServiceStarted(hosť, budova, čakanie)` v momente, keď hosť vojde do budovy. `GuestState.arrivedAt` je čas príchodu na ostrov.
- BRYN-80: `WaveRating` (statická trieda, do scény sa nepridáva) zbiera spokojnosť hostí počas vlny. `WaveManager` ju vynuluje pri štarte vlny a tesne pred `WaveEvents.WaveFinished` doplní do `WaveResult`: `satisfaction` (priemer v %), `stars` (1 – 5) a `buildings`.
- Prahy hviezdičiek sú v `Assets/Config/GameConfig.asset`: `Min Satisfaction For 2/3/4/5 Stars` (20/40/60/80 %). 1 hviezdička je vždy minimum.
- BRYN-83/84: pre každú aktívnu budovu je v `WaveResult.buildings` počet hostí a priemerné čakanie. Budova je bottleneck, ak čakanie presiahlo `Bottleneck Wait Seconds` (15 s). Obrazovka s výsledkom ich vypíše v stĺpcoch „Čo vyšlo“ (nie bottleneck, aj budovy bez hostí) a „Čo treba vylepšiť“ (bottleneck).
- `ResultScreen` má nové voliteľné polia `Star Full Sprite` a `Star Empty Sprite` (`icon_star_full`, `icon_star_empty` z `Assets/Art/horny-panel`, Texture Type musí byť Sprite). Bez nich sa hviezdička nakreslí v kóde.
- Hostia, ktorí sa nedostali do žiadnej budovy (napr. chýba budova pre ich potrebu), sa do spokojnosti nepočítajú.

## Zmena personálu na budove (BRYN-37, BRYN-91)

- Keď sa budove zmení obsluha (`GameState.IsStaffed`), nad odznakom s pracovníkom sa objaví zelené `+1 personál` (pribudol) alebo červené `-1 personál` (ubudol). Text je na tmavej podložke, vystúpi nahor a za 1 s zmizne.
- `StaffChangeIndicator` si pridáva `BuildingView` sám (len ak má prefab priradený `Worker Badge Sprite`), prefab ani scéna sa nemenia. Pri prvom zobrazení budovy sa nič neukáže, len pri zmene.
- Nezáleží na tom, čo personál zmenilo (presun personálu BRYN-33/34, nový pracovník, kláves `W` v `GameStateTester`). Stačí, aby to bolo vidieť v `GameState.IsStaffed`.
- BRYN-91: zmena sa zobrazí do 100 ms. Text je vytvorený vopred a len sa zapne v tom istom frame, v ktorom `BuildingView` zmenu zistí (kontroluje ju každý frame, takže najneskôr o 1 frame). Na konci frame sa do Console vypíše `BRYN-91: ... zobrazený za X ms`, nad 100 ms varovanie.

## Current limitations

- Workers are represented by a count; individual workers and assignments are not yet modeled.
- Building upgrades and activation have saved state only; no upgrade or activation gameplay is included.
- Service duration is configurable per building; level, worker and boost modifiers are separate work.
- Full wave/guest save-and-resume is deferred by agreement. Saving behavior is unchanged: occupancy is persisted, but visits, queues and service progress are not. Reloading a save made during service can leave occupied places without guests; the follow-up must restore complete wave state, as required by the technical document, rather than silently reset progress.
- Save-file corruption handling and validation still need to be implemented.
