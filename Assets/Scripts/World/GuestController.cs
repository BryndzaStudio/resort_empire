using System;
using UnityEngine;

// BRYN-20: Hosť, ktorý ide po móle (k ostrovu alebo k lodi). Pohyb je len medzi bodmi (waypointy).
// Hosťa k žiadnej budove nepriraďuje a nemení dáta hry. Loď hosť nevolá, len vyvolá udalosť.
public class GuestController : MonoBehaviour
{
    // Hosť dorazil na koniec móla pri ostrove a zmizol.
    // Kolega tu môže napojiť priradenie hosťa k budove: GuestController.OnGuestArrived += ...
    public static event Action<GuestController> OnGuestArrived;

    // Hosť nastúpil na loď a zmizol.
    public static event Action<GuestController> OnGuestDeparted;

    private Vector3[] waypoints;
    private int nextWaypoint;
    private float speed;
    private bool departing;

    // Pri štarte hry vyčistíme udalosti, aby neostali odberatelia zo zničených objektov
    // z predošlého Play (keď je vypnuté Reload Domain).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
        OnGuestArrived = null;
        OnGuestDeparted = null;
    }

    // Nastaví vzhľad hosťa, trasu a rýchlosť chôdze.
    // departing = true: hosť odchádza loďou, false: prišiel loďou.
    public void Setup(Sprite sprite, Vector3[] path, float walkSpeed, bool departing)
    {
        if (sprite != null)
        {
            GetComponent<SpriteRenderer>().sprite = sprite;
        }

        waypoints = path;
        speed = walkSpeed;
        this.departing = departing;
    }

    private void Update()
    {
        if (waypoints == null)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, waypoints[nextWaypoint], speed * Time.deltaTime);

        if (transform.position == waypoints[nextWaypoint])
        {
            nextWaypoint++;

            // Koniec trasy: oznámime to a hosť zmizne.
            if (nextWaypoint >= waypoints.Length)
            {
                Raise(departing ? OnGuestDeparted : OnGuestArrived, this);
                Destroy(gameObject);
            }
        }
    }

    // Zavolá odberateľov udalosti. Zničené objekty (odberateľov) preskočí,
    // aby nevznikol MissingReferenceException.
    private static void Raise(Action<GuestController> handlers, GuestController guest)
    {
        if (handlers == null)
        {
            return;
        }

        foreach (Delegate handler in handlers.GetInvocationList())
        {
            if (handler.Target is UnityEngine.Object target && target == null)
            {
                continue;
            }

            ((Action<GuestController>)handler).Invoke(guest);
        }
    }
}
