
using System;
using System.Collections.Generic;
using UnityEngine;

// BRYN-19: Zobrazí ostrov a všetky budovy z GameState.
// Skript patrí na prefab Island.
public class IslandView : MonoBehaviour
{
    // Nastavenie jedného typu budovy: kde na ostrove stojí a aké má obrázky.
    [Serializable]
    public class BuildingSlot
    {
        public BuildingType type;

        // Prázdny objekt na ostrove, na ktorého mieste sa budova vytvorí.
        public Transform anchor;

        // Obrázky podľa levelu: prvok 0 = lv1, prvok 1 = lv2, prvok 2 = lv3.
        public Sprite[] levelSprites;

        // BRYN-29: Vlastná pozícia odznaku s počtom miest, v lokálnych súradniciach
        // pod budovou (rovnaké čísla ako v Inspectore počas Play).
        // Ak je vypnutá, odznak sa dá automaticky nad vrch obrázka budovy.
        public bool useCustomBadgePosition;
        public Vector2 badgeLocalPosition;
    }

    [SerializeField] private BuildingView buildingPrefab;
    [SerializeField] private List<BuildingSlot> slots = new List<BuildingSlot>();

    // Budovy, ktoré sme vytvorili, aby sme ich vedeli neskôr zmazať.
    private readonly List<BuildingView> spawnedBuildings = new List<BuildingView>();

    // Start beží až po Awake v GameManageri, takže stav hry je už načítaný.
    private void Start()
    {
        Refresh();
    }

    // Zmaže zobrazené budovy a vytvorí ich nanovo podľa aktuálneho stavu hry.
    // Neskôr sa dá zavolať napr. po vylepšení budovy.
    public void Refresh()
    {
        foreach (BuildingView view in spawnedBuildings)
        {
            Destroy(view.gameObject);
        }
        spawnedBuildings.Clear();

        if (GameManager.Instance == null || GameManager.Instance.CurrentState == null)
        {
            Debug.LogWarning("IslandView: V scéne chýba GameManager, budovy sa nezobrazia.");
            return;
        }

        foreach (BuildingState building in GameManager.Instance.CurrentState.buildings)
        {
            // Neaktívne budovy (napr. ešte nepostavené) nezobrazujeme.
            if (!building.isActive)
            {
                continue;
            }

            BuildingSlot slot = slots.Find(s => s.type == building.type);
            if (slot == null || slot.anchor == null)
            {
                Debug.LogWarning($"IslandView: Pre budovu {building.type} chýba miesto (anchor) na ostrove.");
                continue;
            }

            Sprite sprite = GetSprite(slot, building.level);
            if (sprite == null)
            {
                Debug.LogWarning($"IslandView: Pre budovu {building.type} chýba obrázok.");
                continue;
            }

            BuildingView view = Instantiate(buildingPrefab, slot.anchor.position, Quaternion.identity, slot.anchor);
            Vector2? badgePosition = slot.useCustomBadgePosition ? slot.badgeLocalPosition : (Vector2?)null;
            view.Setup(building.type, sprite, badgePosition);
            spawnedBuildings.Add(view);
        }
    }

    // Vráti obrázok pre daný level. Level 1 je v poli na indexe 0.
    // Ak pre level obrázok nie je, použije sa najbližší dostupný.
    private Sprite GetSprite(BuildingSlot slot, int level)
    {
        if (slot.levelSprites == null || slot.levelSprites.Length == 0)
        {
            return null;
        }

        int index = Mathf.Clamp(level - 1, 0, slot.levelSprites.Length - 1);
        return slot.levelSprites[index];
    }
}
