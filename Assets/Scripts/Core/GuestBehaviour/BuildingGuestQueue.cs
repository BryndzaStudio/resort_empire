using System;
using System.Collections.Generic;

public enum GuestAdmission
{
    Rejected,
    Entered,
    Queued
}

// BRYN-64/65/67: one FIFO queue and a set of reserved places per building.
// Occupied includes guests walking to their reserved place, preventing overbooking.
public class BuildingGuestQueue
{
    private readonly List<GuestState> waiting = new List<GuestState>();
    private readonly List<GuestState> admitted = new List<GuestState>();

    public BuildingState Building { get; }
    public IReadOnlyList<GuestState> WaitingGuests => waiting;
    public IReadOnlyList<GuestState> AdmittedGuests => admitted;
    public int WaitingCount => waiting.Count;

    public BuildingGuestQueue(BuildingState building)
    {
        Building = building ?? throw new ArgumentNullException(nameof(building));
    }

    public GuestAdmission Assign(GuestState guest)
    {
        if (guest == null || !Building.isActive || !SupportsNeed(Building.type, guest.need) ||
            ContainsGuest(guest.id))
        {
            return GuestAdmission.Rejected;
        }

        if (waiting.Count == 0 && Building.occupied < Building.capacity)
        {
            Admit(guest);
            return GuestAdmission.Entered;
        }

        waiting.Add(guest);
        return GuestAdmission.Queued;
    }

    // A service may complete only once, and only for an admitted guest.
    public bool CompleteService(GuestState guest, out GuestState nextGuest)
    {
        nextGuest = null;
        if (guest == null || !admitted.Remove(guest))
        {
            return false;
        }

        Building.occupied = Math.Max(0, Building.occupied - 1);
        TryAdmitNext(out nextGuest);
        return true;
    }

    // Cancellation frees a reservation without marking the guest as served.
    public bool Cancel(GuestState guest, out GuestState nextGuest)
    {
        nextGuest = null;
        return waiting.Remove(guest) || CompleteService(guest, out nextGuest);
    }

    public bool TryAdmitNext(out GuestState guest)
    {
        guest = null;
        if (!Building.isActive || waiting.Count == 0 || Building.occupied >= Building.capacity)
        {
            return false;
        }

        guest = waiting[0];
        waiting.RemoveAt(0);
        Admit(guest);
        return true;
    }

    private void Admit(GuestState guest)
    {
        admitted.Add(guest);
        Building.occupied++;
    }

    public void Clear()
    {
        Building.occupied = Math.Max(0, Building.occupied - admitted.Count);
        admitted.Clear();
        waiting.Clear();
    }

    private bool ContainsGuest(int id)
    {
        foreach (GuestState guest in waiting)
        {
            if (guest.id == id) return true;
        }
        foreach (GuestState guest in admitted)
        {
            if (guest.id == id) return true;
        }
        return false;
    }

    public static bool SupportsNeed(BuildingType building, GuestNeed need)
    {
        return (building == BuildingType.Restaurant && need == GuestNeed.Food) ||
               (building == BuildingType.BeachBar && need == GuestNeed.Drink) ||
               (building == BuildingType.Reception && need == GuestNeed.Room);
    }
}
