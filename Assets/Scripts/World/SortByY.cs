
using UnityEngine;

// BRYN-19: Izometrické poradie kreslenia pre dekorácie (palmy, kamene, mólo...).
// Čo má spodok nižšie na obrazovke, kreslí sa navrch – rovnako ako budovy v BuildingView.
[RequireComponent(typeof(SpriteRenderer))]
public class SortByY : MonoBehaviour
{
    private void Start()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        // Berieme spodok obrázka (nie stred), aby nezáležalo na pivote spritu.
        spriteRenderer.sortingOrder = -Mathf.RoundToInt(spriteRenderer.bounds.min.y * 100);
    }
}
