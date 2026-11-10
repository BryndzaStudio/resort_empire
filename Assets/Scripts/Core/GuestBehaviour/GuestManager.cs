
using System.Collections.Generic;
using UnityEngine;

public class GuestManager : MonoBehaviour
{
    public static GuestManager Instance { get; private set; }

    private readonly List<GuestState> guests = new List<GuestState>();
    private int nextGuestId = 1;

    public IReadOnlyList<GuestState> Guests => guests;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GuestController.OnGuestArrived += HandleGuestArrived;
    }

    private void OnDisable()
    {
        GuestController.OnGuestArrived -= HandleGuestArrived;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleGuestArrived(GuestController guest)
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null || gameManager.CurrentState == null)
        {
            Debug.LogWarning("Cannot assign guest need: GameState unavailable.");
            return;
        }

        if (!GuestNeedAssigner.TryAssignNeed(
            gameManager.CurrentState, out GuestNeed need))
        {
            Debug.LogWarning("Cannot assign guest need: no active compatible building.");
            return;
        }

        GuestState guestState = new GuestState(nextGuestId++, need);
        guests.Add(guestState);

        Debug.Log($"BRYN-41: Guest {guestState.id} assigned need: {guestState.need}");
    }
}
