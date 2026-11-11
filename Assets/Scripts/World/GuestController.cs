using System;
using UnityEngine;

// BRYN-20: pier travel. BRYN-64/67: the same guest continues to a queue/building.
public class GuestController : MonoBehaviour
{
    public static event Action<GuestController> OnGuestArrived;
    public static event Action<GuestController> OnGuestDeparted;

    private Vector3[] waypoints;
    private int nextWaypoint;
    private float speed;
    private bool departing;
    private Action reachedDestination;

    public bool HasFinishedPierJourney { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
        OnGuestArrived = null;
        OnGuestDeparted = null;
    }

    public void Setup(Sprite sprite, Vector3[] path, float walkSpeed, bool departing)
    {
        if (sprite != null) GetComponent<SpriteRenderer>().sprite = sprite;
        waypoints = path;
        nextWaypoint = 0;
        speed = Mathf.Max(0.01f, walkSpeed);
        this.departing = departing;
        reachedDestination = null;
        HasFinishedPierJourney = false;
    }

    public void MoveTo(Vector3 destination, Action onReached = null)
    {
        destination.z = transform.position.z;
        waypoints = new[] { destination };
        nextWaypoint = 0;
        reachedDestination = onReached;
    }

    private void Update()
    {
        if (waypoints == null || nextWaypoint >= waypoints.Length) return;
        transform.position = Vector3.MoveTowards(transform.position, waypoints[nextWaypoint], speed * Time.deltaTime);
        if (transform.position != waypoints[nextWaypoint]) return;
        nextWaypoint++;
        if (nextWaypoint < waypoints.Length) return;

        waypoints = null;
        Action callback = reachedDestination;
        reachedDestination = null;
        if (HasFinishedPierJourney)
        {
            callback?.Invoke();
            return;
        }

        HasFinishedPierJourney = true;
        bool assigned = !departing && GuestManager.Instance != null &&
                        GuestManager.Instance.ArriveGuest(this);
        Raise(departing ? OnGuestDeparted : OnGuestArrived, this);
        if (!assigned) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (GuestManager.Instance != null) GuestManager.Instance.UnregisterGuest(this);
    }

    private static void Raise(Action<GuestController> handlers, GuestController guest)
    {
        if (handlers == null) return;
        foreach (Delegate handler in handlers.GetInvocationList())
        {
            if (handler.Target is UnityEngine.Object target && target == null) continue;
            ((Action<GuestController>)handler).Invoke(guest);
        }
    }
}
