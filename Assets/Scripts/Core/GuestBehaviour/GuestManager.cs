
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // BRYN-41/43: Assign a need when an arriving guest spawns.
    public void RegisterGuest(GuestController guest)
    {
        if (guest == null)
        {
            return;
        }

        GameManager manager = GameManager.Instance;

        if (manager == null || manager.CurrentState == null)
        {
            Debug.LogWarning("Guest registration failed: no game state.");
            return;
        }

        if (!GuestNeedAssigner.TryAssignNeed(
            manager.CurrentState, out GuestNeed need))
        {
            Debug.LogWarning("No compatible building for guest need.");
            return;
        }

        GuestState guestState = new GuestState(nextGuestId++, need);
        guests.Add(guestState);

        GuestNeedIcon needIcon = guest.GetComponent<GuestNeedIcon>();

        if (needIcon != null)
        {
            needIcon.ShowNeed(need);
        }
        else
        {
            Debug.LogWarning("Guest prefab is missing GuestNeedIcon.");
        }

        Debug.Log(
            $"BRYN-41/43: Guest {guestState.id} needs {guestState.need}"
        );
    }
}
