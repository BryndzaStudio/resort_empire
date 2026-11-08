using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// BRYN-20: Animácia lode pri ľavom móle: príchod hostí, odchod hostí a odplávanie.
// Pohyb je len medzi bodmi (waypointy), bez fyziky. Hostí k budovám nepriraďuje.
// Dráha hostí vedie presne medzi objektmi PierStart a PierEnd pod Pier_Left (prefab Island),
// miesto zastavenia lode sa odvodzuje z veľkosti a polohy móla.
public class BoatController : MonoBehaviour
{
    [Header("Hostia")]
    [SerializeField] private GuestController guestPrefab;
    [SerializeField] private Sprite[] guestSprites;
    [Range(1, 3)] [SerializeField] private int arrivingGuests = 2;   // koľko hostí vystúpi z lode
    [Range(1, 3)] [SerializeField] private int departingGuests = 2;  // koľko hostí nastúpi na loď
    [SerializeField] private float guestInterval = 0.8f;   // odstup medzi hosťami (s)
    [SerializeField] private float guestSpeed = 2f;        // rýchlosť chôdze po móle
    [SerializeField] private Vector2 guestBoardOffset = new Vector2(0.6f, 0.9f); // kde na lodi hosť nastupuje/vystupuje (od stredu lode, smerom k móle)

    [Header("Loď")]
    [SerializeField] private float boatSpeed = 4f;
    [SerializeField] private float dockPause = 1f;         // pauza pri móle (po zastavení, medzi príchodom a odchodom, po nastúpení)
    [SerializeField] private float cycleInterval = 10f;    // pauza po odplávaní, kým sa cyklus zopakuje
    // Smer plavby (provou dopredu): loď príde zľava hore a odpláva doprava dole.
    // Spawn aj odchod sa nájdu automaticky tak, aby bola loď celá mimo záberu kamery (aj po zmene jej veľkosti).
    [SerializeField] private Vector2 approachDirection = new Vector2(0.87f, -0.49f);
    [SerializeField] private float offscreenMargin = 1f;   // rezerva za okrajom záberu

    [Header("Mólo")]
    [SerializeField] private string pierName = "Pier_Left";
    // Dráha hostí po móle: presne z pozícií týchto dvoch prázdnych objektov pod Pier_Left (v prefabe Island).
    // Posúvaj ich v Scene view, hosť pôjde presne po nich (pri odchode opačne). Žiadne ďalšie offsety.
    [SerializeField] private Transform pierStart; // vonkajší koniec móla pri lodi
    [SerializeField] private Transform pierEnd;   // koniec móla pri ostrove

    // Dokovanie lode: bod v relatívnych súradniciach obrázka móla ((0,0) = ľavý dolný roh, (1,1) = pravý horný),
    // takže sa mení spolu s veľkosťou a polohou móla.
    [SerializeField] private Vector2 dockPoint = new Vector2(0.108f, 0.161f);      // stred ľavej dolnej hrany vonkajšieho konca móla
    // Vzdialenosť stredu lode od dockPoint v jednotkách sveta (nezávisí od veľkosti móla, len od lode).
    // Smeruje von z móla do mora, takže loď mólo neprekrýva.
    [SerializeField] private Vector2 dockOffset = new Vector2(-1f, -1.75f);

    private const float FallbackDistance = 40f; // keď nie je ortografická kamera

    private SpriteRenderer boatRenderer;
    private SpriteRenderer pier;

    // Hostia, ktorých sme poslali. Loď len sleduje, či ešte existujú, hostia loď nevolajú.
    private readonly List<GuestController> guests = new List<GuestController>();

    private void Start()
    {
        boatRenderer = GetComponent<SpriteRenderer>();
        pier = FindPier();
        if (pier == null || pierStart == null || pierEnd == null)
        {
            Debug.LogWarning($"BoatController: Chýba objekt '{pierName}' alebo nie sú priradené Pier Start / Pier End, loď sa nespustí.");
            gameObject.SetActive(false);
            return;
        }

        StartCoroutine(Run());
    }

    // Pri opätovnom zapnutí lode sa cyklus spustí znova (pri prvom zapnutí to robí Start).
    private void OnEnable()
    {
        if (pier != null)
        {
            StartCoroutine(Run());
        }
    }

    // Pri vypnutí alebo zničení lode zastavíme všetky coroutines.
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    // Jeden cyklus: loď priplaví, hostia vystúpia, hostia nastúpia, loď odpláva, pauza a znova.
    private IEnumerator Run()
    {
        while (this != null)
        {
            Vector3 dock = DockPosition();
            transform.position = OffscreenPoint(dock, -approachDirection);

            yield return MoveTo(dock);
            yield return new WaitForSeconds(dockPause);

            yield return SendGuests(arrivingGuests, false);
            yield return new WaitForSeconds(dockPause);

            yield return SendGuests(departingGuests, true);
            yield return new WaitForSeconds(dockPause);

            yield return MoveTo(OffscreenPoint(dock, approachDirection));
            yield return new WaitForSeconds(cycleInterval);
        }
    }

