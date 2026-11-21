
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

        if (Input.GetKeyDown(KeyCode.P))
        {
            var state = GameManager.Instance.CurrentState;

            Debug.Log(
                $"Coins: {state.coins}, " +
                $"Energy: {state.energy}, " +
                $"Resort Value: {state.resortValue}, " +
                $"Workers: {state.workerCount}"
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
