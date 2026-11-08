using UnityEngine;
using UnityEngine.UI;

// BRYN-48: Start Wave button. A click locks the button and raises
// WaveEvents.StartWaveRequested. The handler must stay trivial (BRYN-94:
// wave animation within 100 ms of the click): no SaveGame, Instantiate
// or Resources.Load here.
[RequireComponent(typeof(Button))]
public class StartWaveButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(OnClick);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
    }

    // Called by the wave logic to unlock the button again.
    public void SetInteractable(bool value)
    {
        button.interactable = value;
    }

    private void OnClick()
    {
        button.interactable = false;
        WaveEvents.RaiseStartWaveRequested();
    }
}