    // Pohyb lode k cieľu konštantnou rýchlosťou.
    private IEnumerator MoveTo(Vector3 target)
    {
        while (this != null && transform.position != target)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, boatSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // Pošle hostí jedného po druhom a počká, kým všetci zmiznú.
    // Sprity sa miešajú, takže v jednej skupine sa neopakuje ten istý hosť (kým sú k dispozícii).
    private IEnumerator SendGuests(int count, bool departing)
    {
        List<Sprite> sprites = ShuffledSprites();

        for (int i = 0; i < count; i++)
        {
            Sprite sprite = sprites.Count > 0 ? sprites[i % sprites.Count] : null;
            SpawnGuest(sprite, departing);
            yield return new WaitForSeconds(guestInterval);
        }

        while (AnyGuestAlive())
        {
            yield return null;
        }
    }

    // Príchod: hosť začne na lodi a ide na koniec móla pri ostrove.
    // Odchod: hosť začne na konci móla pri ostrove a ide k lodi.
    private void SpawnGuest(Sprite sprite, bool departing)
    {
        Vector3 boardPoint = transform.position + (Vector3)guestBoardOffset;
        Vector3 start = FlatPosition(pierStart);
        Vector3 end = FlatPosition(pierEnd);

        Vector3 spawnPoint = departing ? end : boardPoint;
        Vector3[] path = departing ? new[] { start, boardPoint } : new[] { start, end };

        GuestController guest = Instantiate(guestPrefab, spawnPoint, Quaternion.identity);
        guest.Setup(sprite, path, guestSpeed, departing);
        guests.Add(guest);
    }

    // Zničený hosť (už zmizol) sa zo zoznamu odstráni, Unity ho porovná s null.
    private bool AnyGuestAlive()
    {
        guests.RemoveAll(g => g == null);
        return guests.Count > 0;
    }

    // Náhodne zamiešaný zoznam spritov hostí.
    private List<Sprite> ShuffledSprites()
    {
        List<Sprite> list = new List<Sprite>();
        if (guestSprites != null)
        {
            list.AddRange(guestSprites);
        }

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    private SpriteRenderer FindPier()
    {
        GameObject pierObject = GameObject.Find(pierName);
        return pierObject != null ? pierObject.GetComponent<SpriteRenderer>() : null;
    }

    // Stred lode pri móle: bod na hrane móla posunutý do mora o pevný odstup.
    private Vector3 DockPosition()
    {
        return BoundsPoint(pier, dockPoint) + (Vector3)dockOffset;
    }

    // Pozícia objektu vo svete, kde z = 0 (všetko je v rovine 2D).
    private static Vector3 FlatPosition(Transform target)
    {
        return new Vector3(target.position.x, target.position.y, 0f);
    }

    // Prepočíta relatívny bod na obrázku móla na súradnice vo svete.
    private static Vector3 BoundsPoint(SpriteRenderer pierRenderer, Vector2 relative)
    {
        Bounds bounds = pierRenderer.bounds;
        return new Vector3(
            bounds.min.x + bounds.size.x * relative.x,
            bounds.min.y + bounds.size.y * relative.y,
            0f
        );
    }

    // Prvý bod na priamke od "from" v danom smere, v ktorom je loď celá mimo záberu kamery.
    private Vector3 OffscreenPoint(Vector3 from, Vector2 direction)
    {
        Vector2 step = direction.normalized;
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
        {
            return from + (Vector3)(step * FallbackDistance);
        }

        Vector3 extents = boatRenderer.bounds.extents;
        float halfWidth = cam.orthographicSize * cam.aspect + extents.x + offscreenMargin;
        float halfHeight = cam.orthographicSize + extents.y + offscreenMargin;
        Vector3 center = cam.transform.position;

        Vector3 point = from;
        for (int i = 0; i < 300; i++)
        {
            if (Mathf.Abs(point.x - center.x) > halfWidth || Mathf.Abs(point.y - center.y) > halfHeight)
            {
                break;
            }

            point += (Vector3)step;
        }

        return point;
    }

    // V Scene view je stále vidieť dráhu hostí: PierStart (zelená guľa) a PierEnd (červená guľa).
    // Dajú sa podľa nich pohodlne posúvať.
    private void OnDrawGizmos()
    {
        if (pierStart == null || pierEnd == null)
        {
            return;
        }

        Vector3 start = FlatPosition(pierStart);
        Vector3 end = FlatPosition(pierEnd);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(start, end);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(start, 0.25f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(end, 0.25f);
    }

    // Pri označenej lodi ukáže aj miesto zastavenia a smer plavby.
    private void OnDrawGizmosSelected()
    {
        SpriteRenderer pierRenderer = FindPier();
        if (pierRenderer == null)
        {
            return;
        }

        Vector3 edge = BoundsPoint(pierRenderer, dockPoint);
        Vector3 dock = edge + (Vector3)dockOffset;
        Vector3 along = (Vector3)approachDirection.normalized * 10f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(dock, 0.4f);
        Gizmos.DrawLine(dock - along, dock + along);
        Gizmos.DrawLine(edge, dock);
    }
}
