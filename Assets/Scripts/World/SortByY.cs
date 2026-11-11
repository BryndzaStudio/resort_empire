
using UnityEngine;

// BRYN-19: Izometrické poradie kreslenia pre dekorácie (palmy, kamene, mólo...).
// Čo má spodok nižšie na obrazovke, kreslí sa navrch – rovnako ako budovy v BuildingView.
[RequireComponent(typeof(SpriteRenderer))]
public class SortByY : MonoBehaviour
{
    // BRYN-20: Pohyblivé objekty (loď, hostia) prepočítavajú poradie stále, nielen pri štarte.
    [SerializeField] private bool everyFrame;

    // Posun poradia. Hosť na móle musí byť nad mólom aj lodou, preto dostane veľké kladné číslo.
    [SerializeField] private int orderOffset;

    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateOrder();
    }

    private void LateUpdate()
    {
        if (everyFrame)
        {
            UpdateOrder();
        }
    }

    private void UpdateOrder()
    {
        // Berieme spodok obrázka (nie stred), aby nezáležalo na pivote spritu.
        spriteRenderer.sortingOrder = -Mathf.RoundToInt(spriteRenderer.bounds.min.y * 100) + orderOffset;
    }
}
