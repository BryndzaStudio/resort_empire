
using UnityEngine;

// BRYN-19: Zobrazenie jednej budovy na ostrove.
// Skript patrí na prefab Building, ktorý má komponent SpriteRenderer.
[RequireComponent(typeof(SpriteRenderer))]
public class BuildingView : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    // Typ budovy, ktorú tento objekt zobrazuje.
    public BuildingType Type { get; private set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Nastaví budove obrázok a poradie vykresľovania.
    public void Setup(BuildingType type, Sprite sprite)
    {
        Type = type;
        spriteRenderer.sprite = sprite;
        name = "Building_" + type;

        // Izometria: čo je nižšie na obrazovke, je bližšie ku kamere,
        // preto sa musí kresliť navrch (vyššie sortingOrder).
        spriteRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * 100);
    }
}
