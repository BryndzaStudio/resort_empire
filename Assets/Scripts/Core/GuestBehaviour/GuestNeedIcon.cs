using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GuestNeedIcon : MonoBehaviour
{
    [Header("Need Icons")]
    [SerializeField] private Sprite foodIcon;
    [SerializeField] private Sprite drinkIcon;
    [SerializeField] private Sprite roomIcon;

    [Header("Bubble")]
    [SerializeField] private Sprite bubbleSprite;

    [Header("Appearance")]
    [SerializeField] private float heightAboveHead = 1.4f;
    [SerializeField] private float bubbleScale = 0.9f;
    [SerializeField] private float iconScale = 0.45f;
    [SerializeField] private Vector2 iconLocalOffset = new Vector2(0f, 0.02f);

    [Header("Sorting")]
    [SerializeField] private int sortingOrderOffset = 1000;

    private GameObject bubbleObject;
    private GameObject iconObject;
    private SpriteRenderer bubbleRenderer;
    private SpriteRenderer iconRenderer;

    public void ShowNeed(GuestNeed need)
    {
        Sprite symbol = GetIcon(need);

        if (symbol == null)
        {
            Debug.LogWarning($"Missing icon sprite for {need}.");
            return;
        }

        CreateVisualsIfNeeded();

        SpriteRenderer guestRenderer = GetComponent<SpriteRenderer>();

        if (guestRenderer == null)
        {
            Debug.LogWarning("GuestNeedIcon: Guest is missing SpriteRenderer.");
            return;
        }

        bubbleRenderer.sprite = bubbleSprite;
        iconRenderer.sprite = symbol;

        // same sorting layer as guest, but much higher order so it stays on top
        bubbleRenderer.sortingLayerID = guestRenderer.sortingLayerID;
        iconRenderer.sortingLayerID = guestRenderer.sortingLayerID;

        bubbleRenderer.sortingOrder = guestRenderer.sortingOrder + sortingOrderOffset;
        iconRenderer.sortingOrder = bubbleRenderer.sortingOrder + 1;

        float headHeight = 0.5f;
        if (guestRenderer.sprite != null)
        {
            headHeight = guestRenderer.sprite.bounds.max.y;
        }

        // place whole bubble above guest
        bubbleObject.transform.localPosition = new Vector3(0f, headHeight + heightAboveHead, 0f);
        bubbleObject.transform.localScale = Vector3.one * bubbleScale;

        // place symbol inside bubble
        iconObject.transform.localPosition = new Vector3(iconLocalOffset.x, iconLocalOffset.y, 0f);
        iconObject.transform.localScale = Vector3.one * iconScale;
    }

    private void CreateVisualsIfNeeded()
    {
        if (bubbleObject == null)
        {
            bubbleObject = new GameObject("NeedBubble");
            bubbleObject.transform.SetParent(transform, false);
            bubbleRenderer = bubbleObject.AddComponent<SpriteRenderer>();
        }

        if (iconObject == null)
        {
            iconObject = new GameObject("NeedSymbol");
            iconObject.transform.SetParent(bubbleObject.transform, false);
            iconRenderer = iconObject.AddComponent<SpriteRenderer>();
        }
    }

    private Sprite GetIcon(GuestNeed need)
    {
        switch (need)
        {
            case GuestNeed.Food:
                return foodIcon;
            case GuestNeed.Drink:
                return drinkIcon;
            case GuestNeed.Room:
                return roomIcon;
            default:
                return null;
        }
    }
}