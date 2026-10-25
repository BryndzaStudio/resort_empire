# Resort Empire — Game State & Save System

## Implemented systems

- `GameState.cs` — Stores coins, energy and resort value.
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

## Current limitations

- Only coins, energy and resort value are stored.
- Workers, buildings, upgrades and other gameplay data are not yet included.
- Save-file corruption handling and validation still need to be implemented.
