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

## Current limitations

- Workers are represented by a count; individual workers and assignments are not yet modeled.
- Building upgrades and activation have saved state only; no upgrade or activation gameplay is included.
- Save-file corruption handling and validation still need to be implemented.
