
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

        if (Input.GetKeyDown(KeyCode.P))
        {
            var state = GameManager.Instance.CurrentState;

            Debug.Log(
                $"Coins: {state.coins}, " +
                $"Energy: {state.energy}, " +
                $"Resort Value: {state.resortValue}"
            );

            foreach (var building in state.buildings)
            {
                Debug.Log($"Building: {building.buildingId}, Level: {building.level}");
            }
        }
    }
}
