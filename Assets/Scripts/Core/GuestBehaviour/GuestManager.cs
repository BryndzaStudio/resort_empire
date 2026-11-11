using System.Collections.Generic;
using UnityEngine;

public class GuestManager : MonoBehaviour
{
    public static GuestManager Instance { get; private set; }

    private sealed class BuildingRuntime
    {
        public BuildingGuestQueue queue;
        public BuildingView view;
    }

    private sealed class Visit
    {
        public GuestState state;
        public GuestController view;
        public BuildingRuntime building;
        public bool serving;
        public float serviceProgress;
    }

    private readonly List<GuestState> guests = new List<GuestState>();
    private readonly List<Visit> visits = new List<Visit>();
    private readonly List<BuildingRuntime> buildings = new List<BuildingRuntime>();
    private int nextGuestId = 1;
    private WaveManager registeredWave;

    public IReadOnlyList<GuestState> Guests => guests;
    public int WaitingGuestCount
    {
        get
        {
            int count = 0;
            foreach (BuildingRuntime building in buildings)
                count += building.queue.WaitingCount;
            return count;
        }
    }

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
        RegisterWithWave();
    }

    private void Start()
    {
        RegisterWithWave();
        SyncQueueCount();
    }

    private void RegisterWithWave()
    {
        if (Instance != this || registeredWave == WaveManager.Instance) return;
        if (registeredWave != null) registeredWave.UnregisterQueue(AreVisitsComplete);
        registeredWave = WaveManager.Instance;
        if (registeredWave != null) registeredWave.RegisterQueue(AreVisitsComplete);
    }

    private bool AreVisitsComplete()
    {
        // The waiting counter excludes occupants, but the wave must finish their service too.
        return visits.Count == 0;
    }

    private void OnDisable()
    {
        if (registeredWave != null) registeredWave.UnregisterQueue(AreVisitsComplete);
        registeredWave = null;
        if (Instance != this) return;

        // Release only places owned by these visits; unrelated saved occupancy is untouched.
        foreach (BuildingRuntime building in buildings) building.queue.Clear();
        foreach (Visit visit in visits)
            if (visit.view != null) Destroy(visit.view.gameObject);
        visits.Clear();
        guests.Clear();
        SyncQueueCount();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // IslandView creates and configures building views before the first guest arrives.
    public void RegisterBuilding(BuildingView view)
    {
        if (view == null || view.State == null) return;
        BuildingRuntime existing = buildings.Find(b => b.queue.Building == view.State);
        if (existing != null)
        {
            existing.view = view;
            return;
        }
        buildings.Add(new BuildingRuntime { queue = new BuildingGuestQueue(view.State), view = view });
    }

    // BRYN-41/43: the boat assigns a need at spawn, but a place is reserved only at the island.
    public void RegisterGuest(GuestController guest)
    {
        if (guest == null || FindVisit(guest) != null) return;
        GameManager manager = GameManager.Instance;
        if (manager == null || manager.CurrentState == null)
        {
            Debug.LogWarning("Guest registration failed: no game state.");
            return;
        }
        if (!GuestNeedAssigner.TryAssignNeed(manager.CurrentState, out GuestNeed need))
        {
            Debug.LogWarning("No compatible building for guest need.");
            return;
        }

        var state = new GuestState(nextGuestId++, need);
        guests.Add(state);
        visits.Add(new Visit { state = state, view = guest });
        GuestNeedIcon icon = guest.GetComponent<GuestNeedIcon>();
        if (icon != null) icon.ShowNeed(need);
        else Debug.LogWarning("Guest prefab is missing GuestNeedIcon.");
        Debug.Log($"BRYN-41/43: Guest {state.id} needs {state.need}");
    }

    // Called before OnGuestArrived so the wave sees the last arrival's queue membership.
    public bool ArriveGuest(GuestController guest)
    {
        if (!isActiveAndEnabled) return false;
        Visit visit = FindVisit(guest);
        if (visit == null) return false;
        if (visit.building != null) return true;

        BuildingRuntime destination = null;
        foreach (BuildingRuntime building in buildings)
        {
            BuildingState state = building.queue.Building;
            if (building.view == null || !state.isActive ||
                !BuildingGuestQueue.SupportsNeed(state.type, visit.state.need)) continue;
            if (destination == null) destination = building;
            if (state.occupied < state.capacity && building.queue.WaitingCount == 0)
            {
                destination = building;
                break;
            }
        }
        if (destination == null)
        {
            Debug.LogWarning($"Guest {visit.state.id}: no active building view for {visit.state.need}.");
            return false;
        }

        GuestAdmission admission = destination.queue.Assign(visit.state);
        if (admission == GuestAdmission.Rejected) return false;
        visit.building = destination;
        if (admission == GuestAdmission.Entered) SendIntoBuilding(visit);
        else PositionQueue(destination);
        SyncQueueCount();
        return true;
    }

    private void SendIntoBuilding(Visit visit)
    {
        if (visit.view == null || visit.building.view == null) return;
        visit.view.MoveTo(visit.building.view.GuestEntrancePosition, () =>
        {
            if (!isActiveAndEnabled || visit.view == null || visit.building.view == null) return;
            visit.serving = true;
            visit.serviceProgress = 0f;
            visit.view.gameObject.SetActive(false);
        });
    }

    private void PositionQueue(BuildingRuntime building)
    {
        if (building.view == null) return;
        for (int i = 0; i < building.queue.WaitingGuests.Count; i++)
        {
            Visit visit = FindVisit(building.queue.WaitingGuests[i]);
            if (visit != null && visit.view != null)
                visit.view.MoveTo(building.view.GetQueuePosition(i));
        }
    }

    private void Update()
    {
        for (int i = visits.Count - 1; i >= 0; i--)
        {
            Visit visit = visits[i];
            if (visit.view == null || (visit.building != null && visit.building.view == null))
            {
                CancelVisit(visit);
                continue;
            }
            if (!visit.serving) continue;
            visit.serviceProgress += Time.deltaTime / visit.building.view.ServiceSeconds;
            if (visit.serviceProgress >= 1f) CompleteVisit(visit);
        }

        // Also handles a capacity increase while guests are already waiting.
        foreach (BuildingRuntime building in buildings)
        {
            if (building.view == null) continue;
            bool moved = false;
            while (building.queue.TryAdmitNext(out GuestState next))
            {
                Visit visit = FindVisit(next);
                if (visit != null) SendIntoBuilding(visit);
                moved = true;
            }
            if (moved) PositionQueue(building);
        }
        SyncQueueCount();
    }

    private void CompleteVisit(Visit visit)
    {
        if (!visit.building.queue.CompleteService(visit.state, out GuestState next)) return;
        visits.Remove(visit);
        guests.Remove(visit.state);
        if (GuestStatistics.Instance != null) GuestStatistics.Instance.GuestServed();
        if (visit.view != null) Destroy(visit.view.gameObject);
        PromoteNext(visit.building, next);
    }

    // Destroying a waiting/arriving guest cannot strand a queue or inflate "served".
    public void UnregisterGuest(GuestController guest)
    {
        Visit visit = FindVisit(guest);
        if (visit != null) CancelVisit(visit);
    }

    private void CancelVisit(Visit visit)
    {
        GuestState next = null;
        if (visit.building != null) visit.building.queue.Cancel(visit.state, out next);
        visits.Remove(visit);
        guests.Remove(visit.state);
        if (visit.view != null) Destroy(visit.view.gameObject);
        if (visit.building != null) PromoteNext(visit.building, next);
    }

    private void PromoteNext(BuildingRuntime building, GuestState next)
    {
        Visit visit = next != null ? FindVisit(next) : null;
        if (visit != null) SendIntoBuilding(visit);
        PositionQueue(building);
        SyncQueueCount();
    }

    private Visit FindVisit(GuestController guest)
    {
        return visits.Find(v => ReferenceEquals(v.view, guest));
    }

    private Visit FindVisit(GuestState state)
    {
        return visits.Find(v => v.state == state);
    }

    private void SyncQueueCount()
    {
        if (GuestStatistics.Instance != null)
            GuestStatistics.Instance.SetQueueCount(WaitingGuestCount);
    }
}
