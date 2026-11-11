using TMPro;
using UnityEngine;

// BRYN-19: Zobrazenie jednej budovy na ostrove.
// Skript patrí na prefab Building, ktorý má komponent SpriteRenderer.
// BRYN-29: Nad budovou sa zobrazuje počet miest (kapacita) na tmavom kruhu.
[RequireComponent(typeof(SpriteRenderer))]
public class BuildingView : MonoBehaviour
{
    // Konštanty na ladenie (hodnoty sú vo world jednotkách, nezávislé od scale budovy).
    private const float CapacityFontSize = 10f;       // veľkosť textu (musí sa zmestiť "0/6" do kruhu)
    private const float CapacityOffsetY = 0.6f;       // posun stredu kruhu nad vrch budovy
    private const float BadgeDiameter = 2.1f;         // priemer tmavého kruhu
    private const float OutlineWidth = 0.3f;          // hrúbka čierneho obrysu čísla
    private static readonly Color BadgeColor = new Color(0.05f, 0.08f, 0.12f, 0.85f);

    // Budovy majú sortingOrder = -y * 100 (malé čísla), preto je toto vždy nad nimi.
    private const int BadgeSortingOrder = 10000;
    private const int TextSortingOrder = BadgeSortingOrder + 1;

    // Kruh vytvoríme v kóde len raz a zdieľame ho medzi všetkými budovami.
    private static Sprite circleSprite;

    private SpriteRenderer spriteRenderer;
    private TextMeshPro capacityText;
    private Transform badge;
    private BuildingState state;
    private int shownCapacity = -1;
    private int shownOccupied = -1;

    // Typ budovy, ktorú tento objekt zobrazuje.
    public BuildingType Type { get; private set; }
    public BuildingState State => state;
    public float ServiceSeconds { get; private set; } = 10f;
    private Vector2 guestEntranceOffset;
    private Vector2 guestQueueStep;
    public Vector3 GuestEntrancePosition => transform.position + (Vector3)guestEntranceOffset;

    public Vector3 GetQueuePosition(int index)
    {
        return GuestEntrancePosition + (Vector3)(guestQueueStep * (index + 1));
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CreateCapacityBadge();
    }

    // Nastaví budove obrázok a poradie vykresľovania.
    // badgeLocalPosition: vlastná pozícia odznaku pod budovou, null = automaticky nad budovou.
    public void Setup(BuildingType type, Sprite sprite, Vector2? badgeLocalPosition = null,
        float serviceSeconds = 10f, Vector2? entranceOffset = null, Vector2? queueStep = null)
    {
        Type = type;
        spriteRenderer.sprite = sprite;
        name = "Building_" + type;

        // Izometria: čo je nižšie na obrazovke, je bližšie ku kamere,
        // preto sa musí kresliť navrch (vyššie sortingOrder).
        spriteRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * 100);

        if (badgeLocalPosition.HasValue)
        {
            // Pozícia nastavená v Inspectore (IslandView, slot budovy).
            Vector3 localPosition = badgeLocalPosition.Value;
            badge.localPosition = localPosition;
            capacityText.transform.localPosition = localPosition;
        }
        else
        {
            // Kruh s číslom dáme nad vrch obrázka budovy.
            Bounds bounds = spriteRenderer.bounds;
            Vector3 badgePosition = new Vector3(bounds.center.x, bounds.max.y + CapacityOffsetY, transform.position.z);
            badge.position = badgePosition;
            capacityText.transform.position = badgePosition;
        }

        state = GameManager.Instance.CurrentState.buildings.Find(b => b.type == type);
        ServiceSeconds = serviceSeconds > 0f ? serviceSeconds : 10f;
        guestEntranceOffset = entranceOffset ?? new Vector2(0f, 0.1f);
        guestQueueStep = queueStep ?? new Vector2(1.25f, -0.65f);
        if (GuestManager.Instance != null) GuestManager.Instance.RegisterBuilding(this);
        ShowCapacity();
    }

    // Ak sa obsadené miesta alebo kapacita v stave hry zmenia, text sa prepíše sám.
    private void Update()
    {
        if (state != null && (state.capacity != shownCapacity || state.occupied != shownOccupied))
        {
            ShowCapacity();
        }
    }

    // Zobrazí text v tvare obsadené/kapacita, napr. "0/6".
    private void ShowCapacity()
    {
        if (state == null)
        {
            return;
        }

        shownCapacity = state.capacity;
        shownOccupied = state.occupied;
        capacityText.text = shownOccupied + "/" + shownCapacity;
    }

    // Kruh aj text vytvárame v kóde, aby sa nemusel meniť prefab Building.
    private void CreateCapacityBadge()
    {
        // Budova má scale 0.7, deti by sa zmenšili s ňou. Vyrovnáme to,
        // aby konštanty hore platili vo world jednotkách.
        Vector3 parentScale = transform.localScale;
        Vector3 compensation = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f);

        GameObject badgeObject = new GameObject("CapacityBadge");
        badge = badgeObject.transform;
        badge.SetParent(transform, false);
        badge.localScale = compensation * BadgeDiameter;

        SpriteRenderer badgeRenderer = badgeObject.AddComponent<SpriteRenderer>();
        badgeRenderer.sprite = GetCircleSprite();
        badgeRenderer.color = BadgeColor;
        badgeRenderer.sortingOrder = BadgeSortingOrder;

        GameObject textObject = new GameObject("CapacityText");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localScale = compensation;

        capacityText = textObject.AddComponent<TextMeshPro>();
        capacityText.fontSize = CapacityFontSize;
        capacityText.fontStyle = FontStyles.Bold;
        capacityText.alignment = TextAlignmentOptions.Center;
        capacityText.color = Color.white;
        capacityText.outlineWidth = OutlineWidth;
        capacityText.outlineColor = Color.black;
        capacityText.sortingOrder = TextSortingOrder;
    }

    // Jednoduchý biely kruh s vyhladenou hranou, veľký presne 1 jednotku.
    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        const int size = 64;
        float radius = size / 2f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(radius, radius);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Priehľadnosť klesá na hrane kruhu, vďaka tomu nie je zubatý.
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance)));
            }
        }
        texture.Apply();

        circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}
