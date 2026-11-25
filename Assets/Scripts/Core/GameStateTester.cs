
using UnityEngine;

public class GameStateTester : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            GameManager.Instance.CurrentState.coins += 100;
            Debug.Log("Added 100 coins.");
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            GameManager.Instance.CurrentState.energy -= 10;
            Debug.Log("Removed 10 energy.");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            GameManager.Instance.CurrentState.energy -= 10;
            Debug.Log("Removed 10 energy.");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            GameManager.Instance.SaveGame();
            Debug.Log("Saved game manually.");
        }

        // BRYN-25: testovanie vizuálneho progresu: L zvýši level všetkých budov (3 sa vráti na 1).
        if (Input.GetKeyDown(KeyCode.L))
        {
            foreach (var building in GameManager.Instance.CurrentState.buildings)
            {
                building.level = building.level % 3 + 1;
            }
            Debug.Log("Changed level of all buildings.");
        }

        // BRYN-21: testovanie staveniska: B prepne bazén a bungalovy medzi stavenisko a hotovú budovu.
        if (Input.GetKeyDown(KeyCode.B))
        {
            foreach (var building in GameManager.Instance.CurrentState.buildings)
            {
                if (building.type == BuildingType.Pool || building.type == BuildingType.Bungalows)
                {
                    building.isActive = !building.isActive;
                }
            }
            GameManager.Instance.CurrentState.EnsureStaffing();
            Debug.Log("Toggled pool and bungalows.");
        }

        // Test a legal transfer without breaking the prototype's worker deficit.
        if (Input.GetKeyDown(KeyCode.W))
        {
            var state = GameManager.Instance.CurrentState;
            var source = state.buildings.Find(b => state.IsStaffed(b));
            var target = state.buildings.Find(b => b.isActive && !state.IsStaffed(b));
            GameManager.Instance.TryTransferStaff(source, target);
        }

        // BRYN-26: dočasné testovanie odomykania: N pridá jednu dokončenú vlnu.
        if (Input.GetKeyDown(KeyCode.N))
        {
            var state = GameManager.Instance.CurrentState;
            state.wavesCompleted++;
            Debug.Log($"Waves completed: {state.wavesCompleted}");
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            var state = GameManager.Instance.CurrentState;

            Debug.Log(
                $"Coins: {state.coins}, " +
                $"Energy: {state.energy}, " +
                $"Resort Value: {state.resortValue}, " +
                $"Workers: {state.workerCount}, " +
                $"Waves completed: {state.wavesCompleted}"
            );

            foreach (var building in state.buildings)
            {
                Debug.Log(
                    $"Building: {building.type}, " +
                    $"Level: {building.level}, " +
                    $"Active: {building.isActive}"
                );
            }
        }
    }
}
