# Resort Empire — Game State & Save System

## Implemented systems

- `GameState.cs` — Stores coins, energy, resort value, buildings and worker count.
- `BuildingState.cs` — Stores each building's type, level and active flag.
- `GameManager.cs` — Initializes a new game or loads an existing save.
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

- On first launch, a new game state is initialized using values configured in the `GameManager` Inspector.
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

## Current limitations

- Workers are represented by a count; individual workers and assignments are not yet modeled.
- Building upgrades and activation have saved state only; no upgrade or activation gameplay is included.
- Save-file corruption handling and validation still need to be implemented.
