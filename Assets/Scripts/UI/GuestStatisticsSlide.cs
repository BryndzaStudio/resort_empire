
using System.Collections;
using UnityEngine;

public class GuestStatisticsSlide : MonoBehaviour
{
    [Header("Counters (top to bottom)")]


    [SerializeField] private RectTransform capacityCounter;
    [SerializeField] private RectTransform queueCounter;
    [SerializeField] private RectTransform servedCounter;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.45f;
    [SerializeField] private float delayBetweenCounters = 0.15f;
    [SerializeField] private float extraHiddenDistance = 30f;

    private RectTransform[] counters;
    private Vector2[] visiblePositions;

    private Coroutine animation;
    private bool isVisible;

    private void Start()
    {
        if (capacityCounter == null ||
            queueCounter == null ||
            servedCounter == null)
        {
            Debug.LogError(
                "GuestStatisticsSlide: Assign all three counters."
            );

            enabled = false;
            return;
        }

        counters = new[]
        {
            capacityCounter,
            queueCounter,
            servedCounter

        };

        visiblePositions = new Vector2[counters.Length];

        // Save their positions as configured in the editor.
        for (int i = 0; i < counters.Length; i++)
        {
            visiblePositions[i] = counters[i].anchoredPosition;
            MoveCounter(i, 1f);
        }
    }

    private void Update()
    {
        bool shouldBeVisible =
            WaveManager.Instance != null &&
            WaveManager.Instance.IsWaveRunning;

        if (shouldBeVisible == isVisible)
            return;

        isVisible = shouldBeVisible;

        if (animation != null)
            StopCoroutine(animation);

        animation = StartCoroutine(AnimateCounters(isVisible));
    }

    private IEnumerator AnimateCounters(bool show)
    {
        // Snapshot positions so interrupted animations
        // continue smoothly from their current positions.
        Vector2[] starts = new Vector2[counters.Length];

        for (int i = 0; i < counters.Length; i++)
            starts[i] = counters[i].anchoredPosition;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, slideDuration);
        float delay = Mathf.Max(0f, delayBetweenCounters);

        float totalDuration =
            duration + delay * (counters.Length - 1);

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int i = 0; i < counters.Length; i++)
            {
                float localTime = elapsed - i * delay;

                float t = Mathf.Clamp01(localTime / duration);

                // Smooth ease-in/ease-out.
                t = t * t * (3f - 2f * t);

                Vector2 target = show
                    ? visiblePositions[i]
                    : HiddenPosition(i);

                counters[i].anchoredPosition =
                    Vector2.Lerp(starts[i], target, t);
            }

            yield return null;
        }

        for (int i = 0; i < counters.Length; i++)
        {
            counters[i].anchoredPosition = show
                ? visiblePositions[i]
                : HiddenPosition(i);
        }

        animation = null;
    }

    private Vector2 HiddenPosition(int index)
    {
        Vector2 position = visiblePositions[index];

        float distance =
            counters[index].rect.width + extraHiddenDistance;

        position.x += distance;

        return position;
    }

    private void MoveCounter(int index, float hiddenAmount)
    {
        counters[index].anchoredPosition = Vector2.Lerp(
            visiblePositions[index],
            HiddenPosition(index),
            hiddenAmount
        );
    }
}
